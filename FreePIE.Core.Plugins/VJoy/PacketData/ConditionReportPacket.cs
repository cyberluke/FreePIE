using System;
using System.Runtime.InteropServices;
using vJoyInterfaceWrap;

namespace FreePIE.Core.Plugins.VJoy.PacketData
{

    public struct ConditionReportPacket : IFfbPacketData
    {
        public int DeviceId;
        public FFBPType PacketType;
        public int BlockIndex;
        public vJoy.FFB_EFF_COND Effect;

        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.AppendFormat(" >> CenterPointOffset: {0}\n", Effect.CenterPointOffset);
            sb.AppendFormat(" >> DeadBand: {0}\n", Effect.DeadBand);
            sb.AppendFormat(" >> isY: {0}\n", Effect.isY);
            sb.AppendFormat(" >> NegCoeff: {0}\n", Effect.NegCoeff);
            sb.AppendFormat(" >> NegSatur: {0}\n", Effect.NegSatur);
            sb.AppendFormat(" >> PosCoeff: {0}\n", Effect.PosCoeff);
            sb.AppendFormat(" >> PosSatur: {0}\n", Effect.PosSatur);

            return sb.ToString();
        }

        public void fromPacket(IntPtr data)
        {
            Effect = new vJoy.FFB_EFF_COND();
            if ((uint)ERROR.ERROR_SUCCESS != VJoyUtils.Joystick.Ffb_h_Eff_Cond(data, ref Effect))
            {
                throw new Exception("Could not parse incoming packet as Constant Report from VJoy.");
            }
        }
    }
}
