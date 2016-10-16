using System.Runtime.InteropServices;

namespace FreePIE.Core.Plugins.VJoy.PacketData
{
    [StructLayout(LayoutKind.Explicit)]
    public struct CreateNewEffectPacket : IFfbPacketData
    {
        [FieldOffset(0)]
        public byte IdxAndPacketType;
        [FieldOffset(1)]
        public EffectType Type;

        public override string ToString()
        {
            return string.Format("EffectType: {0}", Type);
        }
    }
}
