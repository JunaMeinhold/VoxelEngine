#include "defs.hlsl"
#include "../../../common.hlsl"

cbuffer WorldData
{
	float3 chunkOffset;
	float padd;
};

GeometryInput main(float3 position : POSITION)
{
	float3 worldPos = position + chunkOffset * 16;
	GeometryInput output;
	output.position = worldPos;
	return output;
}