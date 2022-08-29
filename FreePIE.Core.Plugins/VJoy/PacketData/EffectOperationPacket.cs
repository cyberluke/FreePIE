using System;
using System.Runtime.InteropServices;
using vJoyInterfaceWrap;

namespace FreePIE.Core.Plugins.VJoy.PacketData
{

    public struct EffectOperationPacket : IFfbPacketData
    {
        public int DeviceId;
        public FFBPType PacketType;
        public int BlockIndex;
        public vJoy.FFB_EFF_OP Effect;

        public override string ToString()
        {
            return string.Format("EffectOperation: {0}{1}LoopCount: {2}", Effect.EffectOp, Environment.NewLine, Effect.LoopCount);
        }

        public void fromPacket(IntPtr data, int cmd)
        {
            Effect = new vJoy.FFB_EFF_OP();
            if ((uint)ERROR.ERROR_SUCCESS != VJoyUtils.Joystick.Ffb_dp_EffOp(data, ref Effect, cmd))
            {
                throw new Exception("Could not parse incoming packet as Effect Operation Report from VJoy.");
            }
        }
    }
}
