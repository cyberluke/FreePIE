using System;
using System.Collections.Generic;
using FreePIE.Core.Plugins.Dx;
using FreePIE.Core.Plugins.Strategies;
using vJoyInterfaceWrap;

namespace FreePIE.Core.Plugins.VJoy
{
    public class VJoyGlobalHolder : IDisposable
    {
        private static readonly vJoy Joystick = VJoyUtils.Joystick;
        private readonly Dictionary<HID_USAGES, bool> enabledAxis;
        private readonly Dictionary<HID_USAGES, int> currentAxisValue;

        private readonly int maxButtons;
        private readonly int maxDirPov;
        private readonly int maxContinuousPov;
        public const int ContinuousPovMax = 35900;

        private readonly SetPressedStrategy setPressedStrategy;

        public bool FfbEnabled { get { return Joystick.IsDeviceFfb(Index); } }

        public void RegisterFfbDevice(Device dev)
        {
            if (!FfbEnabled) throw new NotSupportedException("This VJoy device does not have FFB enabled");

            VJoyFfbWrap.RegisterDevice((int)Index, dev);
        }

        public VJoyGlobalHolder(uint index)
        {
            Index = index;
            Global = new VJoyGlobal(this);
            setPressedStrategy = new SetPressedStrategy(b => SetButton(b, true), b => SetButton(b, false));

            if (index < 1 || index > 16)
                throw new ArgumentException(string.Format("Illegal joystick device id: {0}", index));

            if (!Joystick.vJoyEnabled())
                throw new Exception("vJoy driver not enabled: Failed Getting vJoy attributes");

            uint apiVersion = 0;
            uint driverVersion = 0;
            bool match = Joystick.DriverMatch(ref apiVersion, ref driverVersion);
            if (!match)
                Console.WriteLine("vJoy version of Driver ({0:X}) does NOT match DLL Version ({1:X})", driverVersion, apiVersion);

            Version = new VjoyVersionGlobal(apiVersion, driverVersion);

            var status = Joystick.GetVJDStatus(index);


            string error = null;
            switch (status)
            {
                case VjdStat.VJD_STAT_BUSY:
                    error = "vJoy Device {0} is already owned by another feeder";
                    break;
                case VjdStat.VJD_STAT_MISS:
                    error = "vJoy Device {0} is not installed or disabled";
                    break;
                case VjdStat.VJD_STAT_UNKN:
                    error = ("vJoy Device {0} general error");
                    break;
            }

            if (error == null && !Joystick.AcquireVJD(index))
                error = "Failed to acquire vJoy device number {0}";

            if (error != null)
                throw new Exception(string.Format(error, index));

            long max = 0;
            Joystick.GetVJDAxisMax(index, HID_USAGES.HID_USAGE_X, ref max);
            AxisMax = (int)max / 2 - 1;

            enabledAxis = new Dictionary<HID_USAGES, bool>();
            foreach (HID_USAGES axis in Enum.GetValues(typeof(HID_USAGES)))
                enabledAxis[axis] = Joystick.GetVJDAxisExist(index, axis);

            maxButtons = Joystick.GetVJDButtonNumber(index);
            maxDirPov = Joystick.GetVJDDiscPovNumber(index);
            maxContinuousPov = Joystick.GetVJDContPovNumber(index);

            currentAxisValue = new Dictionary<HID_USAGES, int>();

            Joystick.ResetVJD(index);
        }

        public void SetButton(int button, bool pressed)
        {
            if (button >= maxButtons)
                throw new Exception(string.Format("Maximum buttons are {0}. You need to increase number of buttons in vJoy config", maxButtons));

            Joystick.SetBtn(pressed, Index, (byte)(button + 1));
        }

        public void SetPressed(int button)
        {
            setPressedStrategy.Add(button);
        }

        public void SetPressed(int button, bool state)
        {
            setPressedStrategy.Add(button, state);
        }

        public void SendPressed()
        {
            setPressedStrategy.Do();
        }

        public void SetAxis(int x, HID_USAGES usage)
        {
            if (!enabledAxis[usage])
                throw new Exception(string.Format("Axis {0} not enabled, enable it from VJoy config", usage));

            Joystick.SetAxis(x + AxisMax, Index, usage);
            currentAxisValue[usage] = x;
        }

        public int GetAxis(HID_USAGES usage)
        {
            return currentAxisValue.ContainsKey(usage) ? currentAxisValue[usage] : 0;
        }

        public void SetDirectionalPov(int pov, VJoyPov direction)
        {
            if (pov >= maxDirPov)
                throw new Exception(string.Format("Maximum digital POV hats are {0}. You need to increase number of digital POV hats in vJoy config", maxDirPov));

            Joystick.SetDiscPov((int)direction, Index, (uint)pov + 1);
        }

        public void SetContinuousPov(int pov, int value)
        {
            if (pov >= maxContinuousPov)
                throw new Exception(string.Format("Maximum analog POV sticks are {0}. You need to increase number of analog POV hats in vJoy config", maxContinuousPov));

            Joystick.SetContPov(value, Index, (uint)pov + 1);
        }

        public VJoyGlobal Global { get; private set; }
        public uint Index { get; private set; }
        public int AxisMax { get; private set; }

        public VjoyVersionGlobal Version { get; private set; }

        public void Dispose()
        {
            VJoyFfbWrap.Dispose();
            Joystick.RelinquishVJD(Index);
        }
    }
}
