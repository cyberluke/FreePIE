using System;
using System.Runtime.InteropServices;
using vJoyInterfaceWrap;

namespace FreePIE.Core.Plugins.VJoy.PacketData
{

    public struct EnvelopeReportPacket : IFfbPacketData
    {
        public int DeviceId;
        public FFBPType PacketType;
        public int BlockIndex;
        public vJoy.FFB_EFF_ENVLP Effect;

        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.AppendFormat(" >> AttackLevel: {0}\n", Effect.AttackLevel);
            sb.AppendFormat(" >> AttackTime: {0}\n", Effect.AttackTime);
            sb.AppendFormat(" >> FadeLevel: {0}\n", Effect.FadeLevel);
            sb.AppendFormat(" >> FadeTime: {0}\n", Effect.FadeTime);

            return sb.ToString();
        }

        public void fromPacket(IntPtr data)
        {
            Effect = new vJoy.FFB_EFF_ENVLP();
            if ((uint)ERROR.ERROR_SUCCESS != VJoyUtils.Joystick.Ffb_h_Eff_Envlp(data, ref Effect))
            {
                throw new Exception("Could not parse incoming packet as Constant Report from VJoy.");
            }
        }
    }
}
