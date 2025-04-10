using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;


namespace Server
{
    class Program
    {
        const int ThreadCount = 8;  // 실행할 스레드 개수
        const int ItemCount = 1000000; // 각 스레드가 처리할 아이템 개수

        static void Main()
        {
            PacketHandlerManager.Instance.Initialize();



            //var server = new Server.Server();
            //server.Start();


            //Console.WriteLine($"[Thread Count] {ThreadCount}");
            //Console.WriteLine($"[Total Items]  {ThreadCount * ItemCount}\n");

            //// Queue + lock 테스트
            //TestQueueWithLock();

            //// ConcurrentQueue 테스트
            //TestConcurrentQueue();
        }

        static void TestQueueWithLock()
        {
            Queue<int> queue = new Queue<int>();
            object lockObj = new object();
            Stopwatch stopwatch = Stopwatch.StartNew();

            Task[] tasks = new Task[ThreadCount];

            for (int i = 0; i < ThreadCount; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    for (int j = 0; j < ItemCount / 2; j++)
                    {
                        lock (lockObj)
                        {
                            queue.Enqueue(j);
                        }
                    }
                    for (int j = 0; j < ItemCount / 2; j++)
                    {
                        lock (lockObj)
                        {
                            if (queue.Count > 0)
                                queue.Dequeue();
                        }
                    }
                });
            }

            Task.WaitAll(tasks);
            stopwatch.Stop();

            Console.WriteLine("▶ Queue + lock 테스트");
            Console.WriteLine($"Elapsed Time: {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"Final Count:  {queue.Count}\n");
        }

        static void TestConcurrentQueue()
        {
            ConcurrentQueue<int> queue = new ConcurrentQueue<int>();
            Stopwatch stopwatch = Stopwatch.StartNew();

            Task[] tasks = new Task[ThreadCount];

            for (int i = 0; i < ThreadCount; i++)
            {
                tasks[i] = Task.Run(() =>
                {
                    for (int j = 0; j < ItemCount / 2; j++)
                    {
                        queue.Enqueue(j);
                    }
                    for (int j = 0; j < ItemCount / 2; j++)
                    {
                        queue.TryDequeue(out _);
                    }
                });
            }

            Task.WaitAll(tasks);
            stopwatch.Stop();

            Console.WriteLine("▶ ConcurrentQueue 테스트");
            Console.WriteLine($"Elapsed Time: {stopwatch.ElapsedMilliseconds} ms");
            Console.WriteLine($"Final Count:  {queue.Count}\n");
        }
    }
}