using System.Runtime.InteropServices;

namespace FreePIE.Core.Plugins.VJoy.PacketData
{
    [StructLayout(LayoutKind.Explicit, Pack = 0, Size = 3)]
    public struct PIDDeviceControlPacket : IFfbPacketData
    {
        [FieldOffset(0)]
        public byte IdxAndPacketType;
        [FieldOffset(1)]
        public PidDeviceControl DeviceControl;

        public override string ToString()
        {
            return string.Format("PIDDeviceControl: {0}", DeviceControl); 
        }
    }
}
