namespace VoxelEngine.Voxel.Serialization
{
    using K4os.Compression.LZ4;
    using System.Buffers.Binary;
    using System.IO;
    using System.Runtime.CompilerServices;

    public unsafe struct UnsafeLZ4Stream : IDisposable
    {
        private readonly int blockSize;
        private readonly StreamMode mode;
        private readonly LZ4Level level;
        private readonly int outputSize;
        private readonly Stream innerStream;
        private byte* rawBuffer;
        private byte* compressedBuffer;

        private int bufferPosition;
        private int bufferedSize;

        public UnsafeLZ4Stream(Stream stream, int blockSize, StreamMode mode, LZ4Level level)
        {
            innerStream = stream;
            this.blockSize = blockSize;
            this.mode = mode;
            this.level = level;
            outputSize = LZ4Codec.MaximumOutputSize(blockSize) + 4;
            rawBuffer = AllocT<byte>(blockSize);
            compressedBuffer = AllocT<byte>(outputSize);
        }

        public void Write(ReadOnlySpan<byte> buffer)
        {
            fixed (byte* pBuffer = buffer)
            {
                Write(pBuffer, buffer.Length);
            }
        }

        public void Write(byte[] buffer, int offset, int length)
        {
            if (buffer.Length < offset + length) throw new ArgumentOutOfRangeException(nameof(buffer), "Offset or length exceeded the buffer size.");
            fixed (byte* pBuffer = buffer)
            {
                Write(pBuffer + offset, length);
            }
        }

        public void Write(byte* buffer, int length)
        {
            if (mode != StreamMode.Write) throw new InvalidOperationException("Cannot write to an read-only stream.");
            while (length > 0)
            {
                int remaining = blockSize - bufferPosition;
                int toWrite = Math.Min(remaining, length);
                MemcpyT(buffer, rawBuffer + bufferPosition, toWrite);
                bufferPosition += toWrite;
                length -= toWrite;
                buffer += toWrite;
                if (bufferPosition == blockSize)
                {
                    WriteFrame();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void WriteFrame()
        {
            int written = LZ4Codec.Encode(rawBuffer, bufferPosition, compressedBuffer + 4, outputSize - 4, level);
            Span<byte> buffer = new(compressedBuffer, written + 4);
            BinaryPrimitives.WriteInt32LittleEndian(buffer, written);
            innerStream.Write(buffer);
            bufferPosition = 0;
        }

        public void Read(Span<byte> buffer)
        {
            fixed (byte* pBuffer = buffer)
            {
                Read(pBuffer, buffer.Length);
            }
        }

        public void Read(byte[] buffer, int offset, int length)
        {
            if (buffer.Length < offset + length) throw new ArgumentOutOfRangeException(nameof(buffer), "Offset or length exceeded the buffer size.");
            fixed (byte* pBuffer = buffer)
            {
                Read(pBuffer + offset, length);
            }
        }

        public void Read(byte* buffer, int length)
        {
            if (mode != StreamMode.Read) throw new InvalidOperationException("Cannot read from an write-only stream.");
            while (length > 0)
            {
                if (bufferPosition == bufferedSize)
                {
                    ReadFrame();
                }

                int remaining = bufferedSize - bufferPosition;
                int toRead = Math.Min(remaining, length);
                MemcpyT(rawBuffer + bufferPosition, buffer, toRead);

                bufferPosition += toRead;
                length -= toRead;
                buffer += toRead;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ReadFrame()
        {
            Span<byte> lengthBuffer = new(compressedBuffer, 4);
            innerStream.ReadExactly(lengthBuffer);
            int compressedSize = BinaryPrimitives.ReadInt32LittleEndian(lengthBuffer);

            innerStream.ReadExactly(new Span<byte>(compressedBuffer + 4, compressedSize));

            bufferedSize = LZ4Codec.Decode(compressedBuffer + 4, compressedSize, rawBuffer, blockSize);
            bufferPosition = 0;
        }

        public void Flush()
        {
            if (mode == StreamMode.Write)
            {
                if (bufferPosition > 0)
                {
                    WriteFrame();
                }
                innerStream.Flush();
            }
        }

        public void Release()
        {
            if (rawBuffer != null)
            {
                Free(rawBuffer);
                rawBuffer = null;
            }
            if (compressedBuffer != null)
            {
                Free(compressedBuffer);
                compressedBuffer = null;
            }
        }

        public void Dispose()
        {
            Release();
        }
    }
}