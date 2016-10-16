using FreePIE.Core.Plugins.Dx;
using FreePIE.Core.Plugins.VJoy.PacketData;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FreePIE.Core.Plugins.VJoy
{
    public abstract class PacketAction
    {
        public abstract void Apply(IEnumerable<Device> devices, FfbPacket packet);
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

        //private static Task lastTask;

        public override void Apply(IEnumerable<Device> devices, FfbPacket packet)
        {
            T convertedPacket = packet.GetPacketData<T>();
            Console.WriteLine(convertedPacket);

            /*Action a = () =>
            {
            try
            {*/
            Console.WriteLine("Executing action on all joystick(s) registered for vJoy device {0}", packet.DeviceId);
            if (action != null)
                foreach (var dev in devices)
                    action(dev, convertedPacket);
            /*
            } catch (Exception e)
            {
                Console.WriteLine("Excecption when trying to forward ffb packetType {0}{1}{1}{2}", packet.PacketType, Environment.NewLine, e.Message);
                //throw;
            }
            };

            if (lastTask == null)
                lastTask = Task.Factory.StartNew(() => a);
            else
                lastTask = lastTask.ContinueWith((bla) => a);*/
        }
    }
}
