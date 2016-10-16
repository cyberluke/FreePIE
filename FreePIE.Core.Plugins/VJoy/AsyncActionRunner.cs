using System;
using System.Collections.Concurrent;
using System.Threading;

namespace FreePIE.Core.Plugins.VJoy
{
    public class AsyncActionRunner<T>
    {
        private readonly ConcurrentQueue<T> queue;
        private readonly BlockingCollection<T> queueWrapper;
        private readonly Thread asyncThread;

        public AsyncActionRunner()
        {
            queue = new ConcurrentQueue<T>();
            queueWrapper = new BlockingCollection<T>(queue);
            asyncThread = new Thread(asyncThreadEntryPoint);
            asyncThread.Start();
        }

        public void Enqueue(T item)
        {
            queueWrapper.Add(item);
        }

        private void asyncThreadEntryPoint()
        {
            while (true)
                //"A call to Take may block until an item is available to be removed."
                OnAsyncItem(queueWrapper.Take());
        }

        protected virtual void OnAsyncItem(T item)
        {

        }
    }
}
