using System;
using System.Runtime.InteropServices;
using vJoyInterfaceWrap;

namespace FreePIE.Core.Plugins.VJoy.PacketData
{

    public struct DeviceReportPacket : IFfbPacketData
    {
        public int DeviceId;
        public FFBPType PacketType;
        public int BlockIndex;
        public vJoy.FFB_DEVICE_PID Effect;

        public override string ToString()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            switch (PacketType)
            {
                case FFBPType.PT_BLKLDREP:
                    sb.AppendFormat(" >> LoadStatus: {0}\n", Effect.PIDBlockLoad.LoadStatus);
                    sb.AppendFormat(" >> RAMPoolAvailable: {0}\n", Effect.PIDBlockLoad.RAMPoolAvailable);
                    break;
                case FFBPType.PT_POOLREP:
                    sb.AppendFormat(" >> MaxSimultaneousEffects: {0}\n", Effect.PIDPool.MaxSimultaneousEffects);
                    sb.AppendFormat(" >> MemoryManagement: {0}\n", Effect.PIDPool.MemoryManagement);
                    sb.AppendFormat(" >> RAMPoolSize: {0}\n", Effect.PIDPool.RAMPoolSize);
                    break;
                case FFBPType.PT_STATEREP:
                    foreach (vJoy.FFB_PID_EFFECT_STATE_REPORT s in Effect.EffectStates)
                    {
                        sb.AppendFormat("  >> PIDEffectStateReport: {0}\n", s.PIDEffectStateReport);
                        sb.AppendFormat("  >> State: {0}\n", s.State);
                    }
                    break;
            }

            return sb.ToString();
        }

        public void fromPacket(IntPtr data, int cmd)
        {
            Effect = new vJoy.FFB_DEVICE_PID();
            if ((uint)ERROR.ERROR_SUCCESS != VJoyUtils.Joystick.FfbReadPID((uint)DeviceId, ref Effect))
            {
                throw new Exception("Could not parse incoming packet as Constant Report from VJoy.");
            }
        }
    }
}
