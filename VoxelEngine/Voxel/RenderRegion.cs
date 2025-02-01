namespace VoxelEngine.Voxel
{
    using Hexa.NET.Mathematics;
    using System.Collections.Generic;
    using System.Numerics;
    using VoxelEngine.Graphics;
    using VoxelEngine.Voxel.Meshing;

    public class RenderRegion
    {
        private readonly List<ChunkSegment> ChunkSegments = new();
        public RegionVertexBuffer vertexBuffer = new();
        public Vector3 Position;
        public Vector2 Offset;
        public Vector2 Size;
        public BoundingBox BoundingBox;
        public string Name;
        public int IsDirty;

        public RenderRegion(Vector2 offset, Vector2 size)
        {
            Position = new(offset.X, 0, offset.Y);
            Offset = offset;
            Size = size;
            Name = offset.ToString();
        }

        public RegionVertexBuffer VertexBuffer => vertexBuffer;

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

        public bool SetDirty()
        {
            var wasDirty = Interlocked.CompareExchange(ref IsDirty, 1, 0);
            return wasDirty == 0;
        }

        public void Update(GraphicsContext context)
        {
            Interlocked.Exchange(ref IsDirty, 0);

            int verts = 0;

            for (int i = 0; i < ChunkSegments.Count; i++)
            {
                ChunkSegment region = ChunkSegments[i];
                for (int j = 0; j < ChunkSegment.CHUNK_SEGMENT_SIZE; j++)
                {
                    Chunk chunk = region.Chunks[j];
                    chunk.VertexBuffer.Lock();
                    verts += chunk.VertexBuffer.Count;
                }
            }

            vertexBuffer.Map(context, verts);

            var max = Offset + Size;
            BoundingBox = new(new Vector3(Offset.X, 0, Offset.Y) * Chunk.CHUNK_SIZE, new Vector3(max.X, World.CHUNK_AMOUNT_Y, max.Y) * Chunk.CHUNK_SIZE);

            for (int i = 0; i < ChunkSegments.Count; i++)
            {
                ChunkSegment region = ChunkSegments[i];
                for (int j = 0; j < ChunkSegment.CHUNK_SEGMENT_SIZE; j++)
                {
                    Chunk chunk = region.Chunks[j];
                    vertexBuffer.BufferData(chunk.VertexBuffer, chunk.Position * Chunk.CHUNK_SIZE);
                    chunk.VertexBuffer.ReleaseLock();
                }
            }

            vertexBuffer.Unmap(context);
        }

        public void Release()
        {
            vertexBuffer?.Dispose();
        }

        public void Bind(GraphicsContext context)
        {
            vertexBuffer.Bind(context);
        }
    }
}