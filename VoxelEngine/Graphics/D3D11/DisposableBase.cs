﻿namespace VoxelEngine.Graphics.D3D11
{
    public abstract class DisposableBase : IDisposable
    {
        private bool disposedValue;

        public DisposableBase()
        {
            //LeakTracer.Allocate(this);
        }

        public bool IsDisposed => disposedValue;

        public event EventHandler? OnDisposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                DisposeCore();
                OnDisposed?.Invoke(this, EventArgs.Empty);
                disposedValue = true;
            }
        }

        protected abstract void DisposeCore();

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}