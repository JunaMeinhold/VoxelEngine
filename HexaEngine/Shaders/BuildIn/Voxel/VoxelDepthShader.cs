namespace HexaEngine.Shaders.BuildIn.Voxel
{
    using HexaEngine.Resources;
    using HexaEngine.Resources.Buffers;
    using HexaEngine.Scenes.Interfaces;
    using HexaEngine.Windows;
    using System.Numerics;
    using System.Runtime.InteropServices;
    using Vortice.Direct3D11;
    using Vortice.DXGI;
    using VoxelGen;

    public class VoxelDepthShader : Shader
    {
        public ID3D11Buffer MatrixBuffer;

        public VoxelDepthShader()
        {
            VertexShaderDescription = new("voxel/DepthVertexShader.hlsl", "main", VertexShaderVersion.VS_5_0);
            PixelShaderDescription = new("voxel/DepthPixelShader.hlsl", "main", PixelShaderVersion.PS_5_0);
            InputElements.Add(new("POSITION", 0, Format.R32_SInt, 0, 0, InputClassification.PerVertexData, 0));
            Initialize();

            var matrixBufferDesc = new BufferDescription(Marshal.SizeOf<PerFrameBuffer2>(), BindFlags.ConstantBuffer, ResourceUsage.Dynamic) { CpuAccessFlags = CpuAccessFlags.Write };
            MatrixBuffer = CreateBuffer(matrixBufferDesc, nameof(MatrixBuffer));
        }

        public override void Render(IView view, Matrix4x4 transform, int indexCount)
        {
            Write(MatrixBuffer, new PerFrameBuffer()
            {
                Projection = Matrix4x4.Transpose(view.ProjectionMatrix),
                View = Matrix4x4.Transpose(view.ViewMatrix),
                World = Matrix4x4.Transpose(transform)
            });

            Manager.ID3D11DeviceContext.VSSetConstantBuffer(0, MatrixBuffer);

            Manager.ID3D11DeviceContext.IASetInputLayout(InputLayout);
            Manager.ID3D11DeviceContext.VSSetShader(VertexShader);
            Manager.ID3D11DeviceContext.PSSetShader(PixelShader);

            Manager.ID3D11DeviceContext.PSSetShaderResource(0, null);
            Manager.ID3D11DeviceContext.DrawIndexed(indexCount, 0, 0);
        }

        public void Render(IView view, Chunk chunk)
        {
            if (chunk is null) return;
            if (!chunk.Render(DeviceManager.Current.ID3D11DeviceContext)) return;

            var model = Matrix4x4.CreateTranslation(new(chunk.chunkPosX * Chunk.CHUNK_SIZE, chunk.chunkPosY * Chunk.CHUNK_SIZE, chunk.chunkPosZ * Chunk.CHUNK_SIZE));
            Write(MatrixBuffer, new PerFrameBuffer2()
            {
                MVP = Matrix4x4.Transpose(model * view.ViewMatrix * view.ProjectionMatrix)
            });

            Manager.ID3D11DeviceContext.VSSetConstantBuffer(0, MatrixBuffer);
            Manager.ID3D11DeviceContext.IASetInputLayout(InputLayout);
            Manager.ID3D11DeviceContext.VSSetShader(VertexShader);
            Manager.ID3D11DeviceContext.PSSetShader(PixelShader);

            Manager.ID3D11DeviceContext.Draw(chunk.vertexBuffer.VertexCount, 0);
        }

        protected override void Dispose(bool disposing)
        {
            MatrixBuffer.Dispose();
            MatrixBuffer = null;
            base.Dispose(disposing);
        }
    }
}