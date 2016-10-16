using System;
using System.Collections.Generic;
using FreePIE.Core.Plugins.Strategies;
using FreePIE.Core.Plugins.VJoy;
using SlimDX.DirectInput;
using EffectType = FreePIE.Core.Plugins.VJoy.EffectType;
using System.Linq;
using FreePIE.Core.Plugins.VJoy.PacketData;

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
            this.joystick = joystick;
            SetRange(-1000, 1000);
            getPressedStrategy = new GetPressedStrategy<int>(GetDown);

            SupportsFfb = joystick.Capabilities.Flags.HasFlag(DeviceFlags.ForceFeedback);
            if (SupportsFfb)
                PrepareFFB();
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
            {
                if ((deviceObject.ObjectType & ObjectDeviceType.Axis) != 0)
                    joystick.GetObjectPropertiesById((int)deviceObject.ObjectType).SetRange(lowerRange, upperRange);
            }
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
                CheckFfbSupport("Can't set autoCenter");
                joystick.Properties.AutoCenter = value;
            }
        }

        public int Gain
        {
            get { return joystick.Properties.ForceFeedbackGain; }
            set
            {
                CheckFfbSupport("Can't set gain");
                joystick.Properties.ForceFeedbackGain = value;
            }
        }

        private void CheckFfbSupport(string message)
        {
            if (!SupportsFfb)
                throw new Exception(message + " - this device does not support FFB");
        }

        private void PrepareFFB()
        {
            List<int> ax = new List<int>();
            foreach (DeviceObjectInstance deviceObject in joystick.GetObjects())
            {
                if ((deviceObject.ObjectType & ObjectDeviceType.ForceFeedbackActuator) != 0)
                {
                    ax.Add((int)deviceObject.ObjectType);
                }
            }
            Axes = ax.ToArray();
        }

        /// <summary>
        /// Creates a simple effect and instantly initializes it with EffectParameters
        /// </summary>
        public void CreateEffect(int blockIndex, EffectType effectType, int duration, int[] dirs)
        {
            CheckFfbSupport("CreateEffect");

            effectParams[blockIndex] = new EffectParameters()
            {
                Duration = duration,
                Flags = EffectFlags.Cartesian | EffectFlags.ObjectIds,
                Gain = 10000,
                SamplePeriod = 0,
                StartDelay = 0,
                TriggerButton = -1,
                TriggerRepeatInterval = 0,
                Envelope = null
            };
            effectParams[blockIndex].SetAxes(Axes, dirs);

            effectParams[blockIndex].Parameters = GetTypeSpecificParameter(effectType);

            try
            {
                Effects[blockIndex] = new Effect(joystick, EffectTypeGuidMap(effectType), effectParams[blockIndex]);
            } catch (Exception e) { throw new Exception("Unable to create effect: " + e.Message, e); }
        }

        public void SetEffectParams(EffectReportPacket effectReport)
        {
            //This function is supposed to be a combination for multiple packets, because those packets are received in an unusual order (this is what I observed after testing with custom FFB code and games):
            //- CreateNewEffect, which only contains an EffectType (so, techincally one could create the Effect already since only the type needs to be known, but has no idea in which block to put it)
            //- Set<EffectType>, where the packet type already indicates which type it is. This packet can be used to place the effect in the correct block (since it includes a blockIdx), and we can construct the TypeSpecificParameters with this. Again, need to put those parameters "somewhere" until they can be put into the final EffectParameters
            //- Effect (this method). All information needed to construct and start an Effect is included here.

            //So - for simplicity's sake - I decided to just ignore the other two packets and start here. This means that this method is responsible for: creating an Effect; creating and filling EffectParameters; creating and setting TypeSpecificParameters on the EffectParameters; setting the EffectParameters on the Effect.

            //Also, from my testing, when new Effect is called no packets were sent, all above 3 were sent all at once when SetParamters was called (or, when new Effect was called with EffectParameters, obviously). Meaning that there's no real advantage to calling new Effect early.

            CheckFfbSupport("Can't create effect");

            int idx = effectReport.BlockIndex;

            if (Effects[idx] == null)
                throw new Exception("No effect has been created yet! Has CreateNewEffect been called?");

            //first, create EffectParameters and fill it
            effectParams[idx] = new EffectParameters()
            {
                Duration = effectReport.Duration,
                Flags = EffectFlags.ObjectIds,
                Gain = effectReport.Gain * 39,
                SamplePeriod = effectReport.SamplePeriod, //test this. Without vJoy it worked (iirc) with a value of 0. Spintires uses 0 as well, but FFBInspector has -1 as default. Can also use joystick.Capabilities.ForceFeedbackSamplePeriod
                StartDelay = 0,//TODO use data from effectReport
                TriggerButton = -1,
                TriggerRepeatInterval = 0,
                Envelope = null
            };

            //watch the incoming angle type. Both can be outputted, works just fine (and vJoy only receives Polar coordinates, so somewhere internally a conversion is made when Cartesian data is sent).
            if (effectReport.Polar)
            {
                effectParams[idx].Flags |= EffectFlags.Polar;
                //angle is in 100th degrees, so if you want to express 90 degrees (vector pointing to the right) you'll have to enter 9000
                effectParams[idx].SetAxes(Axes, new int[] { effectReport.AngleInDegrees * 100, 0 });
            } else
            {
                effectParams[idx].Flags |= EffectFlags.Cartesian;
                effectParams[idx].SetAxes(Axes, new int[] { effectReport.DirectionX, effectReport.DirectionY });
            }

            //Construct empty TypeSpecificParameters (without, SetParameters throws an exception)
            effectParams[idx].Parameters = GetTypeSpecificParameter(effectReport.EffectType);

            try
            {
                //Create new effect if it doesn't exist yet.
                var g = EffectTypeGuidMap(effectReport.EffectType);
                if (Effects[idx] == null || Effects[idx].Disposed)
                    Effects[idx] = new Effect(joystick, g);
                else if (Effects[idx].Guid != g)
                {
                    Effects[idx].Dispose();
                    Effects[idx] = new Effect(joystick, g);
                }

                //Always set parameters on it (because it hasn't been done in the constructor.
                Effects[idx].SetParameters(effectParams[idx], EffectParameterFlags.All);
            } catch (Exception e)
            {
                throw new Exception("Unable to set effect parameter: " + e.Message, e);
            }
        }

        public void SetConstantForce(int blockIndex, int magnitude)
        {
            CheckFfbSupport("Can't set constant force");

            if (Effects[blockIndex] == null)
                //As discussed in the SetEffectParams method, Set<EffectType> is called before an effect is created. So, instead of throwing an exception it just needs to ignore, for now.

                return;// throw new Exception("No effect has been created in block " + blockIndex);

            if (effectParams[blockIndex].Parameters == null)
                effectParams[blockIndex].Parameters = new ConstantForce();

            effectParams[blockIndex].Parameters.AsConstantForce().Magnitude = magnitude;

            Effects[blockIndex].SetParameters(effectParams[blockIndex], EffectParameterFlags.TypeSpecificParameters);
        }

        //TODO: add Set functions for the other types: SetPeriodicForce, SetContionSet (SetConditionalForce?), and SetRampForce

        public void OperateEffect(int blockIndex, EffectOperation effectOperation, int loopCount = 0)
        {
            CheckFfbSupport("Can't operate effect");

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

        private Guid GetEffectGuid(EffectType et)
        {
            Guid effectGuid = EffectTypeGuidMap(et);
            if (!joystick.GetEffects().Select(effectInfo => effectInfo.Guid).Contains(effectGuid))
                throw new Exception(string.Format("Joystick doesn't support {0}!", et));
            return effectGuid;
        }

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
            if (Effects[blockIndex] != null && Effects[blockIndex].Status == EffectStatus.Playing)
            {
                //OperateEffect(blockIndex, EffectOperation.Stop, 0);
                Effects[blockIndex].Dispose();
            }
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
