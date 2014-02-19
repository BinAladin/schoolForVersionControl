using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace producer_consumer
{
    public class Producer
    {
        public static int LastElement = -1;
        private int HowMany;
        private BoundedBuffer Buffer;
        private Thread producingThread;
        readonly int ID;
        public Producer(BoundedBuffer buffer, int howMany, int _ID)
        {
            if (buffer == null || howMany<1) throw new ArgumentOutOfRangeException("No suitable arguments");
            HowMany = howMany;
            Buffer = buffer;
            ID = _ID;
        }

        public void run()
        {
            (producingThread = new Thread(() => _run())).Start();
        }
        public void stop()
        {
            producingThread.Abort();
        }
        private void _run()
        {
            Random rnd = new Random();
            for (int i = 0; i < HowMany; i++)
            {
                Buffer.Put(rnd.Next(0,10) + ID*1000);
                Thread.Sleep(5);
            }

            this.stop();
        }
    }
    public class Consumer
    {
        private BoundedBuffer Buffer;
        private Thread consumingThread;

        public Consumer(BoundedBuffer buffer)
        {
            if (buffer == null) throw new ArgumentNullException("buffer is null");
            Buffer = buffer;
        }

        public void run()
        {
            (consumingThread = new Thread(() => _run())).Start();
        }

        public void stop()
        {
            consumingThread.Abort();
        }

        private void _run()
        {
            int consumed;
            while ((consumed = Buffer.Take()) != Producer.LastElement)
            {
            }
            this.stop();
        }
    }
    public class BoundedBuffer
    {
        private int Capacity;
        private Queue<int> Elements;
        private Object PutLock;
        private Object TakeLock;
        public BoundedBuffer(int _capacity)
        {
            if (_capacity < 1) throw new ArgumentOutOfRangeException("too small buffer");
            Elements = new Queue<int>(Capacity = _capacity);
        }
        public bool IsFull()
        {
            return Elements.Count == Capacity;
        }

        public void Put(int newElement)
        {
            try
            {
                Monitor.Enter(this.Elements);
                while (IsFull())
                {
                    Monitor.Wait(this.Elements);
                }
                Elements.Enqueue(newElement);
                Console.WriteLine("Put in the stock:"+newElement);
                Monitor.PulseAll(this.Elements);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in thrown while putting !");
            }
            finally
            {
                if (Monitor.IsEntered(this.Elements))
                    Monitor.Exit(this.Elements);
            }
        }

        public int Take()
        {
            int taken = int.MinValue;
            try
            {
                
                Monitor.Enter(this.Elements);
                while (Elements.Count == 0)
                {
                    //Monitor.Exit(this.Elements);
                    Monitor.Wait(this.Elements);
                    //Monitor.Enter(this.Elements);
                }
                taken = Elements.Dequeue();
                Monitor.PulseAll(this.Elements);
                Console.WriteLine("   Taken from stock:"+taken);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine("Exception thrown while taking !");
            }
            finally
            {
                if (Monitor.IsEntered(this.Elements))
                    Monitor.Exit(this.Elements);
            }
            return taken;
        }

        public static void Main(string[] args)
        {
            int length = 10;
            BoundedBuffer testedBuffer = new BoundedBuffer(length);
            Producer p1 = new Producer(testedBuffer,15,1);
            Producer p2 = new Producer(testedBuffer, 15, 2);
            Consumer c1 = new Consumer(testedBuffer);
            p2.run();
            p1.run();
            c1.run();
            Thread.Sleep(1000);
            p1.stop();
            p2.stop();
            c1.stop();
            Console.ReadKey();
        }
    }
}
