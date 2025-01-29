namespace VoxelEngine.Graphics.D3D11
{
    using Hexa.NET.D3D11;
    using HexaGen.Runtime.COM;
    using System.Runtime.CompilerServices;

    public static class DeviceHelper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Write<T>(ComPtr<ID3D11DeviceContext> context, ComPtr<ID3D11Buffer> buffer, T value) where T : unmanaged
        {
            MappedSubresource mapped;
            context.Map(buffer.As<ID3D11Resource>(), 0, Map.WriteDiscard, 0, &mapped);
            Buffer.MemoryCopy(&value, mapped.PData, mapped.RowPitch, sizeof(T));
            context.Unmap(buffer.As<ID3D11Resource>(), 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Write<T>(ComPtr<ID3D11DeviceContext> context, ComPtr<ID3D11Buffer> buffer, T[] values) where T : unmanaged
        {
            fixed (T* pData = values)
            {
                Write(context, buffer, pData, values.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Write<T>(ComPtr<ID3D11DeviceContext> context, ComPtr<ID3D11Buffer> buffer, T* values, int count) where T : unmanaged
        {
            MappedSubresource mapped;
            context.Map(buffer.As<ID3D11Resource>(), 0, Map.WriteDiscard, 0, &mapped);
            Buffer.MemoryCopy(values, mapped.PData, mapped.RowPitch, sizeof(T) * count);
            context.Unmap(buffer.As<ID3D11Resource>(), 0);
        }
    }
}