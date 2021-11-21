////////////////////////////////////////////////////////////////////////////////
// Filename: light.ps
////////////////////////////////////////////////////////////////////////////////

#define Vibrance 0.9  //Intelligently saturates (or desaturates if you use negative values) the pixels depending on their original saturation.
#define Vibrance_RGB_balance float3(1.00, 1.00, 1.00)  //[-10.00 to 10.00,-10.00 to 10.00,-10.00 to 10.00] A per channel multiplier to the Vibrance strength so you can give more boost to certain colors over others
#define Shininess 20.0;

struct DirectionalLight
{
	float4 Color;
	float3 LightDirection;
	float reserved;
	matrix view;
	matrix projection;
};

/////////////
// GLOBALS //
/////////////

Texture2DMS<float4> colorTexture : register(t0);
Texture2DMS<float4> positionTexture : register(t1);
Texture2DMS<float4> normalTexture : register(t2);
Texture2D depthMapTexture : register(t3);
//Texture2DMS<float4> depthTexture : register(t4);

///////////////////
// SAMPLE STATES //
///////////////////
SamplerState SampleTypePoint : register(s0);

//////////////////////
// CONSTANT BUFFERS //
//////////////////////

cbuffer LightBuffer : register(b0)
{
	DirectionalLight light;
};

//////////////
// TYPEDEFS //
//////////////
struct PixelInputType
{
	float4 position : SV_POSITION;
	float2 tex : TEXCOORD0;
};

struct GBufferAttributes {
	float4 color;
	float4 position;
	float4 normal;
	//float4 depth;
};

void ExtractGBufferAttributes(in PixelInputType pixel, in Texture2DMS<float4> color, in Texture2DMS<float4> pos, in Texture2DMS<float4> normal, in int sampleIndex,
	out GBufferAttributes attrs)
{
	//in Texture2DMS<float4> depth
	int3 screenPos = (int3)pixel.position;
	attrs.color = (float4)color.Load((int2)screenPos, sampleIndex);
	attrs.position = (float4)pos.Load((int2)screenPos, sampleIndex);
	attrs.normal = (float4)normal.Load((int2)screenPos, sampleIndex);
	//attrs.depth = (float4)depth.Load((int2)screenPos, sampleIndex);
}

float4 ComputeLighting(PixelInputType input, GBufferAttributes attrs)
{
	float4 image = attrs.color;
	float3 position = (float3)attrs.position;
	float3 normal = normalize((float3)attrs.normal);
	float4 color = float4(0.1, 0.1, 0.1, 0.1);

	float3 lightDir = -light.LightDirection;

	// Correct
	float4 projectedEyeDir = float4(position, 1);
	projectedEyeDir = mul(projectedEyeDir, light.view);
	projectedEyeDir = mul(projectedEyeDir, light.projection);
	float2 projectTexCoord;
	projectTexCoord.x = projectedEyeDir.x / projectedEyeDir.w / 2.0f + 0.5f;
	projectTexCoord.y = -projectedEyeDir.y / projectedEyeDir.w / 2.0f + 0.5f;

	if ((saturate(projectTexCoord.x) == projectTexCoord.x) && (saturate(projectTexCoord.y) == projectTexCoord.y))
	{
		const float bias = 0.0001;
		float depthValue = depthMapTexture.Sample(SampleTypePoint, projectTexCoord).r;
		float lightDepthValue = (projectedEyeDir.z / projectedEyeDir.w) - bias;

		if (lightDepthValue < depthValue)
		{
			float lightIntensity = saturate(dot(normal, lightDir));

			if (lightIntensity > 0.0f)
			{
				color += (light.Color * lightIntensity);
				color = saturate(color);
			}
		}
	}
	color = color * image;
	//color.xyz = projectedEyeDir.xyz;
	color.a = attrs.color.w;
	return color;
}

float4 ColorCorrect(float4 colorInput)
{
	float4 outputColor;
	float gamma = 1.1;

	outputColor.rgb = pow(abs(colorInput.rgb), float3(1.0 / gamma, 1.0 / gamma, 1.0 / gamma));
	outputColor.w = colorInput.w;

	float3 lumCoeff = float3(0.212656, 0.715158, 0.072186);
	float luma = dot(lumCoeff, outputColor.rgb);
#define Vibrance_coeff float3(Vibrance_RGB_balance * Vibrance)

	float max_color = max(outputColor.r, max(outputColor.g, outputColor.b));
	float min_color = min(outputColor.r, min(outputColor.g, outputColor.b));

	float color_saturation = max_color - min_color;

	outputColor.rgb = lerp(luma, outputColor.rgb, (1.0 + (Vibrance_coeff * (1.0 - (sign(Vibrance_coeff) * color_saturation)))));
	return outputColor;
}

////////////////////////////////////////////////////////////////////////////////
// Pixel Shader
////////////////////////////////////////////////////////////////////////////////
float4 LightPixelShader(PixelInputType pixel, uint coverage: SV_Coverage, uint sampleIndex : SV_SampleIndex) : SV_TARGET
{
	GBufferAttributes attrs;
	if (coverage & (1 << sampleIndex))
	{
		// ExtractGBufferAttributes(pixel, colorTexture, positionTexture, normalTexture, depthTexture, sampleIndex, attrs);

		ExtractGBufferAttributes(pixel, colorTexture, positionTexture, normalTexture, sampleIndex, attrs);
		float4 color = ComputeLighting(pixel, attrs);
		color = ColorCorrect(color);
		return color;
	}
	discard;
	return 0;
}