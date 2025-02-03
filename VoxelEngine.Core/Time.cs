namespace VoxelEngine.Core
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;

    public static class Time
    {
        private static long last;
        private static int frame;
        private static int frameRate;
        private static float frameTime;
        private static float fixedTime;
        private static float delta;
        private static float cumulativeFrameTime;
        private static int fixedUpdateRate = 10;
        private static float gameTime = 12;
        private static float gameTimeNormalized;

        // Properties
        public static float Delta { get => delta; private set => delta = value; }

        public static float CumulativeFrameTime { get => cumulativeFrameTime; private set => cumulativeFrameTime = value; }

        public static int FixedUpdateRate { get => fixedUpdateRate; set => fixedUpdateRate = value; }

        public static float FixedDelta => 1f / FixedUpdateRate;

        public static float FixedUpdatePerSecond => 1000F / FixedUpdateRate / 1000f;

        public static int FrameRate => frameRate;

        public static float GameTime { get => gameTime; set => gameTime = value; }

        public static float GameTimeNormalized => gameTimeNormalized;

        public static float TimeScale = 60;

        public static event EventHandler FixedUpdate;

        // Public Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Initialize()
        {
            last = Stopwatch.GetTimestamp();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void FrameUpdate()
        {
            long now = Stopwatch.GetTimestamp();
            delta = (float)(now - last) / Stopwatch.Frequency;
            last = now;

            // Calculate the frame time by the time difference over the timer speed resolution.
            cumulativeFrameTime += delta;
            fixedTime += delta;
            frameTime += delta;

            gameTime += delta * TimeScale / 60 / 60;
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