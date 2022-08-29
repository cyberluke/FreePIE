using FreePIE.Core.Plugins.Dx;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using FreePIE.Core.Plugins.VJoy.PacketData;

namespace FreePIE.Core.Plugins.VJoy
{
    public static class VJoyFfbWrap
    {

        private delegate void FfbPacketAvailable(IntPtr returnedData, IntPtr userData);
        private static FfbPacketAvailable wrapper;
        private static bool isRegistered;
        private static FFBPType lastPacketType;
        private static FFBEType lastEffectType;
        private static FfbPacket ffbPacket;
        private static readonly PacketMapper packetMapper = new PacketMapper();
        private static readonly HashSet<Device>[] registeredDevices = new HashSet<Device>[16];
        private static readonly ConcurrentQueue<IAction<IList<ICollection<Device>>>> queue = new ConcurrentQueue<IAction<IList<ICollection<Device>>>>();
        private static readonly BlockingCollection<IAction<IList<ICollection<Device>>>> queueWrapper;

        static VJoyFfbWrap()
        {
            queueWrapper = new BlockingCollection<IAction<IList<ICollection<Device>>>>(queue);
        }

        /// <summary>
        /// Registers a joystick for receiving packets from a vJoy device.
        /// </summary>
        /// <param name="vJoyIdx">vJoy device index where the ffb packets come from</param>
        /// <param name="dev">DI joystick to forward the ffb packets to</param>
        public static void RegisterDevice(int vJoyIdx, Device dev)
        {
            RegisterBaseCallback();

            if (registeredDevices[vJoyIdx - 1] == null)
                registeredDevices[vJoyIdx - 1] = new HashSet<Device>();

            if (!registeredDevices[vJoyIdx - 1].Add(dev))
                Console.WriteLine("That device has already been registered!");

        }

        /// <summary>
        /// Registers the base callback if not yet registered.
        /// </summary>
        public static void RegisterBaseCallback()
        {
            if (!isRegistered)
            {
                //Console.SetOut(System.IO.TextWriter.Null);
                //Console.SetError(System.IO.TextWriter.Null);
                wrapper = OnFfbPacketAvailable; //needed to keep a reference!
                _FfbRegisterGenCB(wrapper, IntPtr.Zero);
                isRegistered = true;
            }
        }

        /// <summary>
        /// Called when vJoy has a new FFB packet.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="userData"></param>
        private static void OnFfbPacketAvailable(IntPtr data, IntPtr userData)
        {
            ffbPacket = new FfbPacket(data);
            packetMapper.Enqueue(ffbPacket);
        }

        public static void ExecuteOnRegisteredDevices<T>(AsyncPacketData<T> apd)
                where T : IFfbPacketData
        {
            try
            {
                foreach (var dev in registeredDevices[apd.packet.DeviceId - 1])
                    apd.action(dev, apd.convertedPacket);
            }
            catch (Exception e)
            {
                Console.WriteLine("Excecption when trying to forward ffb packet {0}{1}{1}{2}", apd.packet.PacketType, Environment.NewLine, e.Message);
            }
        }

        /// <summary>
        /// Unregisters all devices
        /// </summary>
        public static void Reset()
        {
            foreach (var rd in registeredDevices)
                rd?.Clear();
        }

        public static void Dispose()
        {
            packetMapper.Stop();
        }

        [DllImport("vJoyInterface.dll", EntryPoint = "FfbRegisterGenCB", CallingConvention = CallingConvention.Cdecl)]
        private extern static void _FfbRegisterGenCB(FfbPacketAvailable callback, IntPtr data);
    }
}
