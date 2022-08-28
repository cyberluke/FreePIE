using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using FreePIE.Core.Plugins.Dx;

namespace FreePIE.Core.Plugins.VJoy
{
    public interface IAsyncAction
    {
        void Call();
    }
    public class AsyncActionRunner<T>
        where T : IAsyncAction
    {
        private readonly ConcurrentQueue<T> queue;
        private readonly BlockingCollection<T> queueWrapper;
        private readonly Thread asyncThread;
        private bool stopped = false;

        public AsyncActionRunner()
        {
            queue = new ConcurrentQueue<T>();
            //queueWrapper = new BlockingCollection<T>(queue);
            asyncThread = new Thread(asyncThreadEntryPoint);
            asyncThread.Start();
        }

        public void Enqueue(T item)
        {
            Console.WriteLine("Adding queue async item {0}", item);
            //queueWrapper.Add(item);
            queue.Enqueue(item);
        }

        public void Stop()
        {
            stopped = true;
        }

        private void asyncThreadEntryPoint()
        {
            while (!stopped)
            {
                //T t = queueWrapper.Take();
                T t;
                if (queue.TryDequeue(out t))
                {
                    t.Call();
                }
                /*if (t != null)
                {
                    //"A call to Take may block until an item is available to be removed."
                    Task.Run<String>(async () =>
                    {
                        t.Call();
                        await Task.Yield();
                        return null;
                    });
                }*/
            }
        }
    }
}