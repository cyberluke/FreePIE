using FreePIE.Core.Contracts;
using System;
using System.Runtime.InteropServices;

namespace SCP
{
    [GlobalEnum]
    [Flags]
    public enum ScpButton : ushort
    {
        None,
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
        Undef = 0x800,

        A = 0x1000,
        B = 0x2000,
        X = 0x4000,
        Y = 0x8000
    }

    [StructLayout(LayoutKind.Sequential, Size = 28)]
    public unsafe struct XboxState
    {
        uint magic;
        uint controllerIndex;
        ushort wot;
        ushort buttons;

        public byte LeftTrigger, RightTrigger;
        public short ThumbLX,
            ThumbLY,
            ThumbRX,
            ThumbRY;

        public bool this[ScpButton button]
        {
            set
            {
                if (value)
                    buttons |= (ushort)button;
                else
                    buttons &= (ushort)~((ushort)button);
            }
            get { return (buttons & (ushort)button) != 0; }
        }

        public XboxState(uint cidx)
        {
            magic = 0x1C; //0-3
            controllerIndex = cidx;
            wot = 0x1400;//9
            buttons = 0;
            LeftTrigger = RightTrigger = 0;
            ThumbLX = ThumbLY = ThumbRX = ThumbRY = 0;
        }
    }
}
