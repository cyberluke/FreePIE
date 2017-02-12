using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Threading;
using FreePIE.GUI.Views.Main;

namespace FreePIE.GUI.Views.Script.Output
{
    public class ConsoleViewModel : PanelViewModel
    {
        private readonly AConsoleTextWriter consoleTextWriter;

        public ConsoleViewModel()
        {
            consoleTextWriter = new ConsoleTextWriter(this);//Infinite
            Console.SetOut(consoleTextWriter);

            Title = "Console";
            IconName = "console-16.png";
        }

        public void Clear()
        {
            consoleTextWriter.Clear();
        }

        public bool CanClear { get { return !string.IsNullOrEmpty(Text); } }

        private string text;
        public string Text
        {
            get { return text; }
            set
            {
                text = value;
                NotifyOfPropertyChange(() => Text);
                NotifyOfPropertyChange(() => CanClear);
            }
        }
    }

    public abstract class AConsoleTextWriter : TextWriter
    {
        protected readonly ConsoleViewModel output;
        public AConsoleTextWriter(ConsoleViewModel output)
        {
            this.output = output;
            var worker = new BackgroundWorker();

            worker.DoWork += WorkerDoWork;
            worker.RunWorkerAsync();
        }

        public abstract void Clear();
        protected abstract void WorkerDoWork(object sender, DoWorkEventArgs e);

        public override Encoding Encoding
        {
            get { return Encoding.UTF8; }
        }
    }

    public class InfiniteConsoleTextWriter : AConsoleTextWriter
    {
        private readonly StringBuilder sb = new StringBuilder();

        public InfiniteConsoleTextWriter(ConsoleViewModel output) : base(output)
        {
        }

        public override void Clear()
        {
            output.Text = null;
            sb.Clear();
        }

        public override void WriteLine(string value)
        {
            sb.AppendLine(value);
        }

        protected override void WorkerDoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                output.Text = sb.ToString();
                Thread.Sleep(100);
            }
        }
    }

    public class ConsoleTextWriter : AConsoleTextWriter
    {
        private Queue<string> consoleStack;

        public ConsoleTextWriter(ConsoleViewModel output) : base(output)
        {
            consoleStack = new Queue<string>();
        }

        protected override void WorkerDoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                if (consoleStack.Count > 0)
                    output.Text = string.Join(Environment.NewLine, consoleStack);

                Thread.Sleep(100);
            }
        }

        public override void Clear()
        {
            output.Text = null;
            consoleStack.Clear();
        }

        public override void WriteLine(string value)
        {
            consoleStack.Enqueue(value);
            if (consoleStack.Count > 1000)
                consoleStack.Dequeue();
        }
    }
}
