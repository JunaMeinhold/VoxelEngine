﻿namespace VoxelEngine.Core
{
    using Silk.NET.SDL;

    public static unsafe class CursorHelper
    {
        private static readonly Sdl Sdl = Sdl.GetApi();

        public static void SetCursor(SystemCursor cursor)
        {
            Sdl.SetCursor(Sdl.CreateSystemCursor(cursor));
        }

        public static void SetCursor(IntPtr ptr)
        {
            Sdl.SetCursor((Cursor*)ptr.ToPointer());
        }
    }
}