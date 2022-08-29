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

        public abstract IPacketAction Convert(FfbPacket packet);
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

        public override IPacketAction Convert(FfbPacket packet)
        {
            //currently converting it anyway, so the packet is still logged to console. If there's no action, return null so nothing'll be enqueued.
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
            timestamp = DateTime.Now;
            packet = p;
        }

        private void Call()
        {
            //packet.Init();
            convertedPacket = (T)packet.GetPacketData(packet.PacketType);
            // If it breaks here it means incorrect conversion between PacketMapper definition
            // and FfbPacket.GetPacketData() method
            Console.WriteLine(convertedPacket.ToString());
            VJoyFfbWrap.ExecuteOnRegisteredDevices(this);
            Console.WriteLine("Receive->process delay: {0:N3}ms", (DateTime.Now - this.timestamp).TotalMilliseconds);
            Console.WriteLine("Forwarding {0} to all joystick(s) registered for vJoy device {1}", this.packet.PacketType, this.packet.DeviceId);
        }

        public void Call(InternalFfbPacket inMemoryPacket)
        {
            packet.Init(inMemoryPacket);
            Call();
        }

        /*public void Call(IList<ICollection<Device>> registeredDevices)
        {
            //DEBUG: print useful information
            Console.WriteLine("----------------------");
            Console.WriteLine(packet);
            Console.WriteLine(convertedPacket);
            Console.WriteLine("Receive->process delay: {0:N3}ms", (DateTime.Now - timestamp).TotalMilliseconds);
            if (action != null)
            {
                var rdevs = registeredDevices[packet.DeviceId - 1];
                Console.WriteLine("Forwarding to {0} device{1}", rdevs.Count, rdevs.Count > 1 ? "s" : "");
                try
                {
                    foreach (var dev in rdevs)
                        action(dev, convertedPacket);
                } catch (Exception e)
                {
                    Console.WriteLine("Exception when trying to forward:{0}\t{1}{0}\t{2}", Environment.NewLine, e.Message, e.StackTrace);
                    //throw;
                }
            } else
                Console.WriteLine("No forwarding action defined");
        }*/
    }
}
