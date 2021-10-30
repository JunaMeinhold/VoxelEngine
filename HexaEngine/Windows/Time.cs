namespace HexaEngine.Windows
{
    using System;
    using System.Diagnostics;

    public static class Time
    {
        private static float fixedTime;

        // Variables
        private static Stopwatch _StopWatch;

        private static float m_ticksPerMs;
        private static long m_LastFrameTime = 0;

        // Properties
        public static float Delta { get; private set; }

        public static float CumulativeFrameTime { get; private set; }

        public static event EventHandler FixedUpdate;

        public static int FixedUpdateRate { get; set; } = 3;

        public static float FixedUpdatePerSecond => FixedUpdateRate / 1000F;

        // Public Methods
        public static bool Initialize()
        {
            // Check to see if this system supports high performance timers.
            if (!Stopwatch.IsHighResolution)
                return false;
            if (Stopwatch.Frequency == 0)
                return false;

            // Find out how many times the frequency counter ticks every millisecond.
            m_ticksPerMs = Stopwatch.Frequency / 1000.0f;

            _StopWatch = Stopwatch.StartNew();
            return true;
        }

        public static void FrameUpdate()
        {
            // Query the current time.
            long currentTime = _StopWatch.ElapsedTicks;

            // Calculate the difference in time since the last time we queried for the current time.
            float timeDifference = currentTime - m_LastFrameTime;

            // Calculate the frame time by the time difference over the timer speed resolution.
            Delta = timeDifference / m_ticksPerMs / 1000;
            CumulativeFrameTime += Delta;

            // record this Frames durations to the LastFrame for next frame processing.
            m_LastFrameTime = currentTime;
            fixedTime += Delta;
            while (fixedTime > FixedUpdatePerSecond)
            {
                fixedTime -= FixedUpdatePerSecond;
                FixedUpdate?.Invoke(null, null);
            }
        }
    }
}