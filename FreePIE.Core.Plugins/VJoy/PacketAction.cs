using FreePIE.Core.Plugins.Dx;
using FreePIE.Core.Plugins.VJoy.PacketData;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FreePIE.Core.Plugins.VJoy
{
    public interface IAction<T>
    {
        void Call(T param);
    }

    public abstract class PacketAction
    {

        public abstract IPacketAction FromPacket(FfbPacket packet);
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

        public override IPacketAction FromPacket(FfbPacket packet)
        {
            var p = new AsyncPacketData<T>(action, packet);
            if (action == null)
                return null;
            else
                return p;
        }
    }

    public class AsyncPacketData<T> : PacketAction<T>, IPacketAction
            where T : IFfbPacketData
    {
        public readonly FfbPacket packet;
        public T convertedPacket;
        public readonly DateTime timestamp;

        public AsyncPacketData(Action<Device, T> a, FfbPacket p) : base(a)
        {
#if DEBUG
            timestamp = DateTime.Now;
#endif
            timestamp = DateTime.Now;
            packet = p;
        }

        private void Call()
        {
            convertedPacket = (T)packet.GetPacketData(packet.PacketType);
            // If it breaks here it means incorrect conversion between PacketMapper definition
            // and FfbPacket.GetPacketData() method
            VJoyFfbWrap.ExecuteOnRegisteredDevices(this);
#if DEBUG
            Console.WriteLine(convertedPacket.ToString());
            Console.WriteLine("Receive->process delay: {0:N3}ms", (DateTime.Now - this.timestamp).TotalMilliseconds);
            Console.WriteLine("Forwarding {0} to all joystick(s) registered for vJoy device {1}", this.packet.PacketType, this.packet.DeviceId);
#else
            Console.WriteLine("Receive->process delay: {0:N3}ms", (DateTime.Now - this.timestamp).TotalMilliseconds);
#endif
        }

        public void Call(InternalFfbPacket inMemoryPacket)
        {
            packet.Init(inMemoryPacket);
            Call();
        }

    }
}
