#include "../../../camera.hlsl"
#include "defs.hlsl"

cbuffer ModelBuffer
{
	float4x4 model;
};

cbuffer WorldData
{
	float3 chunkOffset;
	float padd;
};

cbuffer TexData
{
	BlockDescription descs[256];
};

////////////////////////////////////////////////////////////////////////////////
// Vertex Shader
////////////////////////////////////////////////////////////////////////////////
PixelInputType main(int aData : POSITION, float3 offset : POSITION1)
{
	PixelInputType output;

	float3 position = float3(float(aData & (63)), float((aData >> 6) & (63)), float((aData >> 12) & (63))) + offset;

	output.position = mul(float4(position, 1), model);
	output.pos = output.position;
	output.position = mul(output.position, viewProj);

	output.texID = int((aData >> 18) & (31));

	output.brightness = (float((aData >> 23) & (15)) + 2) / 8.0;

	int normal = int((aData >> 27) & (7));

	position += chunkOffset;

	if (normal < 2)
	{
		output.uv = position.xz * 1; // 1 == uvSize[output.texID]
		output.brightness *= normal == 0 ? 1.3 : 0.85;
	}
	else
	{
		output.uv = (normal < 4 ? position.zy : position.xy) * 1; // 1 == uvSize[output.texID]
	}

	[branch]
		switch (normal)
		{
		case 0:
			output.normal = float3(0, 1, 0);
			output.texID = ((descs[output.texID].packedY >> 8) & 0xff);
			break;
		case 1:
			output.normal = float3(0, -1, 0);
			output.texID = ((descs[output.texID].packedY) & 0xff);
			break;
		case 2:
			output.normal = float3(1, 0, 0);
			output.texID = ((descs[output.texID].packedX >> 8) & 0xff);
			break;
		case 3:
			output.normal = float3(-1, 0, 0);
			output.texID = ((descs[output.texID].packedX) & 0xff);
			break;
		case 4:
			output.normal = float3(0, 0, 1);
			output.texID = ((descs[output.texID].packedZ >> 8) & 0xff);
			break;
		case 5:
			output.normal = float3(0, 0, -1);
			output.texID = ((descs[output.texID].packedZ) & 0xff);
			break;
		default:
			output.normal = float3(0, 0, 0);
			break;
		}

	output.uv.y = 1 - output.uv.y;
	output.depth = output.position.z / output.position.w;

	output.normal = mul(output.normal, (float3x3)model);
	output.normal = normalize(output.normal);

	return output;
}