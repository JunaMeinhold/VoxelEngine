namespace VoxelEngine.Voxel
{
    public interface IVoxelVertexBuffer
    {
        int this[int index] { get; set; }

        int Capacity { get; set; }

        int Count { get; }

        void Dispose();

        void EnsureCapacity(int capacity);

        void Increase(int count);

        void Reset(int length = 4096);
    }
}