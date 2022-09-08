using FreePIE.Core.Plugins.VJoy.PacketData;
using System;
using System.Runtime.InteropServices;
using System.Text;
using vJoyInterfaceWrap;

namespace FreePIE.Core.Plugins.VJoy
{
    /// <summary>
    /// Wraps around an FFB packet. Provides packet information and helpful functions.
    /// </summary>
    public class FfbPacket
    {

        private unsafe byte[] newData;

        private int deviceId;
        private FFBPType packetType;
        private int blockIndex;

        int size;
        int cmd;
        IntPtr packetPtrCopy;

        /*~FfbPacket()
        {
            newData = null;
            packetPtrCopy = IntPtr.Zero;
        }*/

        public FfbPacket(IntPtr packetPtr)
        {
            ClonePacket(packetPtr);
            Console.WriteLine("Type: {0}, Data: {1}", packetType.ToString(), new String(VJoyUtils.bytesToHex(newData)));
        }

        public void ClonePacket(IntPtr data)
        {
            unsafe
            {
                InternalFfbPacket* FfbData = (InternalFfbPacket*)data;
                size = FfbData->DataSize;
                cmd = (int)FfbData->Command;

                byte* bytes = (byte*)FfbData->PtrToData;
                newData = new byte[size];
                for (int i = 0; i < size; i++)
                {
                    newData[i] = bytes[i];
                }
                FFBPType type = FFBPType.PT_STATEREP;            
                VJoyUtils.Joystick.Ffb_h_Type(data, ref type);
                PacketType = type;                
            }
        }

        public byte[] getRawData()
        {
            return newData;
        }

        unsafe public void Init(InternalFfbPacket inMemoryPacket) {
            inMemoryPacket.DataSize = size;
            inMemoryPacket.Command = (cmd == (int)CommandType.IOCTL_HID_SET_FEATURE ? CommandType.IOCTL_HID_SET_FEATURE : CommandType.IOCTL_HID_WRITE_REPORT);
            fixed (byte* newData2 = newData) {
                inMemoryPacket.PtrToData = (IntPtr)newData2;
            }

            packetPtrCopy = inMemoryPacket.PtrToData;

            //Read out the first two bytes (into the base packetData class), so we can fill out the 'important' information
            uint effectId = 0;
            VJoyUtils.Joystick.Ffb_dp_EffectBlockIndex(packetPtrCopy, ref effectId, cmd);
            BlockIndex = (int) effectId;

            uint deviceId = 0;
            VJoyUtils.Joystick.Ffb_dp_DeviceID(packetPtrCopy, ref deviceId);
            DeviceId = (int)deviceId;
            if (DeviceId < 1 || DeviceId > 16)
                throw new Exception(string.Format("DeviceID out of range: {0} (should be inbetween 1 and 16)", DeviceId));
               
        }

        public IFfbPacketData GetPacketData(FFBPType packetType)
        {
            switch (packetType)
            {
                case FFBPType.PT_EFFREP:
                    EffectReportPacket effPacket = new EffectReportPacket();
                    effPacket.BlockIndex = BlockIndex;
                    effPacket.DeviceId = DeviceId;
                    effPacket.PacketType = packetType;
                    effPacket.fromPacket(packetPtrCopy, cmd);
                    return effPacket;
                case FFBPType.PT_CONSTREP:
                    ConstantForcePacket constPacket = new ConstantForcePacket();
                    constPacket.BlockIndex = BlockIndex;
                    constPacket.DeviceId = DeviceId;
                    constPacket.PacketType = packetType;
                    constPacket.fromPacket(packetPtrCopy, cmd);
                    return constPacket;
                case FFBPType.PT_EFOPREP:
                    EffectOperationPacket effOpPacket = new EffectOperationPacket();
                    effOpPacket.BlockIndex = BlockIndex;
                    effOpPacket.DeviceId = DeviceId;
                    effOpPacket.PacketType = packetType;
                    effOpPacket.fromPacket(packetPtrCopy, cmd);
                    return effOpPacket;
                case FFBPType.PT_BLKFRREP:
                    BasePacket blkPacket = new BasePacket();
                    blkPacket.BlockIndex = BlockIndex;
                    blkPacket.DeviceId = DeviceId;
                    blkPacket.PacketType = packetType;
                    return blkPacket;
                case FFBPType.PT_CTRLREP:
                    PIDDeviceControlPacket ctrlPacket = new PIDDeviceControlPacket();
                    ctrlPacket.BlockIndex = BlockIndex;
                    ctrlPacket.DeviceId = DeviceId;
                    ctrlPacket.PacketType = packetType;
                    return ctrlPacket;
                case FFBPType.PT_GAINREP:
                    DeviceGainPacket gainPacket = new DeviceGainPacket();
                    gainPacket.BlockIndex = BlockIndex;
                    gainPacket.DeviceId = DeviceId;
                    gainPacket.PacketType = packetType;
                    return gainPacket;
                case FFBPType.PT_PRIDREP:
                    PeriodicReportPacket periodicPacket = new PeriodicReportPacket();
                    periodicPacket.BlockIndex = BlockIndex;
                    periodicPacket.DeviceId = DeviceId;
                    periodicPacket.PacketType = packetType;
                    periodicPacket.fromPacket(packetPtrCopy, cmd);
                    return periodicPacket;
                case FFBPType.PT_ENVREP:
                    EnvelopeReportPacket envelopePacket = new EnvelopeReportPacket();
                    envelopePacket.BlockIndex = BlockIndex;
                    envelopePacket.DeviceId = DeviceId;
                    envelopePacket.PacketType = packetType;
                    envelopePacket.fromPacket(packetPtrCopy, cmd);
                    return envelopePacket;
                case FFBPType.PT_CONDREP:
                    ConditionReportPacket conditionPacket = new ConditionReportPacket();
                    conditionPacket.BlockIndex = BlockIndex;
                    conditionPacket.DeviceId = DeviceId;
                    conditionPacket.PacketType = packetType;
                    conditionPacket.fromPacket(packetPtrCopy, cmd);
                    return conditionPacket;
                case FFBPType.PT_RAMPREP:
                    RampReportPacket rampPacket = new RampReportPacket();
                    rampPacket.BlockIndex = BlockIndex;
                    rampPacket.DeviceId = DeviceId;
                    rampPacket.PacketType = packetType;
                    rampPacket.fromPacket(packetPtrCopy, cmd);
                    return rampPacket;
                case FFBPType.PT_CSTMREP:
                    // Custom Force Data needs to be uncommented in VJoy source and should read correct
                    // parameters for particular vendor from registry such as this HKEY_CURRENT_USER\System\CurrentControlSet\Control\MediaProperties\PrivateProperties\Joystick\OEM\VID_046D&PID_C2A8\OEMForceFeedback\Effects\{13541C2B-8E33-11D0-9AD0-00A0C9A06E35}
                    BasePacket cstPacket = new BasePacket();
                    cstPacket.BlockIndex = BlockIndex;
                    cstPacket.DeviceId = DeviceId;
                    cstPacket.PacketType = packetType;
                    return cstPacket;
                case FFBPType.PT_SMPLREP:
                    BasePacket smplPacket = new BasePacket();
                    smplPacket.BlockIndex = BlockIndex;
                    smplPacket.DeviceId = DeviceId;
                    return smplPacket;
                case FFBPType.PT_SETCREP:
                    // Custom Report needs to be uncommented in VJoy source and should read correct
                    // parameters for particular vendor from registry such as this HKEY_CURRENT_USER\System\CurrentControlSet\Control\MediaProperties\PrivateProperties\Joystick\OEM\VID_046D&PID_C2A8\OEMForceFeedback\Effects\{13541C2B-8E33-11D0-9AD0-00A0C9A06E35}
                    BasePacket cstRepPacket = new BasePacket();
                    cstRepPacket.BlockIndex = BlockIndex;
                    cstRepPacket.DeviceId = DeviceId;
                    return cstRepPacket;
                case FFBPType.PT_BLKLDREP:
                    DeviceReportPacket blkldPacket = new DeviceReportPacket();
                    blkldPacket.BlockIndex = BlockIndex;
                    blkldPacket.DeviceId = DeviceId;
                    blkldPacket.PacketType = packetType;
                    blkldPacket.fromPacket(packetPtrCopy, cmd);
                    return blkldPacket;
                case FFBPType.PT_POOLREP:
                    DeviceReportPacket poolPacket = new DeviceReportPacket();
                    poolPacket.BlockIndex = BlockIndex;
                    poolPacket.DeviceId = DeviceId;
                    poolPacket.PacketType = packetType;
                    poolPacket.fromPacket(packetPtrCopy, cmd);
                    return poolPacket;
                case FFBPType.PT_STATEREP:
                    DeviceReportPacket statePacket = new DeviceReportPacket();
                    statePacket.BlockIndex = BlockIndex;
                    statePacket.DeviceId = DeviceId;
                    statePacket.PacketType = packetType;
                    statePacket.fromPacket(packetPtrCopy, cmd);
                    return statePacket;
                case FFBPType.PT_NEWEFREP:
                    CreateNewEffectPacket newEfPacket = new CreateNewEffectPacket();
                    newEfPacket.BlockIndex = BlockIndex;
                    newEfPacket.DeviceId = DeviceId;
                    newEfPacket.PacketType = packetType;
                    newEfPacket.fromPacket(packetPtrCopy, cmd);
                    return newEfPacket;
                default:
                    Console.WriteLine("Cannot get packet type, cannot create mapping for {0}", packetType.ToString());
                    return null;
            }
        }

        public override string ToString()
        {
            return string.Format("BlockIdx: {1}{0}Device ID: {2}{0}Packet type: {3}",
                Environment.NewLine,
                BlockIndex,
                DeviceId,
                PacketType);
        }

        public int DeviceId
        {
            get
            {
                return deviceId;
            }

            set
            {
                deviceId = value;
            }
        }

        public FFBPType PacketType
        {
            get
            {
                return packetType;
            }

            set
            {
                packetType = value;
            }
        }

        public int BlockIndex
        {
            get
            {
                return blockIndex;
            }

            set
            {
                blockIndex = value;
            }
        }
    }
}
