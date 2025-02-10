namespace VoxelEngine.Voxel
{
    using Hexa.NET.Mathematics;
    using System.Collections.Generic;
    using System.Numerics;
    using VoxelEngine.Graphics;
    using VoxelEngine.Voxel.Meshing;

    public unsafe class RenderRegion
    {
        private readonly Lock @lock = new();
        private readonly List<ChunkSegment> ChunkSegments = new();
        public RegionVertexBuffer vertexBuffer = new();
        public Point3 Position;
        public Point2 Offset;
        public Point2 Size;
        public BoundingBox BoundingBox;
        public int IsDirty;

        public RenderRegion(Point2 offset, Point2 size)
        {
            Position = new(offset.X, 0, offset.Y);
            Offset = offset;
            Size = size;
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
            lock (@lock)
            {
                ChunkSegments.Add(region);
                IsDirty++;
            }
        }

        public void RemoveRegion(ChunkSegment region)
        {
            lock (@lock)
            {
                ChunkSegments.Remove(region);
                IsDirty++;
            }
        }

        public bool ContainsRegion(ChunkSegment region)
        {
            lock (@lock)
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
            lock (@lock)
            {
                IsDirty++;
                return true;
            }
        }

        public void Update(GraphicsContext context)
        {
            lock (@lock)
            {
                int value = IsDirty;
                if (value == 0) return;
                IsDirty = 0;

                int verts = 0;

                for (int i = 0; i < ChunkSegments.Count; i++)
                {
                    ChunkSegment region = ChunkSegments[i];
                    for (int j = 0; j < ChunkSegment.CHUNK_SEGMENT_SIZE; j++)
                    {
                        Chunk* chunk = region.Chunks[j];
                        chunk->VertexBuffer.Lock();
                        verts += chunk->VertexBuffer.Count;
                    }
                }

                vertexBuffer.Map(context, verts);

                var min = new Point3(Offset.X, 0, Offset.Y);
                var max = Offset + Size;
                BoundingBox = new(new Vector3(Offset.X, 0, Offset.Y) * Chunk.CHUNK_SIZE, new Vector3(max.X, World.CHUNK_AMOUNT_Y, max.Y) * Chunk.CHUNK_SIZE);

                for (int i = 0; i < ChunkSegments.Count; i++)
                {
                    ChunkSegment region = ChunkSegments[i];
                    for (int j = 0; j < ChunkSegment.CHUNK_SEGMENT_SIZE; j++)
                    {
                        Chunk* chunk = region.Chunks[j];
                        vertexBuffer.BufferData(chunk->VertexBuffer, (chunk->Position - min) * Chunk.CHUNK_SIZE);
                        chunk->VertexBuffer.ReleaseLock();
                    }
                }

                vertexBuffer.Unmap(context);
            }
        }

        public void Release()
        {
            vertexBuffer?.Dispose();
        }

        public bool Bind(GraphicsContext context)
        {
            return vertexBuffer.Bind(context);
        }
    }
}