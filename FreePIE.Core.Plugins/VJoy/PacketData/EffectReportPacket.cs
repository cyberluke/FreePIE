using System;
using System.Runtime.InteropServices;
using System.Text;

namespace FreePIE.Core.Plugins.VJoy.PacketData
{
    [StructLayout(LayoutKind.Explicit)]
    public struct EffectReportPacket : IFfbPacketData
    {
        [FieldOffset(0)]
        public byte IdxAndPacketType;
        [FieldOffset(1)]
        public byte BlockIndex;
        [FieldOffset(2)]
        public EffectType EffectType;
        [FieldOffset(3)]
        public short Duration;
        [FieldOffset(5)]
        public short TriggerRepeatInterval;
        [FieldOffset(7)]
        public short SamplePeriod;
        [FieldOffset(9)]
        public byte Gain;
        [FieldOffset(10)]
        public sbyte TriggerBtn; //button?
        [FieldOffset(11)]
        private byte PolarByte;
        [FieldOffset(12)]
        public byte Direction;
        [FieldOffset(12)]
        public byte DirectionX;
        [FieldOffset(13)]
        public byte DirectionY;

        public bool Polar { get { return (PolarByte & 0x04) == 0x04; } }

        public int NormalizedGain
        {
            get {
                // Gain as expected by DirectInput: ranging from 0 to 10000
                return Gain * 10000 / 255;
            }
        }

        public int NormalizedAngleInDegrees
        {
            get
            {
                if (!Polar)
                    throw new NotImplementedException("This EffectReport is not in polar coordinates, no directional conversion done yet");

                // Angle as expected by DirectInput: ranging from 0 to 36000
                return Direction * 36000 / 255;
            }
        }

        public int AngleInDegrees
        {
            get
            {
                if (!Polar)
                    throw new NotImplementedException("This EffectReport is not in polar coordinates, no directional conversion done yet");
                return Direction * 360 / 255;
            }
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("\tEffectType: ");
            sb.AppendLine(EffectType.ToString());
            sb.Append("\tDuration: ");
            sb.AppendLine(Duration.ToString());
            sb.Append("\tTriggerRepeatInterval: ");
            sb.AppendLine(TriggerRepeatInterval.ToString());
            sb.Append("\tSamplePeriod: ");
            sb.AppendLine(SamplePeriod.ToString());
            sb.Append("\tGain: ");
            sb.AppendLine(Gain.ToString());
            sb.Append("\tTriggerBtn (?): ");
            sb.AppendLine(TriggerBtn.ToString());
            sb.Append("\tPolar: ");
            sb.AppendLine(Polar.ToString());
            if (Polar)
            {
                sb.Append("\tAngle: ");
                sb.AppendLine(AngleInDegrees.ToString());
            } else
            {
                sb.AppendFormat("\tX: {0:3}, Y:{1:3}\n", DirectionX, DirectionY);
            }
            return sb.ToString();
        }
    }
}
