using FreePIE.Core.Contracts;

//https://github.com/shauleiz/vJoy/blob/master/apps/common/vJoyInterfaceCS/vJoyInterfaceWrap/Wrapper.cs

namespace FreePIE.Core.Plugins.VJoy
{
    [GlobalEnum]
    public enum EffectOperation : byte
    {
        Start = 1,
        SoloStart = 2,
        Stop = 3
    }

    [GlobalEnum]
    public enum ERROR : uint
    {
        ERROR_SUCCESS = 0,
    }
}
