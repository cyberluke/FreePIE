using System;
using System.Runtime.InteropServices;
using vJoyInterfaceWrap;

namespace FreePIE.Core.Plugins.VJoy.PacketData
{

    public struct DeviceGainPacket : IFfbPacketData
    {
        public int DeviceId;
        public FFBPType PacketType;
        public int BlockIndex;
        public byte Gain;

        public override string ToString()
        {
            return string.Format("Gain: {0}%", VJoyUtils.Byte2Percent(Gain));
        }

        public int NormalizedGain
        {
            get
            {
                // Gain as expected by DirectInput: ranging from 0 to 10000
                return ((byte)Gain * 10000) / 255;
            }
        }

        public void fromPacket(IntPtr data)
        {
          
            if ((uint)ERROR.ERROR_SUCCESS != VJoyUtils.Joystick.Ffb_h_DevGain(data, ref Gain))
            {
                throw new Exception("Could not parse incoming packet as Effect Operation Report from VJoy.");
            }
        }
    }
}
