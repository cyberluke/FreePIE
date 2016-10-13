using System;

namespace SCP
{

    public unsafe class XboxDevice : UsbDevice
    {
        public const string XBOX_360_GUID = "{F679F562-3164-42CE-A4DB-E7DDBE723909}";
        public readonly uint Index;
        public ScpStatusReport ControllerState;

        public bool Connected { get; private set; }

        private ControlBuffer cb;

        public XboxDevice(uint idx) : base(XBOX_360_GUID)
        {
            Index = idx;
            ControllerState = new ScpStatusReport(Index);
            cb = new ControlBuffer(Index);
            Connect();
        }

        public bool Report()
        {
            fixed (ScpStatusReport* ptr = &ControllerState)
                return IOControl(ptr, ControllerState.StructureSize, 0x2A400C);
        }

        private bool Control(uint code)
        {
            fixed (ControlBuffer* ptr = &cb)
                return IOControl(ptr, cb.StructureSize, code);
        }

        public void Connect()
        {
            Console.WriteLine("Connecting...");
            if (!Control(0x2A4000))
                Console.WriteLine($"Unable to connect Xbox controller {Index}! Already connected?");
            //throw new Exception($"Unable to connect Xbox controller {Index}!");
            Connected = true;
        }

        public void Disconnect()
        {
            Console.WriteLine("Disconnecting...");
            if (!Control(0x2A4004))
                throw new Exception($"Unable to disconnect Xbox controller {Index}!");
            Connected = false;
        }

        public void Eject()
        {
            Console.WriteLine("Ejecting...");
            if (!Control(0x2A4008))
                throw new Exception($"Unable to eject Xbox controller {Index}!");
            Connected = false;
        }

        public void Toggle()
        {
            if (Connected)
                Disconnect();
            else
                Connect();
        }
    }
}
