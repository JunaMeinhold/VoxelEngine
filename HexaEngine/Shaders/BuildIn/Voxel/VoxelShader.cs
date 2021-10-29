namespace HexaEngine.Shaders.BuildIn.Voxel
{
    using HexaEngine.Resources.Buffers;
    using HexaEngine.Scenes.Interfaces;
    using HexaEngine.Windows;
    using System;
    using System.Numerics;
    using System.Runtime.InteropServices;
    using Vortice.Direct3D11;
    using Vortice.DXGI;
    using VoxelGen;

    public class VoxelShader : Shader
    {
        public readonly ID3D11Buffer MatrixBuffer;
        public readonly ID3D11Buffer WorldBuffer;

        [StructLayout(LayoutKind.Sequential)]
        private struct WorldData
        {
            public Vector3 chunkOffset;
            public float padd;
        }

        public VoxelShader()
        {
            VertexShaderDescription = new("voxel/VertexShader.hlsl", "VoxelVertexShader", VertexShaderVersion.VS_5_0);
            PixelShaderDescription = new("voxel/PixelShader.hlsl", "VoxelPixelShader", PixelShaderVersion.PS_5_0);
            InputElements.Add(new("POSITION", 0, Format.R32_SInt, 0, 0, InputClassification.PerVertexData, 0));
            Initialize();

            var matrixBufferDesc = new BufferDescription(Marshal.SizeOf<PerFrameBuffer2>(), BindFlags.ConstantBuffer, ResourceUsage.Dynamic) { CpuAccessFlags = CpuAccessFlags.Write };
            MatrixBuffer = CreateBuffer(matrixBufferDesc, nameof(MatrixBuffer));

            var worldBufferDesc = new BufferDescription(Marshal.SizeOf<WorldData>(), BindFlags.ConstantBuffer, ResourceUsage.Dynamic) { CpuAccessFlags = CpuAccessFlags.Write };
            WorldBuffer = CreateBuffer(worldBufferDesc, nameof(WorldBuffer));
        }

        /// <summary>
        /// Warning NotImplemented
        /// </summary>
        public override void Render(IView view, Matrix4x4 transform, int indexCount)
        {
            throw new NotImplementedException();
        }

        public void Render(IView view, Chunk chunk)
        {
            if (chunk is null) return;
            if (!chunk.Render(DeviceManager.Current.ID3D11DeviceContext)) return;

            Write(WorldBuffer, new WorldData()
            {
                chunkOffset = new(chunk.chunkPosX, chunk.chunkPosY, chunk.chunkPosZ)
            });

            var model = Matrix4x4.CreateTranslation(new(chunk.chunkPosX * Chunk.CHUNK_SIZE, chunk.chunkPosY * Chunk.CHUNK_SIZE, chunk.chunkPosZ * Chunk.CHUNK_SIZE));
            Write(MatrixBuffer, new PerFrameBuffer2()
            {
                MVP = Matrix4x4.Transpose(model * view.ViewMatrix * view.ProjectionMatrix)
            });

            Manager.ID3D11DeviceContext.VSSetConstantBuffer(0, MatrixBuffer);
            Manager.ID3D11DeviceContext.VSSetConstantBuffer(1, WorldBuffer);
            Manager.ID3D11DeviceContext.IASetInputLayout(InputLayout);
            Manager.ID3D11DeviceContext.VSSetShader(VertexShader);
            Manager.ID3D11DeviceContext.PSSetShader(PixelShader);

            Manager.ID3D11DeviceContext.Draw(chunk.vertexBuffer.VertexCount, 0);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            WorldBuffer.Dispose();
            MatrixBuffer.Dispose();
        }
    }
}