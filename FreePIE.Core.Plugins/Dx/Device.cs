using System;
using System.Collections.Generic;
using FreePIE.Core.Plugins.Strategies;
using FreePIE.Core.Plugins.VJoy;
using SlimDX.DirectInput;
using EffectType = FreePIE.Core.Plugins.VJoy.EffectType;
using System.Linq;
using FreePIE.Core.Plugins.VJoy.PacketData;
using System.Threading;
using System.Globalization;

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


        private readonly Joystick joystick;
        private JoystickState state;
        private readonly GetPressedStrategy<int> getPressedStrategy;

        public Device(Joystick joystick)
        {
            //Force english debugging info.
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            this.joystick = joystick;
            SetRange(-1000, 1000);
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
                    ax.Add((int)deviceObject.ObjectType);
            Axes = ax.ToArray();
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
            var directions = er.Polar ? new int[] { er.NormalizedAngleInDegrees, 0 } : new int[] { er.DirectionX, er.DirectionY };
            CreateEffect(er.BlockIndex, er.EffectType, er.Polar, directions, er.Duration, er.NormalizedGain, er.SamplePeriod, 0);//, er.TriggerBtn, er.TriggerRepeatInterval);
        }

        public void CreateEffect(int blockIndex, EffectType effectType, bool polar, int[] dirs, int duration = -1, int gain = 10000, int samplePeriod = 0, int startDelay = 0, int triggerButton = -1, int triggerRepeatInterval = 0)
        {
            CheckFfbSupport("Unable to create effect");

            effectParams[blockIndex] = new EffectParameters()
            {
                Duration = duration,
                Flags = EffectFlags.ObjectIds | (polar ? EffectFlags.Polar : EffectFlags.Cartesian),
                Gain = gain,
                SamplePeriod = samplePeriod,
                StartDelay = startDelay,
                TriggerButton = triggerButton,
                TriggerRepeatInterval = triggerRepeatInterval,
                Envelope = null
            };

            effectParams[blockIndex].SetAxes(Axes, dirs);

            CreateEffect(blockIndex, effectType);
        }

        public void SetConstantForce(int blockIndex, int magnitude)
        {
            CheckFfbSupport("Unable to set constant force");

            if (Effects[blockIndex] == null)
                //As discussed in the SetEffectParams method, Set<EffectType> is called before an effect is created. So, instead of throwing an exception, just ignore.
                return;// throw new Exception("No effect has been created in block " + blockIndex);

            effectParams[blockIndex].Parameters.AsConstantForce().Magnitude = magnitude;
            Effects[blockIndex].SetParameters(effectParams[blockIndex], EffectParameterFlags.TypeSpecificParameters);
        }

        //TODO: add Set functions for the other types: SetPeriodicForce, SetContionSet (SetConditionalForce?), and SetRampForce

        public void OperateEffect(int blockIndex, EffectOperation effectOperation, int loopCount = 0)
        {
            CheckFfbSupport("Unable to operate effect");

            if (Effects[blockIndex] == null)
                throw new Exception("No effect has been created in block " + blockIndex);

            switch (effectOperation)
            {
                case EffectOperation.Start:
                    Effects[blockIndex].Start(loopCount);
                    break;
                case EffectOperation.SoloStart:
                    Effects[blockIndex].Start(loopCount, EffectPlayFlags.Solo);
                    break;
                case EffectOperation.Stop:
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

        #region FFB helper functions

        /// <summary>
        /// Sets empty TypeSpecificParameters, and determines whether the current effect may need to be disposed
        /// </summary>
        /// <param name="blockIndex"></param>
        /// <param name="type">The <see cref="EffectType"/> to create an effect for</param>
        private void CreateEffect(int blockIndex, EffectType type)
        {
            Console.WriteLine("Creating effect: {0}", type);

            //Construct empty TypeSpecificParameters (without, SetParameters throws an exception)
            effectParams[blockIndex].Parameters = GetTypeSpecificParameter(type);

            var eGuid = GetEffectGuid(type);

            var createdEffects = joystick.CreatedEffects.ToArray();

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
            }

            //if an effect already exists, dispose it (do not attempt to check whether it's still valid and use SetParameters(.., EffectParameterFlags.All), because that'll crash.
            if (Effects[blockIndex] != null && !Effects[blockIndex].Disposed)
                Effects[blockIndex].Dispose();

            try
            {
                Effects[blockIndex] = new Effect(joystick, eGuid, effectParams[blockIndex]);
            } catch (Exception e)
            {
                throw new Exception("Unable to create new effect: " + e.Message, e);
            }
        }
        private Guid GetEffectGuid(EffectType et)
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

        private static TypeSpecificParameters GetTypeSpecificParameter(EffectType effectType)
        {
            switch (effectType)
            {
                case EffectType.ConstantForce:
                    return new ConstantForce();
                case EffectType.Ramp:
                    return new RampForce();
                case EffectType.Square:
                case EffectType.Sine:
                case EffectType.Triangle:
                case EffectType.SawtoothUp:
                case EffectType.SawtoothDown:
                    return new PeriodicForce();
                case EffectType.Spring:
                case EffectType.Damper:
                case EffectType.Inertia:
                case EffectType.Friction:
                    return new ConditionSet();
                case EffectType.CustomForce:
                    return new CustomForce();
                default:
                    return null;
            }
        }

        private static Dictionary<Guid, EffectType> GuidToEffectType = Enum.GetValues(typeof(EffectType)).Cast<EffectType>().ToDictionary(EffectTypeGuidMap);

        private static Guid EffectTypeGuidMap(EffectType et)
        {
            switch (et)
            {
                case EffectType.ConstantForce:
                    return EffectGuid.ConstantForce;
                case EffectType.Ramp:
                    return EffectGuid.RampForce;
                case EffectType.Square:
                    return EffectGuid.Square;
                case EffectType.Sine:
                    return EffectGuid.Sine;
                case EffectType.Triangle:
                    return EffectGuid.Triangle;
                case EffectType.SawtoothUp:
                    return EffectGuid.SawtoothUp;
                case EffectType.SawtoothDown:
                    return EffectGuid.SawtoothDown;
                case EffectType.Spring:
                    return EffectGuid.Spring;
                case EffectType.Damper:
                    return EffectGuid.Damper;
                case EffectType.Inertia:
                    return EffectGuid.Inertia;
                case EffectType.Friction:
                    return EffectGuid.Friction;
                case EffectType.CustomForce:
                    return EffectGuid.CustomForce;
                case EffectType.None:
                default:
                    return new Guid();
            }
        }
        #endregion
    }
}
