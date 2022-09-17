using FreePIE.Core.Contracts;
using System;
using System.Runtime.InteropServices;

namespace FreePIE.Core.Plugins
{
    [GlobalEnum]
    public enum LogiPanelButton
    {
        LOGI_UNDEFINED = -1, LOGI_P1, LOGI_P2, LOGI_P3, LOGI_P4, LOGI_P5, LOGI_P6, LOGI_P7, LOGI_P8
    }

    [GlobalEnum]
    public enum LogiColor
    {
        LOGI_OFF, LOGI_GREEN, LOGI_AMBER, LOGI_RED
    }

    public class Logitech
    {

        [DllImport("G940LedInterface.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "_ButtonSetColor")]
        public extern static ulong ButtonSetColor(IntPtr device, LogiPanelButton button, LogiColor color);

        [DllImport("G940LedInterface.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "_SetAllButtonsColor")]
        public extern static ulong SetAllButtonsColor(IntPtr device, LogiColor color);

        [DllImport("G940LedInterface.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "_IsButtonColor")]
        public extern static bool IsButtonColor(IntPtr device, LogiPanelButton button, LogiColor color);

    }
}
