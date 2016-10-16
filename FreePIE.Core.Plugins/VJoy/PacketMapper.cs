using FreePIE.Core.Plugins.VJoy.PacketData;
using System;

namespace FreePIE.Core.Plugins.VJoy
{
    public class PacketMapper : AsyncActionRunner<IAsyncAction>
    {
        private PacketAction[] mapArr;
        public PacketMapper()
        {
            int len = Enum.GetNames(typeof(PacketType)).Length;
            mapArr = new PacketAction[len];
            SetupDefaultMap();
        }

        public PacketAction this[PacketType pt]
        {
            get { return mapArr[(int)pt]; }
            set { mapArr[(int)pt] = value; }
        }

        public void Enqueue(FfbPacket packet)
        {
            var pa = this[packet.PacketType];
            if (pa != null)
                Enqueue(pa.Convert(packet));
            else
                Console.Error.WriteLine("No packet action for {0}!", packet.PacketType);
        }

        private void SetupDefaultMap()
        {
            this[PacketType.Effect] = new PacketAction<EffectReportPacket>((d, p) => d.SetEffectParams(p));
            this[PacketType.Envelope] = null;
            this[PacketType.Condition] = null;
            this[PacketType.Periodic] = null;
            this[PacketType.ConstantForce] = new PacketAction<ConstantForcePacket>((d, p) => d.SetConstantForce(p.BlockIndex, p.Magnitude));
            this[PacketType.RampForce] = null;
            this[PacketType.CustomForceData] = null;
            this[PacketType.DownloadForceSample] = null;
            this[PacketType.EffectOperation] = new PacketAction<EffectOperationPacket>((d, p) => d.OperateEffect(p.BlockIndex, p.Operation, p.LoopCount));
            this[PacketType.PidBlockFree] = null;// new PacketAction<BasePacket>((d, p) => d.DisposeEffect(p.BlockIndex));
            this[PacketType.PidDeviceControl] = new PacketAction<PIDDeviceControlPacket>(null);
            this[PacketType.DeviceGain] = new PacketAction<DeviceGainPacket>((d, p) => d.Gain = p.Gain);
            this[PacketType.SetCustomForce] = null;
            this[PacketType.CreateNewEffect] = null;// new PacketAction<CreateNewEffectPacket>(null);//(d, p) => dev.CreateNewEffect(???, et);
            this[PacketType.BlockLoad] = null;
            this[PacketType.PIDPool] = null;
        }
    }
}
