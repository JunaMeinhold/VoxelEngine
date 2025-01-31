struct PixelInputType
{
	float4 position : SV_POSITION;
	float3 normal : NORMAL;
	float depth : DEPTH;
	float4 pos : TEXCOORD0;
	int texID : TEXCOORD1;
	float2 uv : TEXCOORD2;
	float brightness : TEXCOORD3;
};

struct BlockDescription
{
	int packedX;
	int packedY;
	int packedZ;
	int padd;
};