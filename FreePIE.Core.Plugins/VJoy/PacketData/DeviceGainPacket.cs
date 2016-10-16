using System.Runtime.InteropServices;

namespace FreePIE.Core.Plugins.VJoy.PacketData
{
    [StructLayout(LayoutKind.Explicit)]
    public struct DeviceGainPacket : IFfbPacketData
    {
        [FieldOffset(0)]
        public byte IdxAndPacketType;
        [FieldOffset(1)]
        public byte BlockIndex;
        [FieldOffset(2)]
        public byte Gain;

        public override string ToString()
        {
            return string.Format("Gain: {0}", Gain);
        }
    }
}
