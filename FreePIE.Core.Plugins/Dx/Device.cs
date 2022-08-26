using System;
using System.Collections.Generic;
using FreePIE.Core.Plugins.Strategies;
using FreePIE.Core.Plugins.VJoy;
using SlimDX.DirectInput;
using System.Linq;
using FreePIE.Core.Plugins.VJoy.PacketData;
using System.Threading;
using System.Globalization;
using System.Runtime.InteropServices;

namespace FreePIE.Core.Plugins.Dx
{
    public class Device : IDisposable
    {
        private const int BlockSize = 8;
        private Effect[] Effects = new Effect[BlockSize];
        private readonly EffectParameters[] effectParams = new EffectParameters[BlockSize];
        private int[] Axes;

        public string Name { get { return joystick.Properties.ProductName; } }
        public Guid InstanceGuid { get { return joystick.Information.InstanceGuid; } }
        public bool SupportsFfb { get; }


        public Joystick joystick { get; }
        private JoystickState state;
        private readonly GetPressedStrategy<int> getPressedStrategy;

        public Device(Joystick joystick)
        {
            //Force english debugging info.
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            this.joystick = joystick;
            SetRange(-10000, 10000);
            getPressedStrategy = new GetPressedStrategy<int>(GetDown);

            SupportsFfb = joystick.Capabilities.Flags.HasFlag(DeviceFlags.ForceFeedback);
            if (SupportsFfb)
                PrepareFfb();
        }

        public JoystickState State
        {
            get { return state ?? (state = joystick.GetCurrentState()); }
        }

        public void Reset()
        {
            state = null;
        }

        public void SetRange(int lowerRange, int upperRange)
        {
            foreach (DeviceObjectInstance deviceObject in joystick.GetObjects())
                if ((deviceObject.ObjectType & ObjectDeviceType.Axis) != 0)
                    joystick.GetObjectPropertiesById((int)deviceObject.ObjectType).SetRange(lowerRange, upperRange);
        }

        public bool GetPressed(int button)
        {
            return getPressedStrategy.IsPressed(button);
        }

        public bool GetDown(int button)
        {
            return State.IsPressed(button);
        }

        public bool AutoCenter
        {
            get { return joystick.Properties.AutoCenter; }
            set
            {
                CheckFfbSupport("Unable to set autoCenter");
                joystick.Properties.AutoCenter = value;
            }
        }

        public int Gain
        {
            get { return joystick.Properties.ForceFeedbackGain; }
            set
            {
                CheckFfbSupport("Unable to set gain");
                joystick.Properties.ForceFeedbackGain = value;
            }
        }

        private void CheckFfbSupport(string message)
        {
            if (!SupportsFfb)
                throw new NotSupportedException(message + " - this device does not support FFB.");
        }

        private void PrepareFfb()
        {
            List<int> ax = new List<int>();
            foreach (DeviceObjectInstance deviceObject in joystick.GetObjects())
                if ((deviceObject.ObjectType & ObjectDeviceType.ForceFeedbackActuator) != 0)
                {
                    ax.Add((int)deviceObject.ObjectType);
                    Console.WriteLine("ObjectType: " + deviceObject.ObjectType);
                }
            Axes = ax.ToArray();
        }

        public void SetEnvelope(int blockIndex, int attackLevel, int attackTime, int fadeLevel, int fadeTime)
        {
            Envelope envelope = new Envelope();
            envelope.AttackLevel = attackLevel;
            envelope.AttackTime = attackTime;
            envelope.FadeLevel = fadeLevel;
            envelope.FadeTime = fadeTime;
            effectParams[blockIndex].Envelope = envelope;
        }

        public void SetPeriodicForce(int blockIndex, int magnitude, int offset, int period, int phase)
        {
            CheckFfbSupport("Unable to set constant force");

            if (effectParams[blockIndex].Parameters == null)
            {
                effectParams[blockIndex].Parameters = new PeriodicForce();
            }

            effectParams[blockIndex].Parameters.AsPeriodicForce().Magnitude = magnitude;
            effectParams[blockIndex].Parameters.AsPeriodicForce().Offset = offset;
            effectParams[blockIndex].Parameters.AsPeriodicForce().Period = period;
            effectParams[blockIndex].Parameters.AsPeriodicForce().Phase = phase;

            if (Effects[blockIndex] != null && !Effects[blockIndex].Disposed)
            {
                Effects[blockIndex].SetParameters(effectParams[blockIndex], EffectParameterFlags.TypeSpecificParameters);
            }
        }

        public void SetConstantForce(int blockIndex, int magnitude)
        {
            CheckFfbSupport("Unable to set constant force");

            effectParams[blockIndex].Parameters = GetTypeSpecificParameter(FFBEType.ET_CONST);
            effectParams[blockIndex].Parameters.AsConstantForce().Magnitude = magnitude;

            if (Effects[blockIndex] != null && !Effects[blockIndex].Disposed)
            {
                Effects[blockIndex].SetParameters(effectParams[blockIndex], EffectParameterFlags.TypeSpecificParameters);
            }

        }

        //TODO: add Set functions for the other types: SetPeriodicForce, SetContionSet (SetConditionalForce?), and SetRampForce

        public void SendPacket(FfbPacket rawPacket)
        {
            ObjectData objectData = new ObjectData();
            objectData.Instance = (int) rawPacket.packet.Command;

           // Microsoft.DirectX.DirectInput.Button.NoTrigger

            byte[] outBuffer = new byte[rawPacket.packet.DataSize];
            Marshal.Copy(rawPacket.packet.PtrToData, outBuffer, 0, rawPacket.packet.DataSize - 8);

            objectData.Data = rawPacket.packet.PtrToData.ToInt32();
            joystick.SendData(new ObjectData[] { objectData }, false);
        }

        public void SetEffectParams(EffectReportPacket er)
        {
            //This function is supposed to be a combination for multiple packets, because those packets are received in an unusual order (this is what I observed after testing with custom FFB code and games):
            //- CreateNewEffect, which only contains an EffectType (so, techincally one could create the Effect already since only the type needs to be known, but has no idea in which block to put it)
            //- Set<EffectType>, where the packet type already indicates which type it is. This packet can be used to place the effect in the correct block (since it includes a blockIdx), and we can construct the TypeSpecificParameters with this. Again, need to put those parameters "somewhere" until they can be put into the final EffectParameters
            //- Effect (this method). All information needed to construct and start an Effect is included here.

            //So - for simplicity's sake - I decided to just ignore the other two packets and start here. This means that this method is responsible for: creating an Effect; creating and filling EffectParameters; creating and setting TypeSpecificParameters on the EffectParameters; setting the EffectParameters on the Effect.

            //Also, from my testing, when new Effect is called no packets were sent, all above 3 were sent all at once when SetParamters was called (or, when new Effect was called with EffectParameters, obviously). Meaning that there's no real advantage to calling new Effect early.

            //angle is in 100th degrees, so if you want to express 90 degrees (vector pointing to the right) you'll have to enter 9000
            Console.WriteLine("Name: {0}", Name);
            Console.WriteLine("InstanceGuid: {0}", InstanceGuid);
            var directions = er.Effect.Polar ? new int[] { er.Effect.Direction * 36000 / 255 } : new int[] { er.Effect.DirX, er.Effect.DirY };
            //CreateEffect(er.BlockIndex, er.EffectType, er.Polar, directions, er.Duration, er.NormalizedGain, er.SamplePeriod, 0, er.TriggerBtn, er.TriggerRepeatInterval);
            CreateEffect(er.BlockIndex, er.Effect.EffectType, er.Effect.Polar, directions, er.Effect.Duration*1000, er.NormalizedGain, er.Effect.SamplePrd * 1000, er.Effect.StartDelay * 1000, -1, 0);
        }

        public void CreateTestEffect()
        {
            SetConstantForce(1, 5000);
            CreateEffect(1, FFBEType.ET_CONST, false, new int[] { 1, 0 });
            OperateEffect(1, FFBOP.EFF_START, 0);
        }

        public void CreateEffect(int blockIndex, FFBEType effectType, bool polar, int[] dirs, int duration = -1, int gain = 10000, int samplePeriod = 0, int startDelay = 0, int triggerButton = -1, int triggerRepeatInterval = 0)
        {
            CheckFfbSupport("Unable to create effect");

            TypeSpecificParameters parametersDefault = effectParams[blockIndex].Parameters;

            Envelope envelope = new Envelope();
            envelope.AttackLevel = 10000;
            envelope.AttackTime = 0;
            envelope.FadeLevel = 10000;
            envelope.FadeTime = 0;
            if (effectParams[blockIndex].Envelope.HasValue)
            {
                envelope.AttackLevel = effectParams[blockIndex].Envelope.Value.AttackLevel;
                envelope.AttackTime = effectParams[blockIndex].Envelope.Value.AttackTime;
                envelope.FadeLevel = effectParams[blockIndex].Envelope.Value.FadeLevel;
                envelope.FadeTime = effectParams[blockIndex].Envelope.Value.FadeTime;
            }

            effectParams[blockIndex] = new EffectParameters()
            {
                Duration = duration,
                Flags = EffectFlags.ObjectIds | (polar ? EffectFlags.Polar : EffectFlags.Cartesian),
                Gain = gain,
                SamplePeriod = samplePeriod,
                StartDelay = startDelay,
                TriggerButton = triggerButton,
                TriggerRepeatInterval = triggerRepeatInterval,
                Envelope = envelope
            };
            effectParams[blockIndex].Parameters = parametersDefault;

            // Convert polar to cartesian coordinates.
            if (polar)
            {
                /*double pa = dirs[0] * Math.PI / 18000d;
                // Convert to actual polar coordinates:
                // FFB polar coordinates have 0degrees pointing to the north, and go clockwise, whereas 'real', mathematic polar coordinates point to the east and go counterclockwise
                pa = 0.5 * Math.PI - pa;
                // For being a direction, scale of x and y should not matter as long as they are in the same proportions relative to eachother (couldn't find anything in the spec though....). Might need to increase the 1000 constant to get a more precise number though...
                dirs[0] = (int)(Math.Cos(pa) * 1000); // x-axis
                if (dirs.Length > 1)
                    dirs[1] = Axes.Length > 1 ? (int)(Math.Sin(pa) * 1000) : 0; // y-axis, if it exists*/

            }

            effectParams[blockIndex].SetAxes(Axes, dirs);

            CreateEffect(blockIndex, effectType);
        }

        #region FFB helper functions

        /// <summary>
        /// Sets empty TypeSpecificParameters, and determines whether the current effect may need to be disposed
        /// </summary>
        /// <param name="blockIndex"></param>
        /// <param name="type">The <see cref="EffectType"/> to create an effect for</param>
        public void CreateEffect(int blockIndex, FFBEType type)
        {
            Console.WriteLine("!!! Creating effect: {0}", type);

            /*var createdEffects = joystick.CreatedEffects.ToArray();

            
            Console.WriteLine("This device already has {0} effects created.", createdEffects.Length);
            if (createdEffects.Length > 0)
            {
                var sameEffects = createdEffects.Where(e => e.Guid == eGuid).ToArray();
                Console.WriteLine("Of those, {0} are of the current type. Disposing them (if they have their 'Disposed' flag set to false)", sameEffects.Length);

                foreach (var sameEffect in sameEffects)
                {
                    Console.WriteLine("{0}: Disposed: {1};", sameEffect.Guid, sameEffect.Disposed);
                    if (!sameEffect.Disposed)
                        sameEffect.Dispose();
                }
            }*/

            //if an effect already exists, dispose it (do not attempt to check whether it's still valid and use SetParameters(.., EffectParameterFlags.All), because that'll crash.
            /*if (Effects[blockIndex] != null && !Effects[blockIndex].Disposed)
                Effects[blockIndex].Dispose();*/


            /*var sameEffects = createdEffects.Where(e => e.Guid == eGuid).ToArray();
            if (sameEffects.Length > 0)
            {
                Effects[blockIndex] = sameEffects[0];
                return;
            }*/

            var eGuid = GetEffectGuid(type);
            Console.WriteLine("eGuid {0}", eGuid);


            if (Effects[blockIndex] != null && !Effects[blockIndex].Disposed)
            {
                //return;
                if (Effects[blockIndex].Guid.Equals(eGuid))
                {
                    //Effects[blockIndex].SetParameters(effectParams[blockIndex]);
                    Console.WriteLine("Ignoring as effect was already created");
                    return;
                }
                else
                {
                    Effects[blockIndex].Dispose();
                }
            }

            try
            {

                if (effectParams[blockIndex].Parameters == null)
                {
                    Console.WriteLine("!!! WARNING: Effect Parameters are empty!");
                    effectParams[blockIndex].Parameters = GetTypeSpecificParameter(type);
                    Effects[blockIndex] = new Effect(joystick, eGuid, effectParams[blockIndex]);
                } else
                {
                    Effects[blockIndex] = new Effect(joystick, eGuid, effectParams[blockIndex]);
                }

            }
            catch (Exception e)
            {
                throw new Exception("Unable to create new effect: " + e.Message, e);
            }
        }

        public void OperateEffect(int blockIndex, FFBOP effectOperation, int loopCount = 0)
        {

            CheckFfbSupport("Unable to operate effect");

            if (Effects[blockIndex] == null)
                throw new Exception("No effect has been created in block " + blockIndex);

            switch (effectOperation)
            {
                case FFBOP.EFF_START:
                    Console.WriteLine("Name: {0}", Name);
                    Console.WriteLine("InstanceGuid: {0}", InstanceGuid);
                    Effects[blockIndex].Download();
                    Effects[blockIndex].Start(loopCount);
                    break;
                case FFBOP.EFF_SOLO:
                    Effects[blockIndex].Download();
                    Effects[blockIndex].Start(loopCount, EffectPlayFlags.Solo);
                    break;
                case FFBOP.EFF_STOP:
                    Effects[blockIndex].Stop();
                    break;
            }
        }

        public void Stop()
        {
            foreach (var e in joystick.CreatedEffects)
            {
                Console.WriteLine("Stopping effect: {0}", GuidToEffectType[e.Guid]);
                e.Stop();
            }
        }

        private Guid GetEffectGuid(FFBEType et)
        {
            Guid effectGuid = EffectTypeGuidMap(et);
            if (!joystick.GetEffects().Any(effectInfo => effectInfo.Guid == effectGuid))
                throw new Exception(string.Format("Joystick doesn't support {0}!", et));
            return effectGuid;
        }

        #endregion

        #region dispose functions
        public void Dispose()
        {
            DisposeEffects();
            //Console.WriteLine("Disposing joystick: " + Name);
            joystick.Dispose();
            //Console.WriteLine("Finished disposing joystick");
        }

        private void DisposeEffects()
        {
            if (SupportsFfb)
            {
                Console.WriteLine("Joystick {0} has {1} active effects. Disposing...", Name, joystick.CreatedEffects.Count);
                foreach (var e in joystick.CreatedEffects)
                    e.Dispose();
            }
        }

        public void DisposeEffect(int blockIndex)
        {
            Effects[blockIndex]?.Dispose();
        }

        #endregion

        #region mapping functions

        private static TypeSpecificParameters GetTypeSpecificParameter(FFBEType effectType)
        {
            switch (effectType)
            {
                case FFBEType.ET_CONST:
                    return new ConstantForce();
                case FFBEType.ET_RAMP:
                    return new RampForce();
                case FFBEType.ET_SQR:
                case FFBEType.ET_SINE:
                case FFBEType.ET_TRNGL:
                case FFBEType.ET_STUP:
                case FFBEType.ET_STDN:
                    return new PeriodicForce();
                case FFBEType.ET_SPRNG:
                case FFBEType.ET_DMPR:
                case FFBEType.ET_INRT:
                case FFBEType.ET_FRCTN:
                    return new ConditionSet();
                case FFBEType.ET_CSTM:
                    return new CustomForce();
                default:
                    return null;
            }
        }

        private static Dictionary<Guid, FFBEType> GuidToEffectType = Enum.GetValues(typeof(FFBEType)).Cast<FFBEType>().ToDictionary(EffectTypeGuidMap);

        private static Guid EffectTypeGuidMap(FFBEType et)
        {
            switch (et)
            {
                case FFBEType.ET_CONST:
                    return EffectGuid.ConstantForce;
                case FFBEType.ET_RAMP:
                    return EffectGuid.RampForce;
                case FFBEType.ET_SQR:
                    return EffectGuid.Square;
                case FFBEType.ET_SINE:
                    return EffectGuid.Sine;
                case FFBEType.ET_TRNGL:
                    return EffectGuid.Triangle;
                case FFBEType.ET_STUP:
                    return EffectGuid.SawtoothUp;
                case FFBEType.ET_STDN:
                    return EffectGuid.SawtoothDown;
                case FFBEType.ET_SPRNG:
                    return EffectGuid.Spring;
                case FFBEType.ET_DMPR:
                    return EffectGuid.Damper;
                case FFBEType.ET_INRT:
                    return EffectGuid.Inertia;
                case FFBEType.ET_FRCTN:
                    return EffectGuid.Friction;
                case FFBEType.ET_CSTM:
                    return EffectGuid.CustomForce;
                case FFBEType.ET_NONE:
                default:
                    return new Guid();
            }
        }
        #endregion
    }
}
