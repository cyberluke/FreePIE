using System;
using System.Collections.Generic;
using FreePIE.Core.Plugins.Strategies;
using SlimDX.DirectInput;
using System.Linq;
using FreePIE.Core.Plugins.VJoy.PacketData;
using System.Threading;
using System.Globalization;
using System.Text;
using FreePIE.Core.Plugins.VJoy;
using static FreePIE.Core.Plugins.Logitech;

namespace FreePIE.Core.Plugins.Dx
{
    public class Device : IDisposable
    {
        private const int BlockSize = 8;
        private Effect[] Effects = new Effect[BlockSize];
        private EffectParameters[] effectParams = new EffectParameters[BlockSize];
        private EffectParameterFlags[] effectFlags = new EffectParameterFlags[BlockSize];
        private int[] effectLastDir = new int[BlockSize];
        private int[] Axes;

        public string Name { get { return joystick.Properties.ProductName; } }
        public Guid InstanceGuid { get { return joystick.Information.InstanceGuid; } }
        public bool SupportsFfb { get; }
        private ConditionSet conditionSet;

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

            initializeConditionForce();
        }

        #region Properties (getters, setters)

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

        #endregion

        protected Envelope? getEnvelope(EffectReportPacket er)
        {
            if (effectParams[er.BlockIndex].Envelope.HasValue)
            {
                Envelope envelope = new Envelope();
                envelope.AttackLevel = effectParams[er.BlockIndex].Envelope.Value.AttackLevel;
                envelope.AttackTime = effectParams[er.BlockIndex].Envelope.Value.AttackTime;
                envelope.FadeLevel = effectParams[er.BlockIndex].Envelope.Value.FadeLevel;
                envelope.FadeTime = effectParams[er.BlockIndex].Envelope.Value.FadeTime;
                return envelope;
            }
            else
            {
                return null;
            }
        }

        public void initializeConditionForce()
        {
            // Setup and reuse parameters for ConditionReportPacket
            // the real hardware might have X a Y axis switched
            conditionSet = new ConditionSet();
            conditionSet.AsConditionSet().Conditions = new Condition[2];
        }

        public void SetEffectParams(EffectReportPacket er)
        {
            CheckFfbSupport("Unable to create effect");

            //angle is in 100th degrees, so if you want to express 90 degrees (vector pointing to the right) you'll have to enter 9000
            var directions = er.Effect.Polar ? new int[] { (VJoyUtils.Polar2Deg(er.Effect.Direction)) * 100, 0 } : new int[] { er.Effect.DirX, er.Effect.DirY };
            var eGuid = GetEffectGuid(er.Effect.EffectType);
            TypeSpecificParameters parametersDefault = effectParams[er.BlockIndex].Parameters;

            if (!isExistingEffect(er, eGuid))
            {
                effectParams[er.BlockIndex] = new EffectParameters();
            }

            if (effectParams[er.BlockIndex].Duration != er.Effect.Duration * 1000) {
                EffectFlagsUtil.AddTo(EffectParameterFlags.Duration, ref effectFlags[er.BlockIndex]);
                effectParams[er.BlockIndex].Duration = er.Effect.Duration * 1000;
            }
            //if (er.Effect.Duration == 65535)
            //{
            //    EffectFlagsUtil.AddTo(EffectParameterFlags.Start, ref effectFlags[er.BlockIndex]);
            //}

            effectParams[er.BlockIndex].Flags = EffectFlags.ObjectIds | (er.Effect.Polar ? EffectFlags.Polar : EffectFlags.Cartesian);

            if (effectParams[er.BlockIndex].Gain != er.NormalizedGain)
            {
                EffectFlagsUtil.AddTo(EffectParameterFlags.Gain, ref effectFlags[er.BlockIndex]);
                effectParams[er.BlockIndex].Gain = er.NormalizedGain;
            }

            if (effectParams[er.BlockIndex].SamplePeriod != er.Effect.SamplePrd * 1000)
            {
                EffectFlagsUtil.AddTo(EffectParameterFlags.SamplePeriod, ref effectFlags[er.BlockIndex]);
                effectParams[er.BlockIndex].SamplePeriod = er.Effect.SamplePrd * 1000;
            }
            if (effectParams[er.BlockIndex].StartDelay != er.Effect.StartDelay * 1000)
            {
                EffectFlagsUtil.AddTo(EffectParameterFlags.StartDelay, ref effectFlags[er.BlockIndex]);
                effectParams[er.BlockIndex].StartDelay = er.Effect.StartDelay * 1000;
            }

            //effectParams[er.BlockIndex].TriggerButton = er.Effect.TrigerBtn;
            //effectParams[er.BlockIndex].TriggerRepeatInterval = er.Effect.TrigerRpt;
            effectParams[er.BlockIndex].Envelope = getEnvelope(er);

            effectParams[er.BlockIndex].Parameters = parametersDefault;
            effectParams[er.BlockIndex].SetAxes(Axes, directions);
            if (effectLastDir[er.BlockIndex] != directions[0])
            {
                effectLastDir[er.BlockIndex] = directions[0];
                EffectFlagsUtil.AddTo(EffectParameterFlags.Direction, ref effectFlags[er.BlockIndex]);
            }

            if (effectParams[er.BlockIndex].TriggerButton != -1)
            {
                EffectFlagsUtil.AddTo(EffectParameterFlags.TriggerButton, ref effectFlags[er.BlockIndex]);
                effectParams[er.BlockIndex].TriggerButton = -1;
            }


            Console.WriteLine("  >> Effect Flags: {0}", VJoyUtils.FlagsEnumToString<EffectParameterFlags>(effectFlags[er.BlockIndex]));

            if (isExistingEffect(er, eGuid) && effectParams[er.BlockIndex].Parameters != null)
            {
                Console.WriteLine("param change only - try to change only new params");
                SlimDX.Result result = Effects[er.BlockIndex].SetParameters(effectParams[er.BlockIndex], effectFlags[er.BlockIndex]);
                if (result.IsSuccess)
                {
                    // clean this flag for incremental changes in next packets for same effect guid
                    effectFlags[er.BlockIndex] = EffectParameterFlags.None;

                    return;
                }
                else
                {
                    Console.WriteLine("failed setting params only => recreating whole effect block");
                }
            }

            try
            {
                if (Effects[er.BlockIndex] != null && !Effects[er.BlockIndex].Disposed)
                {
                    Effects[er.BlockIndex].Dispose();
                }

                if (effectParams[er.BlockIndex].Parameters == null)
                {
#if DEBUG
                    Console.WriteLine("!!! ERROR: Effect Parameters are empty! (missing packet?)");
#endif
                    effectParams[er.BlockIndex].Parameters = GetTypeSpecificParameter(er.Effect.EffectType);
                    if (er.Effect.EffectType.Equals(FFBEType.ET_RAMP))
                    {
                        // set default ramp force if app did not send (compat fix)
                        setRamp(er.BlockIndex, 10000, -10000);
                    }
                    Effects[er.BlockIndex] = new Effect(joystick, eGuid, effectParams[er.BlockIndex]);
                }
                else
                {
                    Effects[er.BlockIndex] = new Effect(joystick, eGuid, effectParams[er.BlockIndex]);
                }

                // clean this flag for incremental changes in next packets for same effect guid
                effectFlags[er.BlockIndex] = EffectParameterFlags.None;

            }
            catch (Exception e)
            {
                throw new Exception("Unable to create new effect: " + e.Message, e);
            }
        }

        public void CreateEffect(int blockIndex, FFBEType type)
        {
            var eGuid = GetEffectGuid(type);
            Effects[blockIndex] = new Effect(joystick, eGuid);
            effectParams[blockIndex] = new EffectParameters();
            effectFlags[blockIndex] = EffectParameterFlags.All;
            if (type.Equals(FFBEType.ET_SPRNG))
            {
                initializeConditionForce();
                effectParams[blockIndex].Parameters = conditionSet;
            }
        }

        protected bool isExistingEffect(EffectReportPacket er, Guid eGuid)
        {
            if (Effects[er.BlockIndex] != null && !Effects[er.BlockIndex].Disposed)
            {
                if (Effects[er.BlockIndex].Guid.Equals(eGuid))
                {
                    return true;
                }
            }
            return false;
        }

        #region Effect Parameters (individual packets)
        public void SetEnvelope(int blockIndex, int attackLevel, int attackTime, int fadeLevel, int fadeTime)
        {
            Envelope envelope = new Envelope();
            envelope.AttackLevel = attackLevel;
            envelope.AttackTime = attackTime;
            envelope.FadeLevel = fadeLevel;
            envelope.FadeTime = fadeTime;
            effectParams[blockIndex].Envelope = envelope;

            EffectFlagsUtil.AddTo(EffectParameterFlags.Envelope, ref effectFlags[blockIndex]);
        }


        public void setRamp(int blockIndex, int start, int end)
        {
            CheckFfbSupport("Unable to set constant force");

            effectParams[blockIndex].Parameters = new RampForce();

            effectParams[blockIndex].Parameters.AsRampForce().Start = start;
            effectParams[blockIndex].Parameters.AsRampForce().End = end;

            EffectFlagsUtil.AddTo(EffectParameterFlags.TypeSpecificParameters, ref effectFlags[blockIndex]);
        }

        public void setConditionReport(int blockIndex, int centerPointOffset, int deadBand, bool isY, int negCoeff, int negSatur, int posCoeff, int posSatur)
        {
            CheckFfbSupport("Unable to set constant force");

            int lastConditionId = isY ? 1 : 0;

            if (isAxisReverse)
            {
                //G940 is reversed here
                lastConditionId = isY ? 0 : 1;
            }
            effectParams[blockIndex].Parameters.AsConditionSet().Conditions[lastConditionId].Offset = centerPointOffset;
            effectParams[blockIndex].Parameters.AsConditionSet().Conditions[lastConditionId].DeadBand = deadBand;
            effectParams[blockIndex].Parameters.AsConditionSet().Conditions[lastConditionId].NegativeCoefficient = negCoeff;
            effectParams[blockIndex].Parameters.AsConditionSet().Conditions[lastConditionId].NegativeSaturation = negSatur;
            effectParams[blockIndex].Parameters.AsConditionSet().Conditions[lastConditionId].PositiveCoefficient = posCoeff;
            effectParams[blockIndex].Parameters.AsConditionSet().Conditions[lastConditionId].PositiveSaturation = posSatur;

            EffectFlagsUtil.AddTo(EffectParameterFlags.Start, ref effectFlags[blockIndex]);
            EffectFlagsUtil.AddTo(EffectParameterFlags.TypeSpecificParameters, ref effectFlags[blockIndex]);
        }


        public void SetPeriodicForce(int blockIndex, int magnitude, int offset, int period, int phase)
        {
            CheckFfbSupport("Unable to set periodic force");

            effectParams[blockIndex].Parameters = new PeriodicForce();
            effectParams[blockIndex].Parameters.AsPeriodicForce().Magnitude = magnitude;
            effectParams[blockIndex].Parameters.AsPeriodicForce().Offset = offset;
            effectParams[blockIndex].Parameters.AsPeriodicForce().Period = period * 1000;
            effectParams[blockIndex].Parameters.AsPeriodicForce().Phase = phase;

            EffectFlagsUtil.AddTo(EffectParameterFlags.TypeSpecificParameters, ref effectFlags[blockIndex]);
        }

        public void SetConstantForce(int blockIndex, int magnitude)
        {
            CheckFfbSupport("Unable to set constant force");

            effectParams[blockIndex].Parameters = GetTypeSpecificParameter(FFBEType.ET_CONST);
            effectParams[blockIndex].Parameters.AsConstantForce().Magnitude = magnitude;

            EffectFlagsUtil.AddTo(EffectParameterFlags.TypeSpecificParameters, ref effectFlags[blockIndex]);
        }
        #endregion

        #region Effect Operations (play, stop, download, unload)

        public void DownloadToDevice(int blockIndex)
        {
            Effects[blockIndex].Download();
        }

        public void OperateEffect(int blockIndex, FFBOP effectOperation, int loopCount = 0)
        {

            CheckFfbSupport("Unable to operate effect");

            if (Effects[blockIndex] == null)
            {
#if DEBUG
                Console.WriteLine("No effect has been created in block " + blockIndex);
#endif
                return;
            }

            switch (effectOperation)
            {
                case FFBOP.EFF_START:
                    Effects[blockIndex].Start(loopCount);
                    break;
                case FFBOP.EFF_SOLO:
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
        #endregion

        #region dispose functions
        public void Dispose()
        {
            DisposeEffects();
            joystick.Dispose();
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

        private Guid GetEffectGuid(FFBEType et)
        {
            Guid effectGuid = EffectTypeGuidMap(et);
            if (!joystick.GetEffects().Any(effectInfo => effectInfo.Guid == effectGuid))
                throw new Exception(string.Format("Joystick doesn't support {0}!", et));
            return effectGuid;
        }

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
        private bool isAxisReverse;

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
            if (ax.Capacity >= 2)
            {
                /**
                 *  G940
                     AxisEnabledDirection
                     [0] = 16777474
                     [1] = 16777218
 
                     Vjoy
                     [0] = 16777218
                     [1] = 16777474
                */
                if (ax[0] == 16777474 && ax[1] == 16777218)
                {
                    Console.WriteLine("Reversed Axis detected. Enabling automatic XY axes reversed position in effect buffer queue");
                    ax.Reverse();
                    isAxisReverse = true;
                }
            }
            Axes = ax.ToArray();
        }

        public bool isG940Throttle()
        {
            return Name == "Logitech G940 Throttle";
        }

        public void setG940LED(int button, LogiColor color)
        {
            if (!isG940Throttle())
            {
                return;
            }

            unsafe
            {
                ButtonSetColor((IntPtr)joystick.InternalPointer, (LogiPanelButton)(button - 1), color);
            }
        }

        public void setAllG940LED(LogiColor color)
        {
            if (!isG940Throttle())
            {
                return;
            }

            unsafe
            {
                SetAllButtonsColor((IntPtr)joystick.InternalPointer, color);
            }
        }

        public bool isG940LED(int button, LogiColor color)
        {
            if (!isG940Throttle())
            {
                return false;
            }

            unsafe
            {
                return IsButtonColor((IntPtr)joystick.InternalPointer, (LogiPanelButton)(button - 1), color);
            }
        }

        public void printSupportedEffects()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat(" >> Device: {0}\n", Name);
            sb.AppendFormat(" >> Device GUID: {0}\n", InstanceGuid);
            sb.AppendFormat(" >> Device Supports FFB: {0}\n", SupportsFfb);

            foreach (EffectInfo effect in joystick.GetEffects())
            {
                sb.AppendFormat(" >> Effect Name: {0}\n", effect.Name);
                sb.AppendFormat(" >> Effect Type: {0}\n", effect.Type);
                sb.AppendFormat(" >> Effect Guid: {0}\n", effect.Guid);
                sb.AppendFormat(" >> Static Parameters: {0}\n", effect.StaticParameters.ToString("g"));
                sb.AppendFormat(" >> Dynamic Parameters: {0}\n", effect.DynamicParameters.ToString("g"));
            }

            Console.WriteLine(sb.ToString());
        }
    }
}
