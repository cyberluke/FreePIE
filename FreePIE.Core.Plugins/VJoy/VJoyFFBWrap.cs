using FreePIE.Core.Plugins.Dx;
using FreePIE.Core.Plugins.VJoy.PacketData;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace FreePIE.Core.Plugins.VJoy
{
    public static class VJoyFfbWrap
    {

        private delegate void FfbPacketAvailable(IntPtr returnedData, IntPtr userData);
        private static PacketMapper packetMapper = new PacketMapper();
        private static FfbPacketAvailable wrapper;
        private static bool isRegistered;
        private static readonly HashSet<Device>[] registeredDevices = new HashSet<Device>[16];

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
            registeredDevices[vJoyIdx - 1].Add(dev);
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
        /// <param name="ffbDataPtr"></param>
        /// <param name="userData"></param>
        private static void OnFfbPacketAvailable(IntPtr ffbDataPtr, IntPtr userData)
        {
            FfbPacket ffbPacket = new FfbPacket(ffbDataPtr);
            packetMapper.Enqueue(ffbPacket);
        }

        public static void ExecuteOnRegisteredDevices<T>(AsyncPacketData<T> apd)
            where T : IFfbPacketData
        {
            try
            {
                Console.WriteLine("Forwarding {0} to all joystick(s) registered for vJoy device {1}", apd.packet.PacketType, apd.packet.DeviceId);
                foreach (var dev in registeredDevices[apd.packet.DeviceId - 1])
                    apd.action.action(dev, apd.convertedPacket);
            } catch (Exception e)
            {
                Console.WriteLine("Excecption when trying to forward ffb packet {0}{1}{1}{2}", apd.packet.PacketType, Environment.NewLine, e.Message);
                //throw;
            }
        }

        [DllImport("vJoyInterface.dll", EntryPoint = "FfbRegisterGenCB", CallingConvention = CallingConvention.Cdecl)]
        private extern static void _FfbRegisterGenCB(FfbPacketAvailable callback, IntPtr data);
    }
}
