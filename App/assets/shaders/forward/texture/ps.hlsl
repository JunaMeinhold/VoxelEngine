#include "defs.hlsl"

Texture2D texture;
SamplerState linearClampSampler;

float4 main(PixelInput input) : SV_TARGET
{
	return texture.Sample(linearClampSampler, input.tex);
}