﻿namespace VoxelEngine.Voxel.Metadata
{
    using System.Buffers.Binary;
    using System.Runtime.CompilerServices;

    public interface IBlockMetadata
    {
        public int SizeOf();

        public int Read(ReadOnlySpan<byte> source);

        public int Write(Span<byte> destination);
    }

    public unsafe struct BlockMetadata
    {
        public LocalChunkPoint Position;
        public BlockMetadataType Type;
        public byte* Data;
        public int Length;
        private int capacity;

        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => capacity;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                if (capacity == value) return;
                if (Data == null)
                {
                    Data = AllocT<byte>(value);
                }
                else
                {
                    Data = ReAllocT(Data, value);
                }

                capacity = value;
            }
        }

        public void Release()
        {
            if (Data != null)
            {
                Free(Data);
                Data = null;
                Length = 0;
                capacity = 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void EnsureCapacity(int size)
        {
            if (Data == null || capacity < size)
            {
                int newCapacity = Math.Max(capacity * 2, size);
                Capacity = newCapacity;
            }
        }

        public void Resize(int size)
        {
            EnsureCapacity(size);
            Length = size;
        }

        public readonly Span<byte> AsSpan()
        {
            return new Span<byte>(Data, Length);
        }

        public readonly T ReadAs<T>() where T : IBlockMetadata, new()
        {
            T t = new();
            t.Read(AsSpan());
            return t;
        }

        public void Write<T>(T meta) where T : IBlockMetadata
        {
            int size = meta.SizeOf();
            EnsureCapacity(size);
            Length = size;
            meta.Write(AsSpan());
        }

        public readonly void Write(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[3 + 4 + 4];
            buffer[0] = Position.X;
            buffer[1] = Position.Y;
            buffer[2] = Position.Z;
            BinaryPrimitives.WriteInt32LittleEndian(buffer[3..], (int)Type);
            BinaryPrimitives.WriteInt32LittleEndian(buffer[7..], Length);
            stream.Write(buffer);
            stream.Write(AsSpan());
        }

        public void Read(Stream stream)
        {
            Span<byte> buffer = stackalloc byte[3 + 4 + 4];
            stream.ReadExactly(buffer);
            Position.X = buffer[0];
            Position.Y = buffer[1];
            Position.Z = buffer[2];
            Type = (BlockMetadataType)BinaryPrimitives.ReadInt32LittleEndian(buffer[3..]);
            capacity = Length = BinaryPrimitives.ReadInt32LittleEndian(buffer[7..]);
            if (Length > 0)
            {
                EnsureCapacity(Length);
                stream.ReadExactly(AsSpan());
            }
        }

        public static BlockMetadata ReadFrom(Stream stream)
        {
            BlockMetadata metadata = new();
            metadata.Read(stream);
            return metadata;
        }
    }
}