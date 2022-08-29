using System;
using System.Runtime.InteropServices;

namespace FreePIE.Core.Plugins.VJoy.PacketData
{
    [StructLayout(LayoutKind.Explicit)]
    public struct CreateNewEffectPacket : IFfbPacketData
    {
        [FieldOffset(0)]
        public byte IdxAndPacketType;
        [FieldOffset(1)]
        public byte BlockIndex;
        [FieldOffset(2)]
        public FFBEType Type;

        public override string ToString()
        {
            return string.Format("EffectType: {0}", Type);
        }

        public void fromPacket(IntPtr data, int cmd)
        {
            throw new NotImplementedException();
        }
    }
}
