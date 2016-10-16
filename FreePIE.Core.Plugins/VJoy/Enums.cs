using FreePIE.Core.Contracts;

//https://github.com/shauleiz/vJoy/blob/master/apps/common/vJoyInterfaceCS/vJoyInterfaceWrap/Wrapper.cs

namespace FreePIE.Core.Plugins.VJoy
{
    [GlobalEnum]
    public enum PacketType : byte
    {
        None = 0,

        // Write
        Effect = 0x1,
        Envelope = 0x2,
        Condition = 0x3,
        Periodic = 0x4,
        ConstantForce = 0x5,
        RampForce = 0x6,
        CustomForceData = 0x7,
        DownloadForceSample = 0x8,
        None2 = 0x9,
        EffectOperation = 0xA,
        PidBlockFree = 0xB,
        PidDeviceControl = 0xC,
        DeviceGain = 0xD,
        SetCustomForce = 0xE,

        None3 = 0xF,
        None4 = 0x10,

        // Feature
        CreateNewEffect = 0x1 + 0x10,
        BlockLoad = 0x2 + 0x10,
        PIDPool = 0x3 + 0x10
    }

    [GlobalEnum]
    public enum EffectOperation : byte
    {
        Start = 1,
        SoloStart = 2,
        Stop = 3
    }

    [GlobalEnum]
    public enum EffectType : byte
    {
        None = 0,
        ConstantForce = 1,
        Ramp = 2,
        Square = 3,
        Sine = 4,
        Triangle = 5,
        SawtoothUp = 6,
        SawtoothDown = 7,
        Spring = 8,
        Damper = 9,
        Inertia = 10,
        Friction = 11,
        CustomForce = 12
    }

    [GlobalEnum]
    public enum PidDeviceControl : byte
    {
        EnableActuators = 1,
        DisableActuators = 2,
        StopAll = 3,
        DeviceReset = 4,
        DevicePause = 5,
        DeviceContinue = 6
    }
}
