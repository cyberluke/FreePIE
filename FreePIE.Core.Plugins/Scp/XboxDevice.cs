using System;
using System.Runtime.InteropServices;

namespace SCP
{

    public class XboxDevice : UsbDevice
    {
        public const string XBOX_360_GUID = "{F679F562-3164-42CE-A4DB-E7DDBE723909}";
        private readonly uint Index = 0;
        public bool Connected { get; private set; }
        public XboxDevice(uint idx) : base(XBOX_360_GUID)
        {
            Index = idx;
            Connect();
        }

        public bool Report(ref XboxState data)
        {
            return IOControl(data, 0x2A400C, 8);
        }

        [StructLayout(LayoutKind.Sequential, Size = 16)]
        internal struct ConnectionBuffer
        {
            uint flag;
            uint serial;
            internal ConnectionBuffer(uint s)
            {
                flag = 0x10;
                serial = s;
            }
        }

        public void Connect()
        {
            Console.WriteLine("Connecting...");
            if (!IOControl(new ConnectionBuffer(Index), 0x2A4000))
                Console.WriteLine($"Unable to connect controller {Index}! Already connected?");
            //throw new Exception($"Unable to connect Xbox controller {Index}!");
            Connected = true;
        }

        public void Disconnect()
        {
            Console.WriteLine("Disconnecting...");
            if (!IOControl(new ConnectionBuffer(Index), 0x2A4004))
                throw new Exception($"Unable to disconnect Xbox controller {Index}!");
            Connected = false;
        }

        public XboxState GetState()
        {
            return new XboxState(Index);
        }
    }
}
