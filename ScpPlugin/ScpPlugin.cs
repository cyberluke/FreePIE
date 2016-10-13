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
        private XboxDevice controller;
        public XboxState controllerState;

        public ScpGlobalHolder(uint index)
        {
            Index = index;
            Global = new ScpGlobal(this);

            if (index < 1 || index > 4)
                throw new ArgumentException(string.Format("Illegal Xbox controller number: {0}", index));

            controller = new XboxDevice(index);
            controllerState = controller.GetState();
            //will automatically throw the right exception when the driver isn't installed
        }

        public ScpGlobal Global { get; private set; }
        public uint Index { get; private set; }

        public void SendState()
        {
            controller.Report(ref controllerState);
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

        public byte leftTrigger
        {
            get { return holder.controllerState.LeftTrigger; }
            set { holder.controllerState.LeftTrigger = value; }
        }

        public byte rightTrigger
        {
            get { return holder.controllerState.RightTrigger; }
            set { holder.controllerState.RightTrigger = value; }
        }

        public short leftX
        {
            get { return holder.controllerState.ThumbLX; }
            set { holder.controllerState.ThumbLX = value; }
        }

        public short leftY
        {
            get { return holder.controllerState.ThumbLY; }
            set { holder.controllerState.ThumbLY = value; }
        }

        public short rightX
        {
            get { return holder.controllerState.ThumbRX; }
            set { holder.controllerState.ThumbRX = value; }
        }

        public short rightY
        {
            get { return holder.controllerState.ThumbRY; }
            set { holder.controllerState.ThumbRY = value; }
        }


        public void SetButton(ScpButton button, bool pressed)
        {
            holder.controllerState[button] = pressed;
        }
    }
}
