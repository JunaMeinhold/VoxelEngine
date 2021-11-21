using BepuUtilities;
using BepuUtilities.Memory;
using System;
using System.Diagnostics;
using System.Threading;

namespace HexaEngine.Scenes
{
    public class ThreadDispatcher : IThreadDispatcher, IDisposable
    {
        private int threadCount;
        public int ThreadCount => threadCount;

        private struct Worker
        {
            public Thread Thread;
            public AutoResetEvent Signal;
        }

        private Worker[] workers;
        private AutoResetEvent finished;

        private BufferPool[] bufferPools;

        public ThreadDispatcher(int threadCount)
        {
            this.threadCount = threadCount;
            workers = new Worker[threadCount - 1];
            for (int i = 0; i < workers.Length; ++i)
            {
                workers[i] = new Worker { Thread = new Thread(WorkerLoop), Signal = new AutoResetEvent(false) };
                workers[i].Thread.IsBackground = true;
                workers[i].Thread.Start(workers[i].Signal);
            }
            finished = new AutoResetEvent(false);
            bufferPools = new BufferPool[threadCount];
            for (int i = 0; i < bufferPools.Length; ++i)
            {
                bufferPools[i] = new BufferPool();
            }
        }

        private void DispatchThread(int workerIndex)
        {
            Debug.Assert(workerBody != null);
            workerBody(workerIndex);

            if (Interlocked.Increment(ref completedWorkerCounter) == threadCount)
            {
                finished.Set();
            }
        }

        private volatile Action<int> workerBody;
        private int workerIndex;
        private int completedWorkerCounter;

        private void WorkerLoop(object untypedSignal)
        {
            var signal = (AutoResetEvent)untypedSignal;
            while (true)
            {
                signal.WaitOne();
                if (disposed)
                    return;
                DispatchThread(Interlocked.Increment(ref workerIndex) - 1);
            }
        }

        private void SignalThreads()
        {
            for (int i = 0; i < workers.Length; ++i)
            {
                workers[i].Signal.Set();
            }
        }

        public void DispatchWorkers(Action<int> workerBody)
        {
            Debug.Assert(this.workerBody == null);
            workerIndex = 1; //Just make the inline thread worker 0. While the other threads might start executing first, the user should never rely on the dispatch order.
            completedWorkerCounter = 0;
            this.workerBody = workerBody;
            SignalThreads();
            //Calling thread does work. No reason to spin up another worker and block this one!
            DispatchThread(0);
            finished.WaitOne();
            this.workerBody = null;
        }

        private volatile bool disposed;

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                SignalThreads();
                for (int i = 0; i < bufferPools.Length; ++i)
                {
                    bufferPools[i].Clear();
                }
                foreach (var worker in workers)
                {
                    worker.Thread.Join();
                    worker.Signal.Dispose();
                }
            }
        }

        public BufferPool GetThreadMemoryPool(int workerIndex)
        {
            return bufferPools[workerIndex];
        }
    }
}