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

        private static bool isRegistered;
        private static FfbPacket ffbPacket;
        private static FfbPacketAvailable wrapper;
        private delegate void FfbPacketAvailable(IntPtr returnedData, IntPtr userData);
        private static PacketMapper packetMapper;
        private static readonly HashSet<Device>[] registeredDevices = new HashSet<Device>[16];

        private static byte[] lastMessage { get; set; }

        public static void Init()
        {
            packetMapper = new PacketMapper();
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
            if (!UnsafeCompare(ffbPacket.getRawData(), lastMessage))
            {
                lastMessage = ffbPacket.getRawData();
                packetMapper.Enqueue(ffbPacket);
            } else
            {
                Console.WriteLine("<< SKIP");
            }
        }

        // Copyright (c) 2008-2013 Hafthor Stefansson
        // Distributed under the MIT/X11 software license
        // Ref: http://www.opensource.org/licenses/mit-license.php.
        static unsafe bool UnsafeCompare(byte[] a1, byte[] a2)
        {
            unchecked
            {
                if (a1 == a2) return true;
                if (a1 == null || a2 == null || a1.Length != a2.Length)
                    return false;
                fixed (byte* p1 = a1, p2 = a2)
                {
                    byte* x1 = p1, x2 = p2;
                    int l = a1.Length;
                    for (int i = 0; i < l / 8; i++, x1 += 8, x2 += 8)
                        if (*((long*)x1) != *((long*)x2)) return false;
                    if ((l & 4) != 0) { if (*((int*)x1) != *((int*)x2)) return false; x1 += 4; x2 += 4; }
                    if ((l & 2) != 0) { if (*((short*)x1) != *((short*)x2)) return false; x1 += 2; x2 += 2; }
                    if ((l & 1) != 0) if (*((byte*)x1) != *((byte*)x2)) return false;
                    return true;
                }
            }
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
