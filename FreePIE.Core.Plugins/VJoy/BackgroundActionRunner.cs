using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using FreePIE.Core.Plugins.Dx;
using System.Runtime.InteropServices;

namespace FreePIE.Core.Plugins.VJoy
{
    public interface IPacketAction
    {
        void Call(InternalFfbPacket inMemoryPacket);
    }
    /// <summary>
    /// HID descriptor type: feature or report
    /// </summary>
    public enum CommandType : int
    {
        IOCTL_HID_SET_FEATURE = 0xB0191,
        IOCTL_HID_WRITE_REPORT = 0xB000F
    }

    /// <summary>
    /// Aligned struct for marshaling of raw packets back to C#
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct InternalFfbPacket
    {
        public int DataSize;
        public CommandType Command;
        public IntPtr PtrToData;
    }

    public class BackgroundActionRunner<T>
        where T : IPacketAction
    {
        private readonly ConcurrentQueue<T> queue;
        private readonly BlockingCollection<T> queueWrapper;
        private readonly Thread asyncThread;
        private bool stopped = false;
        private InternalFfbPacket inMemoryPacket;

        ~BackgroundActionRunner()
        {
            inMemoryPacket.PtrToData = IntPtr.Zero;
        }

        public BackgroundActionRunner()
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
            //queue.Enqueue(item);
        }

        public void Stop()
        {
            Console.WriteLine("STOPPING BACKGROUND FFB THREAD! FFB IS NOW OFF. RESTART FREEPIE.");
            stopped = true;

            // Because blocking queue might wait for new item blocking the thread
            if (asyncThread.IsAlive)
                asyncThread.Abort();
        }

        private void asyncThreadEntryPoint()
        {
            while (!stopped)
            {
                T t = queueWrapper.Take();
                //T t;
                //if (queue.TryDequeue(out t))
                //{
                    t.Call(inMemoryPacket);
                //}
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