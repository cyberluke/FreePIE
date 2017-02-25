using System.Runtime.InteropServices;

namespace FreePIE.Core.Plugins.VJoy.PacketData
{
    [StructLayout(LayoutKind.Explicit)]
    public struct DeviceGainPacket : IFfbPacketData
    {
        [FieldOffset(0)]
        public byte IdxAndPacketType;
        [FieldOffset(1)]
        public byte Gain;

        public int NormalizedGain
        {
            // Gain as expected by DirectInput: ranging from 0 to 10000
            get { return Gain * 10000 / 255; }
        }

        public override string ToString()
        {
            return string.Format("Gain: {0}", NormalizedGain);
        }
    }
}
