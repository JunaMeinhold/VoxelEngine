#include "defs.hlsl"
#include "../../../common.hlsl"

cbuffer MatrixBuffer : register(b0)
{
    matrix world;
};

cbuffer WorldData : register(b1)
{
    float3 chunkOffset;
    float padd;
};

GeometryInput main(int aData : POSITION, float3 offset : POSITION1)
{
    GeometryInput output;

    float3 position = float3(float(aData & (63)), float((aData >> 6) & (63)), float((aData >> 12) & (63))) + offset;

    output.position = mul(float4(position, 1), world);
    return output;
}