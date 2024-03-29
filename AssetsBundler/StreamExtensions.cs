﻿namespace AssetsBundler
{
    using System;
    using System.Buffers;
    using System.IO;
    using System.Text;

    public static class StreamExtensions
    {
        public static void WriteString(this Stream stream, string str)
        {
            stream.WriteInt(str.Length);
            stream.Write(Encoding.UTF8.GetBytes(str));
        }

        public static string ReadString(this Stream stream)
        {
            int length = stream.ReadInt();
            byte[] buffer = new byte[length];
            stream.Read(buffer);
            return Encoding.UTF8.GetString(buffer);
        }

        public static void WriteInt(this Stream stream, int val)
        {
            stream.Write(BitConverter.GetBytes(val));
        }

        public static int ReadInt(this Stream stream)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(4);
            stream.Read(buffer, 0, 4);
            int val = BitConverter.ToInt32(buffer);
            ArrayPool<byte>.Shared.Return(buffer);
            return val;
        }

        public static void WriteInt64(this Stream stream, long val)
        {
            stream.Write(BitConverter.GetBytes(val));
        }

        public static long ReadInt64(this Stream stream)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(8);
            stream.Read(buffer, 0, 8);
            long val = BitConverter.ToInt64(buffer);
            ArrayPool<byte>.Shared.Return(buffer);
            return val;
        }

        public static byte[] Read(this Stream stream, long length)
        {
            byte[] buffer = new byte[length];
            stream.Read(buffer, 0, (int)length);
            return buffer;
        }
    }
}