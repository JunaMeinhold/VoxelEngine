namespace VoxelEngine.Rendering.D3D
{
    using System;
    using System.Collections.Concurrent;
    using Vortice.Direct3D11;

    public class RenderThread : IDisposable
    {
        public readonly ConcurrentQueue<Action<ID3D11DeviceContext>> workQueue;
        private readonly Thread thread;
        private ID3D11DeviceContext deferredContext;
        private bool isRendering = true;
        private readonly AutoResetEvent workHandle = new(false);
        private readonly AutoResetEvent waitHandle = new(false);

        public RenderThread(ID3D11Device device, ConcurrentQueue<Action<ID3D11DeviceContext>> workQueue)
        {
            deferredContext = device.CreateDeferredContext();

            this.workQueue = workQueue;
            thread = new Thread(RenderThreadVoid);
            thread.Start();
            Wait();
        }

        private void RenderThreadVoid()
        {
            while (isRendering)
            {
                while (workQueue.TryDequeue(out Action<ID3D11DeviceContext> work))
                {
                    work(deferredContext);
                }

                waitHandle.Set();
                workHandle.WaitOne();
            }
        }

        public void Wait()
        {
            waitHandle.WaitOne();
        }

        public void Execute()
        {
            workHandle.Set();
        }

        public ID3D11CommandList GetCommandList()
        {
            return deferredContext.FinishCommandList(true);
        }

        public void Dispose()
        {
            isRendering = false;
            workHandle.Set();
            while (thread.IsAlive)
            {
                Thread.Sleep(1);
            }

            deferredContext.Dispose();
            deferredContext = null;

            GC.SuppressFinalize(this);
        }
    }

    public class RenderThreadPool : IDisposable
    {
        private readonly RenderThread[] renderThreads;
        private readonly ConcurrentQueue<Action<ID3D11DeviceContext>> workQueue;
        private readonly ID3D11CommandList[] commandLists;

        public RenderThreadPool(ID3D11Device device, int count)
        {
            workQueue = new();
            renderThreads = new RenderThread[count];
            commandLists = new ID3D11CommandList[count];
            for (int i = 0; i < count; i++)
            {
                renderThreads[i] = new(device, workQueue);
            }
        }

        public void Dispose()
        {
            for (int i = 0; i < renderThreads.Length; i++)
            {
                renderThreads[i].Dispose();
            }

            GC.SuppressFinalize(this);
        }

        public void Enqueue(Action<ID3D11DeviceContext> action)
        {
            workQueue.Enqueue(action);
        }

        public void Dispatch()
        {
            for (int i = 0; i < renderThreads.Length; i++)
            {
                renderThreads[i].Execute();
            }

            for (int i = 0; i < renderThreads.Length; i++)
            {
                renderThreads[i].Wait();
            }

            for (int i = 0; i < renderThreads.Length; i++)
            {
                commandLists[i]?.Release();
                commandLists[i] = renderThreads[i].GetCommandList();
            }
        }

        public Task DispatchAsync()
        {
            return Task.Run(() =>
            {
                for (int i = 0; i < renderThreads.Length; i++)
                {
                    renderThreads[i].Execute();
                }

                for (int i = 0; i < renderThreads.Length; i++)
                {
                    renderThreads[i].Wait();
                }

                for (int i = 0; i < renderThreads.Length; i++)
                {
                    commandLists[i]?.Release();
                    commandLists[i] = renderThreads[i].GetCommandList();
                }
            });
        }

        public void Execute(ID3D11DeviceContext context)
        {
            for (int i = 0; i < commandLists.Length; i++)
            {
                if (commandLists[i] is not null)
                {
                    context.ExecuteCommandList(commandLists[i], true);
                }
            }
        }

        public void Wait(ID3D11DeviceContext context)
        {
            for (int i = 0; i < renderThreads.Length; i++)
            {
                renderThreads[i].Execute();
            }

            for (int i = 0; i < renderThreads.Length; i++)
            {
                renderThreads[i].Wait();
            }

            for (int i = 0; i < renderThreads.Length; i++)
            {
                ID3D11CommandList commandList = renderThreads[i].GetCommandList();
                context.ExecuteCommandList(commandList, true);
                commandList.Release();
            }
        }
    }
}