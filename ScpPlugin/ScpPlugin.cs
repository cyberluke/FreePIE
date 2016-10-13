using System;
using System.Collections.Generic;
using FreePIE.Core.Contracts;
using SCP;
using FreePIE.Core.Plugins.Globals;

//plugin: takes care of indexing and lazy-creating
//holder: does the actual interferencing with backend code
//global: python interface that acts as a layer over the holder

namespace FreePIE.Core.Plugins
{
    [GlobalType(Type = typeof(ScpGlobal), IsIndexed = true)]
    public class ScpPlugin : IPlugin
    {
        private List<ScpGlobalHolder> holders;

        public event EventHandler Started;

        public object CreateGlobal()
        {
            holders = new List<ScpGlobalHolder>();

            return new GlobalIndexer<ScpGlobal, uint>(Create);
        }

        public void Stop()
        {
            holders.ForEach(h => h.Dispose());
        }

        private ScpGlobal Create(uint index)
        {
            var holder = new ScpGlobalHolder(index + 1);
            holders.Add(holder);

            return holder.Global;
        }

        public void DoBeforeNextExecute()
        {
            holders.ForEach(h => h.SendState());
        }

        public Action Start()
        {
            return null;
        }

        public bool GetProperty(int index, IPluginProperty property)
        {
            return false;
        }

        public bool SetProperties(Dictionary<string, object> properties)
        {
            return false;
        }

        public string FriendlyName
        {
            get { return "X360 emulator (SCP)"; }
        }
    }

    public class ScpGlobalHolder : IDisposable
    {
        public XboxDevice controller;

        public ScpGlobalHolder(uint index)
        {
            Index = index;
            Global = new ScpGlobal(this);

            if (index < 1 || index > 4)
                throw new ArgumentException(string.Format("Illegal Xbox controller number: {0}", index));

            controller = new XboxDevice(index);
            //will automatically throw the right exception when the driver isn't installed
        }

        public ScpGlobal Global { get; private set; }
        public uint Index { get; private set; }

        public void SendState()
        {
            controller.Report();
        }

        public void Dispose()
        {
            controller.Disconnect();
        }
    }

    [Global(Name = "scp")]
    public class ScpGlobal
    {
        private readonly ScpGlobalHolder holder;

        public ScpGlobal(ScpGlobalHolder holder)
        {
            this.holder = holder;
        }

        public int leftTrigger
        {
            get { return holder.controller.ControllerState.LeftTrigger; }
            set { holder.controller.ControllerState.LeftTrigger = (byte)value; }
        }

        public int rightTrigger
        {
            get { return holder.controller.ControllerState.RightTrigger; }
            set { holder.controller.ControllerState.RightTrigger = (byte)value; }
        }

        public int leftX
        {
            get { return holder.controller.ControllerState.ThumbLX; }
            set { holder.controller.ControllerState.ThumbLX = (short)value; }
        }

        public int leftY
        {
            get { return holder.controller.ControllerState.ThumbLY; }
            set { holder.controller.ControllerState.ThumbLY = (short)value; }
        }

        public int rightX
        {
            get { return holder.controller.ControllerState.ThumbRX; }
            set { holder.controller.ControllerState.ThumbRX = (short)value; }
        }

        public int rightY
        {
            get { return holder.controller.ControllerState.ThumbRY; }
            set { holder.controller.ControllerState.ThumbRY = (short)value; }
        }


        public void SetButton(ScpButtonMask button, bool pressed)
        {
            holder.controller.ControllerState[button] = pressed;
        }
    }
}
