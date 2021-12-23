////////////////////////////////////////////////////////////////////////////////
// Filename: deferred.vs
////////////////////////////////////////////////////////////////////////////////

//////////////////////
// CONSTANT BUFFERS //
//////////////////////
cbuffer MatrixBuffer
{
	matrix mvp;
};

cbuffer WorldData
{
	float3 chunkOffset;
};

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

////////////////////////////////////////////////////////////////////////////////
// Vertex Shader
////////////////////////////////////////////////////////////////////////////////
PixelInputType VoxelVertexShader(int aData : POSITION)
{
	PixelInputType output;

	float3 position = float3(float(aData & (63)), float((aData >> 6) & (63)), float((aData >> 12) & (63)));

	output.position = mul(float4(position, 1), mvp);

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
		output.uv = (normal < 4 ? position.zy : position.xy) * 1;  // 1 == uvSize[output.texID]

		// If X- or X+
		if (normal < 4)
			output.brightness *= 1.15;
	}

	[branch] switch (normal) {
	case 0:
		output.normal = float3(0, 1, 0);
		break;
	case 1:
		output.normal = float3(0, -1, 0);
		break;
	case 2:
		output.normal = float3(1, 0, 0);
		break;
	case 3:
		output.normal = float3(-1, 0, 0);
		break;
	case 4:
		output.normal = float3(0, 0, 1);
		break;
	case 5:
		output.normal = float3(0, 0, -1);
		break;
	default:
		output.normal = float3(0, 0, 0);
		break;
	}

	output.depth = output.position.z / output.position.w;
	return output;
}