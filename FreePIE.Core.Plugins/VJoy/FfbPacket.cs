using FreePIE.Core.Plugins.VJoy.PacketData;
using System;
using System.Runtime.InteropServices;

namespace FreePIE.Core.Plugins.VJoy
{
    /// <summary>
    /// Wraps around an FFB packet. Provides packet information and helpful functions.
    /// </summary>
    public class FfbPacket
    {
        private enum CommandType : int
        {
            IOCTL_HID_SET_FEATURE = 0xB0191,
            IOCTL_HID_WRITE_REPORT = 0xB000F
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct InternalFfbPacket
        {
            public int DataSize;
            public CommandType Command;
            public IntPtr PtrToData;

        }

        private readonly InternalFfbPacket packet;
        private readonly BasePacket basePacketData;

        public int DeviceId { get; }
        public PacketType PacketType { get; }
        public int BlockIndex { get; }

        public FfbPacket(IntPtr packetPtr)
        {
            //copy ffb packet to managed structure
            packet = (InternalFfbPacket)Marshal.PtrToStructure(packetPtr, typeof(InternalFfbPacket));

            //A packet contains only a tiny bit of information, and a pointer to the actual FFB data which is interesting as well.   
            if (packet.DataSize < 10)
                throw new Exception(string.Format("DataSize incorrect, {0}", packet.DataSize));

            //Read out the first two bytes (into the base packetData class), so we can fill out the 'important' information
            basePacketData = GetPacketData<BasePacket>();
            BlockIndex = basePacketData.BlockIndex;
            DeviceId = (basePacketData.IdxAndPacketType & 0xF0) >> 4;
            if (DeviceId < 1)
                throw new Exception(string.Format("DeviceID out of range, {0}", DeviceId));
            PacketType = (PacketType)(basePacketData.IdxAndPacketType & 0x0F + (packet.Command == CommandType.IOCTL_HID_SET_FEATURE ? 0x10 : 0));

            //DEBUG: print useful information
            Console.WriteLine("----------------------");
            Console.WriteLine("DataSize: {0}, CMD: {1}", packet.DataSize, packet.Command);
            Console.WriteLine(Data.ToHexString());
            Console.WriteLine("BlockIdx: {0}", BlockIndex);
            Console.WriteLine("Device ID: {0}", DeviceId);
            Console.WriteLine("Packet type: {0}", PacketType);
        }

        public byte[] Data
        {
            get
            {
                byte[] outBuffer = new byte[packet.DataSize];
                Marshal.Copy(packet.PtrToData, outBuffer, 0, packet.DataSize - 8);//last 8 bytes are not interesting? (haven't seen them in use anywhere anyway)
                return outBuffer;
            }
        }

        public T GetPacketData<T>()
        where T : IFfbPacketData
        {
            return (T)Marshal.PtrToStructure(packet.PtrToData, typeof(T));
        }

        public void CopyPacketData<T>(T originalPacket)
        where T : IFfbPacketData
        {
            Marshal.PtrToStructure(packet.PtrToData, originalPacket);
        }
    }
}
