using System;

namespace FreePIE.Core.Plugins.VJoy.PacketData
{
    public struct CreateNewEffectPacket : IFfbPacketData
    {
        public int DeviceId;
        public FFBPType PacketType;
        public FFBEType EffectType;
        public int BlockIndex;

        public override string ToString()
        {
            return string.Format("EffectType: {0}", PacketType);
        }

        public void fromPacket(IntPtr data, int cmd)
        {
            uint newEffectId = (uint)BlockIndex;
            if ((uint)ERROR.ERROR_SUCCESS != VJoyUtils.Joystick.Ffb_dp_CreateNewEffect(data, ref EffectType, ref newEffectId, cmd))
            {
                throw new Exception("Could not parse incoming packet as Effect Operation Report from VJoy.");
            }
            if (newEffectId != (uint)BlockIndex)
            {
                throw new Exception("Possible issue with future effect block being rewritten here.");
            }
        }
    }
}
