using FreePIE.Core.Plugins.Dx;
using FreePIE.Core.Plugins.VJoy.PacketData;
using System;
using System.Collections.Generic;

namespace FreePIE.Core.Plugins.VJoy
{
    public class AsyncPacketData
    {
        public FfbPacket packet;
    }
    public class AsyncPacketData<T> : AsyncPacketData
        where T : IFfbPacketData
    {
        public T convertedPacket;
        public IEnumerable<Device> devices;

        public AsyncPacketData(FfbPacket p, T cp, IEnumerable<Device> d)
        {
            packet = p;
            convertedPacket = cp;
            devices = d;
        }
    }

    public abstract class PacketAction
    {
        public abstract AsyncPacketData Convert(IEnumerable<Device> devices, FfbPacket packet);
        public abstract void Call(AsyncPacketData convertedPacket);
    }

    /// <summary>
    /// Wrapper class for extracting <typeparamref name="T"/> from an FfbPacket, and apply it to devices using the given <see cref="Action"/>
    /// </summary>
    /// <typeparam name="T"><see cref="IFfbPacketData"/> type to convert to.</typeparam>
    public class PacketAction<T> : PacketAction
        where T : IFfbPacketData
    {
        private Action<Device, T> action;
        public PacketAction(Action<Device, T> act)
        {
            action = act;
        }

        public override void Call(AsyncPacketData convertedPacket)
        {
            var cp = (AsyncPacketData<T>)convertedPacket;
            try
            {
                Console.WriteLine("Forwarding {0} to all joystick(s) registered for vJoy device {1}", cp.packet.PacketType, cp.packet.DeviceId);
                foreach (var dev in cp.devices)
                    action(dev, cp.convertedPacket);
            } catch (Exception e)
            {
                Console.WriteLine("Excecption when trying to forward ffb packet {0}{1}{1}{2}", cp.packet.PacketType, Environment.NewLine, e.Message);
                //throw;
            }
        }

        public override AsyncPacketData Convert(IEnumerable<Device> devices, FfbPacket packet)
        {
            T convertedPacket = packet.GetPacketData<T>();
            Console.WriteLine(convertedPacket);
            return new AsyncPacketData<T>(packet, convertedPacket, devices);
        }
    }
}
