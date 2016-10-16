using System.Runtime.InteropServices;

namespace FreePIE.Core.Plugins.VJoy.PacketData
{
    [StructLayout(LayoutKind.Explicit)]
    public struct ConstantForcePacket : IFfbPacketData
    {
        [FieldOffset(0)]
        public byte IdxAndPacketType;
        [FieldOffset(1)]
        public byte BlockIndex;
        [FieldOffset(2)]
        public short Magnitude;
        public override string ToString()
        {
            return string.Format("Magnitude: {0}", Magnitude);
        }
    }
}
