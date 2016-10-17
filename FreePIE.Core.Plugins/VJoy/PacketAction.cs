using FreePIE.Core.Plugins.Dx;
using FreePIE.Core.Plugins.VJoy.PacketData;
using System;
using System.Collections.Generic;

namespace FreePIE.Core.Plugins.VJoy
{
    public interface IAction<T>
    {
        void Call(T param);
    }

    public abstract class PacketAction
    {
        public abstract IAction<IList<IEnumerable<Device>>> Convert(FfbPacket packet);
    }

    /// <summary>
    /// Wrapper class for extracting <typeparamref name="T"/> from an <see cref="FfbPacket"/>, and apply it to devices using the given <see cref="action"/>
    /// </summary>
    /// <typeparam name="T"><see cref="IFfbPacketData"/> type to convert to.</typeparam>
    public class PacketAction<T> : PacketAction
        where T : IFfbPacketData
    {
        public readonly Action<Device, T> action;
        public PacketAction(Action<Device, T> act)
        {
            action = act;
        }

        public override IAction<IList<IEnumerable<Device>>> Convert(FfbPacket packet)
        {
            //XXX:
            //currently converting it anyway, so the packet is still logged to console. If there's no action, return null so nothing'll be enqueued.
            var p = new AsyncPacketData<T>(action, packet);
            if (action == null)
                return null;
            else
                return p;
        }
    }

    public class AsyncPacketData<T> : PacketAction<T>, IAction<IList<IEnumerable<Device>>>
            where T : IFfbPacketData
    {
        public readonly FfbPacket packet;
        public readonly T convertedPacket;
        private readonly DateTime timestamp;

        public AsyncPacketData(Action<Device, T> a, FfbPacket p) : base(a)
        {
            timestamp = DateTime.Now;
            packet = p;
            convertedPacket = packet.GetPacketData<T>();
            Console.WriteLine(convertedPacket);
        }

        public void Call(IList<IEnumerable<Device>> registeredDevices)
        {
            try
            {
                Console.WriteLine("Forwarding {0} to all joystick(s) registered for vJoy device {1}. Delay: {2}ms", packet.PacketType, packet.DeviceId, (DateTime.Now - timestamp).TotalMilliseconds);
                foreach (var dev in registeredDevices[packet.DeviceId - 1])
                    action(dev, convertedPacket);
            } catch (Exception e)
            {
                Console.WriteLine("Excecption when trying to forward ffb packet {0}{1}{1}{2}", packet.PacketType, Environment.NewLine, e.Message);
                //throw;
            }
        }
    }
}
