using System.Runtime.InteropServices;

namespace FreePIE.Core.Plugins.VJoy.PacketData
{
    public interface IFfbPacketData
    {
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct BasePacket : IFfbPacketData
    {
        [FieldOffset(0)]
        public byte IdxAndPacketType;
        [FieldOffset(1)]
        public byte BlockIndex;
        public override string ToString()
        {
            return "Base packet";
        }
    }
}
