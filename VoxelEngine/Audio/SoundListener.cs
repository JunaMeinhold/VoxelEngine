namespace VoxelEngine.Audio
{
    using System.Numerics;
    using Vortice.XAudio2;

    public class SoundListener
    {
        private bool disposedValue;

        public SoundListener()
        {
            Listener = new();
        }

        public Listener Listener { get; set; }

        public static SoundListener Active { get; private set; }

        public Vector3 OrientFront { get => Listener.OrientFront; set => Listener.OrientFront = value; }

        public Vector3 OrientTop { get => Listener.OrientTop; set => Listener.OrientTop = value; }

        public Vector3 Position { get => Listener.Position; set => Listener.Position = value; }

        public Vector3 Velocity { get => Listener.Velocity; set => Listener.Velocity = value; }

        public Cone? Cone { get => Listener.Cone; set => Listener.Cone = value; }

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