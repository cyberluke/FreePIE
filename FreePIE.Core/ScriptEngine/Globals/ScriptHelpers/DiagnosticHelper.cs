using System;
using FreePIE.Core.Common.Events;
using FreePIE.Core.Contracts;
using FreePIE.Core.Model.Events;
using System.IO;

namespace FreePIE.Core.ScriptEngine.Globals.ScriptHelpers
{
    [Global(Name = "diagnostics")]
    public class DiagnosticHelper : IScriptHelper
    {
        private readonly IEventAggregator eventAggregator;
        private TextWriter defaultOutLogger = System.IO.TextWriter.Null;
        private TextWriter defaultErrLogger = System.IO.TextWriter.Null;

        public DiagnosticHelper(IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;
        }

        public void debug(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }

        public void debug(object arg)
        {
            Console.WriteLine(arg);
        }

        public void disable()
        {
            if (Console.Out != System.IO.TextWriter.Null)
                defaultOutLogger = Console.Out;
            if (Console.Error != System.IO.TextWriter.Null)
                defaultErrLogger = Console.Error;

            Console.SetOut(System.IO.TextWriter.Null);
            Console.SetError(System.IO.TextWriter.Null);
        }

        public void enable()
        {
            Console.SetOut(defaultOutLogger);
            Console.SetError(defaultErrLogger);
        }

        [NeedIndexer]
        public void watch(object value, string indexer)
        {
            eventAggregator.Publish(new WatchEvent(indexer, value));
        }
    }
}
