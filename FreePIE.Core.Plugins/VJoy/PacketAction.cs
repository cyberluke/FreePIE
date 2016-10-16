using FreePIE.Core.Plugins.Dx;
using FreePIE.Core.Plugins.VJoy.PacketData;
using System;

namespace FreePIE.Core.Plugins.VJoy
{
    public abstract class PacketAction
    {
        public abstract IAsyncAction Convert(FfbPacket packet);
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

        public override IAsyncAction Convert(FfbPacket packet)
        {
            T convertedPacket = packet.GetPacketData<T>();
            Console.WriteLine(convertedPacket);
            return new AsyncPacketData<T>(packet, convertedPacket, this);
        }
    }
    public class AsyncPacketData<T> : IAsyncAction
            where T : IFfbPacketData
    {
        public readonly FfbPacket packet;
        public readonly T convertedPacket;
        public readonly PacketAction<T> action;

        public AsyncPacketData(FfbPacket p, T cp, PacketAction<T> a)
        {
            packet = p;
            convertedPacket = cp;
            action = a;
        }

        public void Call()
        {
            VJoyFfbWrap.ExecuteOnRegisteredDevices(this);
        }
    }
}
