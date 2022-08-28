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
        /// <summary>
        /// HID descriptor type: feature or report
        /// </summary>
        private enum CommandType : int
        {
            IOCTL_HID_SET_FEATURE = 0xB0191,
            IOCTL_HID_WRITE_REPORT = 0xB000F
        }
        /// <summary>
        /// Aligned struct for marshaling of raw packets back to C#
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct InternalFfbPacket
        {
            public int DataSize;
            public CommandType Command;
            public IntPtr PtrToData;
        }
        //GCHandle _DataPtr;
        GCHandle _PacketPtr;
        private unsafe byte[] newData;
        private unsafe IntPtr packetPtrCopy;
        private unsafe InternalFfbPacket inMemoryPacket;

        public int DeviceId;
        public FFBPType PacketType;
        public int BlockIndex;

        ~FfbPacket()
        {
            newData = null;
            packetPtrCopy = IntPtr.Zero;
            inMemoryPacket.PtrToData = IntPtr.Zero;
            if (_PacketPtr.IsAllocated)
                _PacketPtr.Free();

        }

        public FfbPacket(IntPtr packetPtr)
        {
            ClonePacket(packetPtr);
        }

        public void ClonePacket(IntPtr data)
        {
            unsafe
            {
                InternalFfbPacket* FfbData = (InternalFfbPacket*)data;
                int size = FfbData->DataSize;
                int command = (int)FfbData->Command;
                byte* bytes = (byte*)FfbData->PtrToData;
                inMemoryPacket = new InternalFfbPacket();
                inMemoryPacket.DataSize = size;
                inMemoryPacket.Command = FfbData->Command;
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

        unsafe public void Init() {
            fixed (byte* newData2 = newData) {
                inMemoryPacket.PtrToData = (IntPtr)newData2;
            }
            _PacketPtr = GCHandle.Alloc(inMemoryPacket, GCHandleType.Pinned);
            packetPtrCopy = _PacketPtr.AddrOfPinnedObject();

            //Read out the first two bytes (into the base packetData class), so we can fill out the 'important' information
            uint effectId = 0;
            VJoyUtils.Joystick.Ffb_h_EffectBlockIndex(packetPtrCopy, ref effectId);
            BlockIndex = (int) effectId;

            uint deviceId = 0;
            VJoyUtils.Joystick.Ffb_h_DeviceID(packetPtrCopy, ref deviceId);
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
                    effPacket.fromPacket(packetPtrCopy);
                    return effPacket;
                case FFBPType.PT_CONSTREP:
                    ConstantForcePacket constPacket = new ConstantForcePacket();
                    constPacket.BlockIndex = BlockIndex;
                    constPacket.DeviceId = DeviceId;
                    constPacket.PacketType = packetType;
                    constPacket.fromPacket(packetPtrCopy);
                    return constPacket;
                case FFBPType.PT_EFOPREP:
                    EffectOperationPacket effOpPacket = new EffectOperationPacket();
                    effOpPacket.BlockIndex = BlockIndex;
                    effOpPacket.DeviceId = DeviceId;
                    effOpPacket.PacketType = packetType;
                    effOpPacket.fromPacket(packetPtrCopy);
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
                    periodicPacket.fromPacket(packetPtrCopy);
                    return periodicPacket;
                case FFBPType.PT_ENVREP:
                    EnvelopeReportPacket envelopePacket = new EnvelopeReportPacket();
                    envelopePacket.BlockIndex = BlockIndex;
                    envelopePacket.DeviceId = DeviceId;
                    envelopePacket.PacketType = packetType;
                    envelopePacket.fromPacket(packetPtrCopy);
                    return envelopePacket;
                case FFBPType.PT_CONDREP:
                    ConditionReportPacket conditionPacket = new ConditionReportPacket();
                    conditionPacket.BlockIndex = BlockIndex;
                    conditionPacket.DeviceId = DeviceId;
                    conditionPacket.PacketType = packetType;
                    conditionPacket.fromPacket(packetPtrCopy);
                    return conditionPacket;
                case FFBPType.PT_RAMPREP:
                    RampReportPacket rampPacket = new RampReportPacket();
                    rampPacket.BlockIndex = BlockIndex;
                    rampPacket.DeviceId = DeviceId;
                    rampPacket.PacketType = packetType;
                    rampPacket.fromPacket(packetPtrCopy);
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
                    blkldPacket.fromPacket(packetPtrCopy);
                    return blkldPacket;
                case FFBPType.PT_POOLREP:
                    DeviceReportPacket poolPacket = new DeviceReportPacket();
                    poolPacket.BlockIndex = BlockIndex;
                    poolPacket.DeviceId = DeviceId;
                    poolPacket.PacketType = packetType;
                    poolPacket.fromPacket(packetPtrCopy);
                    return poolPacket;
                case FFBPType.PT_STATEREP:
                    DeviceReportPacket statePacket = new DeviceReportPacket();
                    statePacket.BlockIndex = BlockIndex;
                    statePacket.DeviceId = DeviceId;
                    statePacket.PacketType = packetType;
                    statePacket.fromPacket(packetPtrCopy);
                    return statePacket;
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
    }
}
