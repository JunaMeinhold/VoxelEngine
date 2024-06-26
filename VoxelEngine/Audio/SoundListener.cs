namespace VoxelEngine.Audio
{
    using System.Numerics;
    using Hexa.NET.X3DAudio;

    public unsafe class SoundListener
    {
        private bool disposedValue;
        private X3DAudioListener listener;

        public SoundListener()
        {
            listener = new();
        }

        public X3DAudioListener Listener { get => listener; set => listener = value; }

        public static SoundListener Active { get; private set; }

        public Vector3 OrientFront { get => listener.OrientFront; set => listener.OrientFront = value; }

        public Vector3 OrientTop { get => listener.OrientTop; set => listener.OrientTop = value; }

        public Vector3 Position { get => listener.Position; set => listener.Position = value; }

        public Vector3 Velocity { get => listener.Velocity; set => listener.Velocity = value; }

        public X3DAudioCone? Cone
        {
            get
            {
                if (listener.PCone == null)
                {
                    return null;
                }
                return *listener.PCone;
            }
            set
            {
                if (value == null)
                {
                    if (listener.PCone != null)
                    {
                        Free(listener.PCone);
                        listener.PCone = null;
                    }
                }
                else
                {
                    if (listener.PCone != null)
                    {
                        listener.PCone = AllocTAndZero<X3DAudioCone>();
                    }
                    *listener.PCone = value.Value;
                }
            }
        }

        public void Activate()
        {
            Active = this;
        }

        public void Deactivate()
        {
            if (Active == this)
            {
                Active = null;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                Deactivate();
                if (listener.PCone != null)
                {
                    Free(listener.PCone);
                    listener.PCone = null;
                }
                disposedValue = true;
            }
        }

        ~SoundListener()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}