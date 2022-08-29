using System;
using System.Runtime.InteropServices;
using vJoyInterfaceWrap;

namespace FreePIE.Core.Plugins.VJoy.PacketData
{

    public struct ConstantForcePacket : IFfbPacketData
    {
        public int DeviceId;
        public FFBPType PacketType;
        public int BlockIndex;
        public vJoy.FFB_EFF_CONSTANT Effect;

        public override string ToString()
        {
            return string.Format("Magnitude: {0}", Effect.Magnitude);
        }

        public void fromPacket(IntPtr data, int cmd)
        {
            Effect = new vJoy.FFB_EFF_CONSTANT();
            if ((uint)ERROR.ERROR_SUCCESS != VJoyUtils.Joystick.Ffb_dp_Eff_Constant(data, ref Effect, cmd))
            {
                throw new Exception("Could not parse incoming packet as Constant Report from VJoy.");
            }
        }
    }
}
