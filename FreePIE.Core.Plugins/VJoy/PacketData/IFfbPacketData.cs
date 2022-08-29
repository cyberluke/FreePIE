using System;
using System.Runtime.InteropServices;

namespace FreePIE.Core.Plugins.VJoy.PacketData
{
    public interface IFfbPacketData
    {
        void fromPacket(IntPtr data, int cmd);
    }

    public struct BasePacket : IFfbPacketData
    {
        public int DeviceId;
        public FFBPType PacketType;
        public int BlockIndex;

        public override string ToString()
        {
            return "Base packet";
        }

        public void fromPacket(IntPtr data, int cmd)
        {
            throw new NotImplementedException();
        }
    }
}
