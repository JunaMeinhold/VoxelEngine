namespace VoxelEngine.Rendering.Shaders
{
    using System.Reflection;
    using Vortice.Direct3D;
    using Vortice.Direct3D11;
    using Vortice.DXGI;
    using VoxelEngine.Rendering.D3D.Attributes;
    using VoxelEngine.Rendering.D3D.Interfaces;

    public struct PipelineDesc
    {
        public string? VertexShader = null;
        public string VertexShaderEntrypoint = "main";
        public string? HullShader = null;
        public string HullShaderEntrypoint = "main";
        public string? DomainShader = null;
        public string DomainShaderEntrypoint = "main";
        public string? GeometryShader = null;
        public string GeometryShaderEntrypoint = "main";
        public string? PixelShader = null;
        public string PixelShaderEntrypoint = "main";

        public RasterizerDescription Rasterizer = RasterizerDescription.CullBack;
        public DepthStencilDescription DepthStencil = DepthStencilDescription.Default;
        public BlendDescription Blend = BlendDescription.Opaque;
        public PrimitiveTopology Topology = PrimitiveTopology.Undefined;

        public PipelineDesc()
        {
        }

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