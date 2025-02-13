#include "../../camera.hlsl"
#include "defs.hlsl"

cbuffer WorldData
{
	float3 chunkOffset;
	float padd;
};

cbuffer TexData
{
	BlockDescription descs[256];
};

PixelInputType main(float3 position : POSITION, int aData : POSITION1, float4 color : COLOR)
{
	PixelInputType output;

	float3 relativePos = position + chunkOffset;

	output.pos = float4(relativePos, 1);
	output.position = mul(float4(relativePos, 1), relViewProj);

	output.texID = int((aData >> 18) & (31));

	output.color = color;

	//output.brightness = (float((aData >> 23) & (15)) + 2) / 8.0;

	int normal = int((aData >> 27) & (7));

	if (normal < 2)
	{
		output.uv = position.xz * 1; // 1 == uvSize[output.texID]
		//output.brightness *= normal == 0 ? 1.3 : 0.85;
	}
	else
	{
		output.uv = (normal < 4 ? position.zy : position.xy) * 1; // 1 == uvSize[output.texID]
		// If X- or X+
		//if (normal < 4)
		//	output.brightness *= 1.15;
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

	output.normal = normalize(output.normal);

	return output;
}