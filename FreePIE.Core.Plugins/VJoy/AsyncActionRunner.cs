using System;
using System.Collections.Concurrent;
using System.Threading;

namespace FreePIE.Core.Plugins.VJoy
{
    public class AsyncActionRunner
    {
        private ConcurrentQueue<Action> queue;
        private BlockingCollection<Action> queueWrapper;
        private Thread asyncThread;
        public AsyncActionRunner()
        {
            queue = new ConcurrentQueue<Action>();
            queueWrapper = new BlockingCollection<Action>(queue);
            asyncThread = new Thread(asyncThreadEntryPoint);
            asyncThread.Start();
        }

        public void Enqueue(Action item)
        {
            queueWrapper.Add(item);
        }

        private void asyncThreadEntryPoint()
        {
            while (true)
                //"A call to Take may block until an item is available to be removed."
                queueWrapper.Take()();
        }
    }
}
