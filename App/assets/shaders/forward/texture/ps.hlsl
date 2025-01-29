#include "defs.hlsl"

Texture2D tex;
SamplerState linearClampSampler;

float4 main(PixelInput input) : SV_TARGET
{
	return tex.Sample(linearClampSampler, input.tex);
}