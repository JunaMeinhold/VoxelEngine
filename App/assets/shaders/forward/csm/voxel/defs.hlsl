struct GeometryInput
{
	float3 position : POSITION;
};

struct PixelInput
{
	float4 position : SV_POSITION;
	uint rtvIndex : SV_RenderTargetArrayIndex;
	float depth : DEPTH;
};

struct PatchTess
{
	float EdgeTess[3] : SV_TessFactor;
	float InsideTess : SV_InsideTessFactor;
};