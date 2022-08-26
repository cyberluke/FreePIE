using System;
using System.Text;
using vJoyInterfaceWrap;

namespace FreePIE.Core.Plugins.VJoy.PacketData
{

    public struct EffectReportPacket : IFfbPacketData
    {
        public int DeviceId;
        public FFBPType PacketType;
        public int BlockIndex;
        public vJoy.FFB_EFF_REPORT Effect;


        public void fromPacket(IntPtr data)
        {
            Effect = new vJoy.FFB_EFF_REPORT();
            if ((uint)ERROR.ERROR_SUCCESS != VJoyUtils.Joystick.Ffb_h_Eff_Report(data, ref Effect))
            {
                throw new Exception("Could not parse incoming packet as EffectReport from VJoy.");
            }
        }

        public int NormalizedGain
        {
            get
            {
                // Gain as expected by DirectInput: ranging from 0 to 10000
                return ((byte)Effect.Gain * 10000) / 255;
            }
        }

        public int NormalizedAngleInDegrees
        {
            get
            {
                if (!Effect.Polar)
                    throw new NotImplementedException("This EffectReport is not in polar coordinates, no directional conversion done yet");

                // Angle as expected by DirectInput: ranging from 0 to 36000
                return Effect.Direction * 36000 / 255;
            }
        }

        public int AngleInDegrees
        {
            get
            {
                if (!Effect.Polar)
                    throw new NotImplementedException("This EffectReport is not in polar coordinates, no directional conversion done yet");
                return Effect.Direction * 360 / 255;
            }
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            string TypeStr;
            if (!VJoyUtils.EffectType2Str(Effect.EffectType, out TypeStr))
                sb.AppendFormat(" >> Effect Report: {0} {1}\n", (int)Effect.EffectType, Effect.EffectType.ToString());
            else
                sb.AppendFormat(" >> Effect Report: {0}\n", TypeStr);

            sb.AppendFormat(" >> AxisEnabledDirection: {0}\n", (ushort)Effect.AxesEnabledDirection);
            if (Effect.Polar)
            {
                sb.AppendFormat(" >> Direction: {0} deg ({1})\n", VJoyUtils.Polar2Deg(Effect.Direction), Effect.Direction);
            }
            else
            {
                sb.AppendFormat(" >> X Direction: {0}\n", Effect.DirX);
                sb.AppendFormat(" >> Y Direction: {0}\n", Effect.DirY);
            }

            if (Effect.Duration == 0xFFFF)
                sb.AppendFormat(" >> Duration: Infinite\n");
            else
                sb.AppendFormat(" >> Duration: {0} MilliSec\n", (int)(Effect.Duration));

            if (Effect.TrigerRpt == 0xFFFF)
                sb.AppendFormat(" >> Trigger Repeat: Infinite\n");
            else
                sb.AppendFormat(" >> Trigger Repeat: {0}\n", (int)(Effect.TrigerRpt));

            sb.AppendFormat("\tTrigger Button (flags): {0}\n", Effect.TrigerBtn.ToString());

            if (Effect.SamplePrd == 0xFFFF)
                sb.AppendFormat(" >> Sample Period: Infinite\n");
            else
                sb.AppendFormat(" >> Sample Period: {0}\n", (int)(Effect.SamplePrd));

            if (Effect.StartDelay == 0xFFFF)
                sb.AppendFormat(" >> Start Delay: max \n");
            else
                sb.AppendFormat(" >> Start Delay: {0}\n", (int)(Effect.StartDelay));

            sb.AppendFormat(" >> Gain: {0}%\n", VJoyUtils.Byte2Percent(Effect.Gain));

            return sb.ToString();
        }
    }
}
