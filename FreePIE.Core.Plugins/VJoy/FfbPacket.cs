using FreePIE.Core.Plugins.VJoy.PacketData;
using System;
using System.Runtime.InteropServices;

namespace FreePIE.Core.Plugins.VJoy
{
    /// <summary>
    /// Wraps around an FFB packet. Provides packet information and helpful functions.
    /// </summary>
    public class FfbPacket
    {
        public enum CommandType : int
        {
            IOCTL_HID_SET_FEATURE = 0xB0191,
            IOCTL_HID_WRITE_REPORT = 0xB000F
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct InternalFfbPacket
        {
            public int DataSize;
            public CommandType Command;
            public IntPtr PtrToData;
        }

        public InternalFfbPacket packet;
        protected IntPtr packetPtrCopy;

        public int DeviceId { get; }
        public FFBPType PacketType { get; }
        public int BlockIndex { get; }

        public FfbPacket(IntPtr packetPtr)
        {
            //copy ffb packet to managed structure
            packet = (InternalFfbPacket)Marshal.PtrToStructure(packetPtr, typeof(InternalFfbPacket));
            packetPtrCopy = packetPtr;

            //A packet contains only a tiny bit of information, and a pointer to the actual FFB data which is interesting as well.   
            if (packet.DataSize < 10)
                throw new Exception(string.Format("DataSize incorrect: {0} (should be at least 10 bytes)", packet.DataSize));

            //Read out the first two bytes (into the base packetData class), so we can fill out the 'important' information
            uint effectId = 0;
            VJoyUtils.Joystick.Ffb_h_EffectBlockIndex(packetPtrCopy, ref effectId);
            BlockIndex = (int) effectId;

            uint deviceId = 0;
            VJoyUtils.Joystick.Ffb_h_DeviceID(packetPtrCopy, ref deviceId);
            DeviceId = (int)deviceId;
            if (DeviceId < 1 || DeviceId > 16)
                throw new Exception(string.Format("DeviceID out of range: {0} (should be inbetween 1 and 16)", DeviceId));

            FFBPType type = FFBPType.PT_STATEREP;            
            VJoyUtils.Joystick.Ffb_h_Type(packetPtrCopy, ref type);

            PacketType = type;
        }

        public byte[] Data
        {
            get
            {
                byte[] outBuffer = new byte[packet.DataSize];
                Marshal.Copy(packet.PtrToData, outBuffer, 0, packet.DataSize);//last 8 bytes are not interesting? (haven't seen them in use anywhere anyway)
                return outBuffer;
            }
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

        public void CopyPacketData<T>(T originalPacket)
        where T : IFfbPacketData
        {
            Marshal.PtrToStructure(packet.PtrToData, originalPacket);
        }

        public override string ToString()
        {
            return string.Format("DataSize: {1}, CMD: {2}{0}{3}{0}BlockIdx: {4}{0}Device ID: {5}{0}Packet type: {6}",
                Environment.NewLine,
                packet.DataSize,
                packet.Command,
                Data.ToHexString(),
                BlockIndex,
                DeviceId,
                PacketType);
        }
    }
}
