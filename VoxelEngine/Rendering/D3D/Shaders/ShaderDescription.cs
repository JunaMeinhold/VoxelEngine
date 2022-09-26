namespace VoxelEngine.Rendering.D3D.Shaders
{
    using System.Reflection;
    using Vortice.Direct3D;
    using Vortice.Direct3D11;
    using Vortice.DXGI;
    using VoxelEngine.Rendering.D3D.Attributes;
    using VoxelEngine.Rendering.D3D.Interfaces;

    public struct ShaderDescription
    {
        public VertexShaderDescription? VertexShader;
        public HullShaderDescription? HullShader;
        public DomainShaderDescription? DomainShader;
        public GeometryShaderDescription? GeometryShader;
        public PixelShaderDescription? PixelShader;

        public RasterizerDescription Rasterizer;
        public DepthStencilDescription DepthStencil;
        public BlendDescription Blend;

        public InputElementDescription[] InputElements;
        public IConstantBuffer[] ConstantBuffers;
        public IShaderResource[] ShaderResources;
        public PrimitiveTopology Topology;

        public static InputElementDescription[] GenerateInputElements<T>() where T : unmanaged
        {
            List<InputElementDescription> inputElements = new();
            Type type = typeof(T);
            InputClassification classification = 0;
            if (type.GetCustomAttribute<PerVertexDataAttribute>() != null)
            {
                classification = InputClassification.PerVertexData;
            }

            if (type.GetCustomAttribute<PerInstanceDataAttribute>() != null)
            {
                classification = InputClassification.PerInstanceData;
            }

            foreach (FieldInfo item in type.GetFields())
            {
                string semanticName = item.GetCustomAttribute<SemanticNameAttribute>().Name;
                int semanticIndex = item.GetCustomAttribute<SemanticIndexAttribute>().Index;
                int offset = item.GetCustomAttribute<OffsetAttribute>().Offset;
                Format format = item.GetCustomAttribute<FormatAttribute>().Format;
                inputElements.Add(new InputElementDescription(semanticName, semanticIndex, format, offset, 0, classification, 0));
            }

            return inputElements.ToArray();
        }
    }
}