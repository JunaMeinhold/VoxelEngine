namespace VoxelEngine.Graphics
{
    using System;

    public abstract class DisposableBase : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public event EventHandler? Disposed;

        public void Dispose()
        {
            if (!IsDisposed)
            {
                DisposeCore();
                Disposed?.Invoke(this, EventArgs.Empty);
                IsDisposed = true;
                GC.SuppressFinalize(this);
            }
        }

        protected abstract void DisposeCore();
    }
}