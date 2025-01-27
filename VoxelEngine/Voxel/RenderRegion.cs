namespace VoxelEngine.Voxel
{
    using System.Collections.Generic;
    using System.Numerics;
    using Hexa.NET.Mathematics;
    using Vortice.Direct3D11;

    public class RenderRegion
    {
        private readonly List<ChunkSegment> ChunkSegments = new();
        public BlockVertexBuffer vertexBuffer = new();
        public Vector3 Position;
        public Vector2 Offset;
        public Vector2 Size;
        public BoundingBox BoundingBox;
        public string Name;

        public RenderRegion(Vector2 offset, Vector2 size)
        {
            Position = new(offset.X, 0, offset.Y);
            Offset = offset;
            Size = size;
            Name = offset.ToString();
        }

        public BlockVertexBuffer VertexBuffer => vertexBuffer;

        public int RegionCount
        {
            get
            {
                lock (ChunkSegments)
                {
                    return ChunkSegments.Count;
                }
            }
        }

        public void AddRegion(ChunkSegment region)
        {
            lock (ChunkSegments)
            {
                ChunkSegments.Add(region);
            }
        }

        public void RemoveRegion(ChunkSegment region)
        {
            lock (ChunkSegments)
            {
                ChunkSegments.Remove(region);
            }
        }

        public bool ContainsRegion(ChunkSegment region)
        {
            lock (ChunkSegments)
            {
                return ChunkSegments.Contains(region);
            }
        }

        public bool ContainsRegionPos(Vector2 point)
        {
            Vector2 max = Offset + Size;
            if (Offset.X <= point.X && max.X > point.X &&
                Offset.Y <= point.Y && max.Y > point.Y)
                return true;

            return false;
        }

        public void Update(ID3D11Device device, ID3D11DeviceContext context)
        {
            vertexBuffer.Reset();
            var max = Offset + Size;
            BoundingBox = new(new Vector3(Offset.X, 0, Offset.Y) * Chunk.CHUNK_SIZE, new Vector3(max.X, World.CHUNK_AMOUNT_Y, max.Y) * Chunk.CHUNK_SIZE);
            for (int i = 0; i < ChunkSegments.Count; i++)
            {
                ChunkSegment region = ChunkSegments[i];
                for (int j = 0; j < ChunkSegment.CHUNK_SEGMENT_SIZE; j++)
                {
                    Chunk chunk = region.Chunks[j];

#if !USE_LEGACY_LOADER
                    vertexBuffer.BufferData(chunk.VertexBuffer, chunk.Position * Chunk.CHUNK_SIZE);
#endif
                }
            }

            vertexBuffer.BufferData(device);
        }

        public void Release()
        {
            vertexBuffer?.Dispose();
        }

        public void Bind(ID3D11DeviceContext context)
        {
            vertexBuffer.Bind(context);
        }
    }
}