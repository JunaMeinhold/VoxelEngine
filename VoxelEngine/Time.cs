namespace VoxelEngine
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;

    public static class Time
    {
        private static int frame;
        private static int frameRate;
        private static float frameTime;
        private static float fixedTime;
        private static float delta;
        private static float cumulativeFrameTime;
        private static int fixedUpdateRate = 3;
        private static Stopwatch stopwatch;
        private static float gameTime;
        private static float gameTimeNormalized;

        // Properties
        public static float Delta { get => delta; private set => delta = value; }

        public static float CumulativeFrameTime { get => cumulativeFrameTime; private set => cumulativeFrameTime = value; }

        public static int FixedUpdateRate { get => fixedUpdateRate; set => fixedUpdateRate = value; }

        public static float FixedUpdatePerSecond => FixedUpdateRate / 1000F;

        public static int FrameRate => frameRate;

        public static float GameTime => gameTime;

        public static float GameTimeNormalized => gameTimeNormalized;

        public static float TimeScale = 10;

        public static event EventHandler FixedUpdate;

        // Public Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Initialize()
        {
            stopwatch = Stopwatch.StartNew();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void FrameUpdate()
        {
            delta = (float)stopwatch.Elapsed.TotalSeconds;
            stopwatch.Restart();

            // Calculate the frame time by the time difference over the timer speed resolution.
            cumulativeFrameTime += delta;
            fixedTime += delta;
            frameTime += delta;
            gameTime += (delta * TimeScale) / 60 / 60;
            if (gameTime > 24)
            {
                gameTime -= 24;
            }
            gameTimeNormalized = gameTime / 24;

            frame++;

            while (frameTime > 1)
            {
                frameTime--;
                frameRate = frame;
                frame = 0;
            }

            while (fixedTime > FixedUpdatePerSecond)
            {
                fixedTime -= FixedUpdatePerSecond;
                FixedUpdate?.Invoke(null, null);
            }
        }
    }
}