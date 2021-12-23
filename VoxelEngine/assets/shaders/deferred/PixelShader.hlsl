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
SamplerState SampleTypeWrap : register(s0);

//////////////
// TYPEDEFS //
//////////////
struct PixelInputType
{
	float4 position : SV_POSITION;
	float3 tex : TEXCOORD0;
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
PixelOutputType GPixelShader(PixelInputType input)
{
	PixelOutputType output;
	output.color = shaderTexture.Sample(SampleTypeWrap, input.tex);
	output.position = input.position;
	output.normal = float4(input.normal, 1.0f);
	output.depth = float4(input.depth, input.depth, input.depth, 1.0f);
	return output;
}