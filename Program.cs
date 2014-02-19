using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace producer_consumer
{
    public class Producer
    {
        public static int LastElement = int.MinValue;
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

        public void Run()
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
            Buffer.Put(LastElement);
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

        public void Run()
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
            Console.WriteLine("Consumer ended !");
            this.stop();
        }
    }

    public class MiddleMan
    {
        private BoundedBuffer InBuffer;
        private BoundedBuffer OutBuffer;
        private Thread middleThread;
        
        public MiddleMan(BoundedBuffer _inBuffer, BoundedBuffer _outBuffer)
        {
            InBuffer = _inBuffer;
            OutBuffer = _outBuffer;
            middleThread = new Thread(() => _run());
            
        }

        public void Run()
        {
            middleThread.Start();
        }

        private void _run()
        {
            while (true)
            OutBuffer.Put(InBuffer.Take());
        }

        public void Stop()
        {
            middleThread.Abort();
        }
    }

    public class Duplicator
    {
        private BoundedBuffer InBuffer;
        private BoundedBuffer OutBuffer1;
        private BoundedBuffer OutBuffer2;
        private Thread duplicatingThread;
        public Duplicator(BoundedBuffer _InBuffer, BoundedBuffer _OutBuffer1, BoundedBuffer _OutBuffer2)
        {
            InBuffer = _InBuffer;
            OutBuffer1 = _OutBuffer1;
            OutBuffer2 = _OutBuffer2;
            duplicatingThread = new Thread(() => _run());
        }

        public void Run()
        {
            duplicatingThread.Start();
        }

        public void Stop()
        {
            duplicatingThread.Abort();
        }
        private void _run()
        {
            int Item;
            while (true)
            {
                Item = InBuffer.Take();
                OutBuffer1.Put(Item);
                OutBuffer2.Put(Item);
            }
        }

    }
    public class BoundedBuffer
    {
        private int Capacity;
        private Queue<int> Elements;
        private Object PutLock;
        private Object TakeLock;
        public static int countOfBuffers = 0;
        private int ID;
        public BoundedBuffer(int _capacity)
        {
            if (_capacity < 1) throw new ArgumentOutOfRangeException("too small buffer");
            Elements = new Queue<int>(Capacity = _capacity);
            ID = countOfBuffers++;
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
                Console.WriteLine("Put in the stock "+ID+":"+newElement);
                Monitor.PulseAll(this.Elements);
            }
            catch (ApplicationException ex)
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
            int taken = int.MaxValue;
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
                Console.WriteLine("   Taken from stock " +ID+":"+taken);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception thrown while taking !" + taken + ex.ToString());
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
            BoundedBuffer b1 = new BoundedBuffer(length);
            BoundedBuffer b2 = new BoundedBuffer(length);
            BoundedBuffer b3 = new BoundedBuffer(length);
            Duplicator d1 = new Duplicator(b1,b2,b3);
            Consumer c1 = new Consumer(b2);
            Consumer c2 = new Consumer(b3);
            Producer p1 = new Producer(b1,100,1);
            p1.Run();
            d1.Run();
            c2.Run();
            c1.Run();
            Thread.Sleep(1000);
            p1.stop();
            Thread.Sleep(300);
            d1.Stop();
            c1.stop();
            c2.stop();
            Console.ReadKey();
        }
    }
}
