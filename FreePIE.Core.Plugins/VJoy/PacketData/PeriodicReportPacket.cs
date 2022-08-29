using System;
using System.Runtime.InteropServices;
using vJoyInterfaceWrap;

namespace FreePIE.Core.Plugins.VJoy.PacketData
{

    public struct PeriodicReportPacket : IFfbPacketData
    {
        public int DeviceId;
        public FFBPType PacketType;
        public int BlockIndex;
        public vJoy.FFB_EFF_PERIOD Effect;

        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.AppendFormat(" >> Magnitude: {0}\n", Effect.Magnitude);
            sb.AppendFormat(" >> Offset: {0}\n", Effect.Offset);
            sb.AppendFormat(" >> Period: {0}\n", Effect.Period);
            sb.AppendFormat(" >> Phase: {0}\n", Effect.Phase);

            return sb.ToString();
        }

        public void fromPacket(IntPtr data, int cmd)
        {
            Effect = new vJoy.FFB_EFF_PERIOD();
            if ((uint)ERROR.ERROR_SUCCESS != VJoyUtils.Joystick.Ffb_dp_Eff_Period(data, ref Effect, cmd))
            {
                throw new Exception("Could not parse incoming packet as Constant Report from VJoy.");
            }
        }
    }
}
