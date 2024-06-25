namespace VoxelEngine.Rendering.D3D
{
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Vortice.Direct3D11;

    public static class DeviceHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Write<T>(ID3D11DeviceContext context, ID3D11Buffer buffer, T value) where T : unmanaged
        {
            MappedSubresource mapped = context.Map(buffer, MapMode.WriteDiscard);
            Buffer.MemoryCopy(&value, (void*)mapped.DataPointer, mapped.RowPitch, sizeof(T));
            context.Unmap(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Write<T>(ID3D11DeviceContext context, ID3D11Buffer buffer, T[] values) where T : struct
        {
            MappedSubresource mapped = context.Map(buffer, MapMode.WriteDiscard);
            var size = Marshal.SizeOf<T>();
            var basePtr = Marshal.AllocHGlobal(size * values.Length);
            var ptr = basePtr.ToInt64();
            for (int i = 0; i < values.Length; i++)
            {
                Marshal.StructureToPtr(values[i], (IntPtr)ptr, true);
                ptr += size;
            }
            Buffer.MemoryCopy((void*)basePtr, (void*)mapped.DataPointer, mapped.RowPitch, size * values.Length);
            Marshal.FreeHGlobal(basePtr);
            context.Unmap(buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Write<T>(ID3D11DeviceContext context, ID3D11Buffer buffer, T* values, int count) where T : unmanaged
        {
            MappedSubresource mapped = context.Map(buffer, MapMode.WriteDiscard);
            var size = Marshal.SizeOf<T>();
            Buffer.MemoryCopy(values, (void*)mapped.DataPointer, mapped.RowPitch, size * count);
            context.Unmap(buffer);
        }
    }
}