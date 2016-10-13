using FreePIE.Core.Contracts;
using System;
using System.Runtime.InteropServices;

namespace SCP
{
    [Flags]
    [GlobalEnum]
    public enum ScpButtonMask : ushort
    {
        None = 0,
        Up = 1,
        Down = 2,
        Left = 4,
        Right = 8,
        Start = 0x10,
        Back = 0x20,
        LeftThumb = 0x40,
        RightThumb = 0x80,

        LeftShoulder = 0x100,
        RightShoulder = 0x200,

        Guide = 0x400,

        //Undef = 0x800,

        A = 0x1000,
        B = 0x2000,
        X = 0x4000,
        Y = 0x8000
    }

    [GlobalEnum]
    public enum AxisMap : int
    {
        ThumbLX,
        ThumbLY,
        ThumbRX,
        ThumbRY,
        LeftTrigger,
        RightTrigger
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x10)]
    internal unsafe struct ControlBuffer
    {
        public uint StructureSize;
        public uint ControllerIndex;
        uint Reserved1,
            Reserved2;
        internal ControlBuffer(uint controllerIndex)
        {
            StructureSize = (uint)sizeof(ControlBuffer);
            ControllerIndex = controllerIndex;
            Reserved1 = Reserved2 = 0;
        }
    }

    [StructLayout(LayoutKind.Sequential, Size = 0x1C)]
    public unsafe struct ScpStatusReport
    {
        public uint StructureSize;
        public uint ControllerIndex;
        byte someFlagThatdoesSeemToMatter,
            someFlagThatdoesntSeemToMatter;
        ushort buttons;

        public byte LeftTrigger, RightTrigger;

        public short ThumbLX,
            ThumbLY,
            ThumbRX,
            ThumbRY;

        public bool this[ScpButtonMask button]
        {
            set
            {
                if (value)
                    buttons |= (ushort)button;
                else
                    buttons &= (ushort)~((ushort)button);
            }
        }
        public ScpStatusReport(uint controllerIndex)
        {
            StructureSize = (uint)sizeof(ScpStatusReport);
            ControllerIndex = controllerIndex;
            someFlagThatdoesSeemToMatter = 0;
            someFlagThatdoesntSeemToMatter = 0x14;
            buttons = 0;
            LeftTrigger = RightTrigger = 0;
            ThumbLX = ThumbLY = ThumbRX = ThumbRY = 0;
        }
    }
}
