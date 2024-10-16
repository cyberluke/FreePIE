﻿using System;
using System.Runtime.InteropServices;
using vJoyInterfaceWrap;

namespace FreePIE.Core.Plugins.VJoy.PacketData
{

    public struct PIDDeviceControlPacket : IFfbPacketData
    {
        public int DeviceId;
        public FFBPType PacketType;
        public int BlockIndex;
        public FFB_CTRL DeviceControl;

        public override string ToString()
        {
            return string.Format("PIDDeviceControl: {0}", DeviceControl); 
        }

        public void fromPacket(IntPtr data, int cmd)
        {
            DeviceControl = new FFB_CTRL();
            if ((uint)ERROR.ERROR_SUCCESS != VJoyUtils.Joystick.Ffb_dp_DevCtrl(data, ref DeviceControl, cmd))
            {
                throw new Exception("Could not parse incoming packet as Constant Report from VJoy.");
            }
        }
    }
}
