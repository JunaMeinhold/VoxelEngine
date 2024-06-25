struct GeometryInput
{
    float4 position : POSITION;
};

struct PixelInput
{
    float4 position : SV_POSITION;
    float2 shadowCoord : TEXCOORD0;
    uint rtIndex : SV_RenderTargetArrayIndex;
};

struct PatchTess
{
    float EdgeTess[3] : SV_TessFactor;
    float InsideTess : SV_InsideTessFactor;
};