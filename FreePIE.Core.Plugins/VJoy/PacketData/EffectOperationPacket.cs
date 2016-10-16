using System;
using System.Runtime.InteropServices;

namespace FreePIE.Core.Plugins.VJoy.PacketData
{
    [StructLayout(LayoutKind.Explicit)]
    public struct EffectOperationPacket : IFfbPacketData
    {
        [FieldOffset(0)]
        public byte IdxAndPacketType;
        [FieldOffset(1)]
        public byte BlockIndex;
        [FieldOffset(2)]
        public EffectOperation Operation;
        [FieldOffset(3)]
        public sbyte LoopCount;

        public override string ToString()
        {
            return string.Format("EffectOperation: {0}{1}LoopCount: {2}", Operation, Environment.NewLine, LoopCount);
        }
    }
}
