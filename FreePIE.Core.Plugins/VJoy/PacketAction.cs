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
            return new AsyncPacketData<T>(action, packet);
        }
    }

    public class AsyncPacketData<T> : PacketAction<T>, IAsyncAction
            where T : IFfbPacketData
    {
        public readonly FfbPacket packet;
        public readonly T convertedPacket;

        public AsyncPacketData(Action<Device, T> a, FfbPacket p) : base(a)
        {
            packet = p;
            convertedPacket = packet.GetPacketData<T>();
            Console.WriteLine(convertedPacket);
        }

        public void Call()
        {
            VJoyFfbWrap.ExecuteOnRegisteredDevices(this);
        }
    }
}
