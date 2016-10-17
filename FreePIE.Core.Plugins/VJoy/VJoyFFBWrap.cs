using FreePIE.Core.Plugins.Dx;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;

namespace FreePIE.Core.Plugins.VJoy
{
    public static class VJoyFfbWrap
    {

        private delegate void FfbPacketAvailable(IntPtr returnedData, IntPtr userData);
        private static FfbPacketAvailable wrapper;
        private static bool isRegistered;

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
            var pa = packetMapper[ffbPacket.PacketType];
            if (pa != null)
                queueWrapper.Add(pa.Convert(ffbPacket));
            else
                Console.WriteLine("No packet action for {0}", ffbPacket.PacketType);
        }

        public static void HandleQueuedPackets()
        {
            IAction<IList<ICollection<Device>>> action = null;
            while (queueWrapper.TryTake(out action))
                action.Call(registeredDevices);
        }

        /// <summary>
        /// Unregisters all devices
        /// </summary>
        public static void Reset()
        {
            for (int i = 0; i < registeredDevices.Length; i++)
                registeredDevices[i]?.Clear();
        }

        [DllImport("vJoyInterface.dll", EntryPoint = "FfbRegisterGenCB", CallingConvention = CallingConvention.Cdecl)]
        private extern static void _FfbRegisterGenCB(FfbPacketAvailable callback, IntPtr data);
    }
}
