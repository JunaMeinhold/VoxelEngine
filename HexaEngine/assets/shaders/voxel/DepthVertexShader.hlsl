cbuffer MatrixBuffer : register(b0)
{
	matrix mvp;
};

//////////////
// TYPEDEFS //
//////////////
struct PixelInputType
{
	float4 position : SV_POSITION;
	float depth : DEPTH;
};

////////////////////////////////////////////////////////////////////////////////
// Vertex Shader
////////////////////////////////////////////////////////////////////////////////
PixelInputType main(int aData : POSITION)
{
	PixelInputType output;

	float3 position = float3(float(aData & (63)), float((aData >> 6) & (63)), float((aData >> 12) & (63)));

	output.position = mul(float4(position, 1), mvp);

	output.depth = output.position.z / output.position.w;
	return output;
}