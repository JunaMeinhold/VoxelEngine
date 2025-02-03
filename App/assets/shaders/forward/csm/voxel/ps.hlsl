#include "defs.hlsl"

float4 main(PixelInput input) : SV_Target
{
	float depth = input.depth;
	return float4(depth, depth * depth, 0, 0);
}