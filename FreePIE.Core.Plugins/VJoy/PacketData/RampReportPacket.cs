using System;
using vJoyInterfaceWrap;

namespace FreePIE.Core.Plugins.VJoy.PacketData
{

    public struct RampReportPacket : IFfbPacketData
    {
        public int DeviceId;
        public FFBPType PacketType;
        public int BlockIndex;
        public vJoy.FFB_EFF_RAMP Effect;

        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.AppendFormat(" >> Start: {0}\n", Effect.Start);
            sb.AppendFormat(" >> End: {0}\n", Effect.End);

            return sb.ToString();
        }

        public void fromPacket(IntPtr data)
        {
            Effect = new vJoy.FFB_EFF_RAMP();
            if ((uint)ERROR.ERROR_SUCCESS != VJoyUtils.Joystick.Ffb_h_Eff_Ramp(data, ref Effect))
            {
                throw new Exception("Could not parse incoming packet as Constant Report from VJoy.");
            }
        }
    }
}
