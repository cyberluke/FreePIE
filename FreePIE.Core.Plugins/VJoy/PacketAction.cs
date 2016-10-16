using FreePIE.Core.Plugins.Dx;
using FreePIE.Core.Plugins.VJoy.PacketData;
using System;
using System.Collections.Generic;

namespace FreePIE.Core.Plugins.VJoy
{
    public abstract class PacketAction
    {
        protected static AsyncActionRunner asyncRunner = new AsyncActionRunner();
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


        public override void Apply(IEnumerable<Device> devices, FfbPacket packet)
        {
            T convertedPacket = packet.GetPacketData<T>();
            Console.WriteLine(convertedPacket);

            //I'm not sure if running the actions async is needed. I do not own an FFB device, thus my only option is to try and forward from one vJoy device to another one, which causes everything to block when not done async. I do not know whether this is because of the inner workings of vJoy, or because of the inner workings of FFB.

            //also, convertedPacket needs to be read out here, to prevent a race condition where the packet sender could've reused the memory of the packet already before it's read. Hence why that's done here (this function is called from the *blocking* ffb packet callback).

            //continuously creating an action isn't that neat. This should be rewritten to have a single action stored inside AsyncActionRunner, which is called with parameters from the queue. Have to think about types/generics though.
            //this function needs to know about "packet" (for printing information), "convertedPacket", "devices" and "action".

            Action a = () =>
            {
                try
                {
                    Console.WriteLine("Forwarding {0} on all joystick(s) registered for vJoy device {1}", packet.PacketType, packet.DeviceId);
                    if (action != null)
                        foreach (var dev in devices)
                            action(dev, convertedPacket);
                } catch (Exception e)
                {
                    Console.WriteLine("Excecption when trying to forward ffb packet {0}{1}{1}{2}", packet.PacketType, Environment.NewLine, e.Message);
                    //throw;
                }
            };
            asyncRunner.Enqueue(a);
        }
    }
}
