////////////////////////////////////////////////////////////////////////////////
// Filename: deferred.ps
////////////////////////////////////////////////////////////////////////////////

//////////////
// TEXTURES //
//////////////
Texture2DArray shaderTexture : register(t0);

///////////////////
// SAMPLE STATES //
///////////////////
SamplerState Sampler : register(s0);

//////////////
// TYPEDEFS //
//////////////
struct PixelInputType
{
	float4 position : SV_POSITION;
	int texID : TEXCOORD0;
	float2 uv : TEXCOORD1;
	float brightness : TEXCOORD2;
	float3 normal : NORMAL;
	float depth : DEPTH;
};

struct PixelOutputType
{
	float4 color : SV_Target0;
	float4 position : SV_Target1;
	float4 normal : SV_Target2;
	float4 depth : SV_Target3;
};

////////////////////////////////////////////////////////////////////////////////
// Pixel Shader
////////////////////////////////////////////////////////////////////////////////
PixelOutputType VoxelPixelShader(PixelInputType input)
{
	PixelOutputType output;
	output.color = float4(shaderTexture.Sample(Sampler, float3(input.uv, input.texID)).rgb * input.brightness, 1.0);
	output.position = input.position;
	output.normal = float4(input.normal, 1.0f);
	output.depth = float4(input.depth, input.depth, input.depth, 1.0f);
	return output;
}