//////////////
// TYPEDEFS //
//////////////

struct PixelInputType
{
	float4 position : SV_POSITION;
	float4 depthPosition : TEXTURE0;
};

struct PixelOutputType
{
	float4 color : SV_TARGET;
};

////////////////////////////////////////////////////////////////////////////////
// Pixel Shader
////////////////////////////////////////////////////////////////////////////////
PixelOutputType main(PixelInputType input)
{
	PixelOutputType output;

	// Get the depth value of the pixel by dividing the Z pixel depth by the homogeneous W coordinate.
	float depth = input.position.z / input.position.w;
	output.color = float4(depth, depth, depth, 1.0f);
	return output;
}