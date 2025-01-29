#include "../../camera.hlsl"
#include "defs.hlsl"

cbuffer ModelBuffer
{
	float4x4 model;
};

PixelInput main(VertexInput input)
{
	PixelInput output;

	output.pos = mul(float4(input.pos, 1), model);
	output.pos = mul(output.pos, viewProj);
	output.tex = input.tex;
	return output;
}