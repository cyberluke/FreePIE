using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

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

        public AsyncActionRunner()
        {
            queue = new ConcurrentQueue<T>();
            queueWrapper = new BlockingCollection<T>(queue);
            asyncThread = new Thread(asyncThreadEntryPoint);
            asyncThread.Start();
        }

        public void Enqueue(T item)
        {
            Console.WriteLine("Adding queue async item {0}", item);
            queueWrapper.Add(item);
        }

        private void asyncThreadEntryPoint()
        {
            while (true)
            {
                T t = queueWrapper.Take();
                t.Call();
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