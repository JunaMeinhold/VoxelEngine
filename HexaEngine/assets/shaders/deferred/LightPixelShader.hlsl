////////////////////////////////////////////////////////////////////////////////
// Filename: light.ps
////////////////////////////////////////////////////////////////////////////////

#define Vibrance 0.9  //Intelligently saturates (or desaturates if you use negative values) the pixels depending on their original saturation.
#define Vibrance_RGB_balance float3(1.00, 1.00, 1.00)  //[-10.00 to 10.00,-10.00 to 10.00,-10.00 to 10.00] A per channel multiplier to the Vibrance strength so you can give more boost to certain colors over others
#define Shininess 20.0;

struct Fog {
	float fogStart;
	float fogEnd;
};

struct DirectionalLight
{
	float3 LightDirection;
	float3 Position;
	float4 Ambient;
};

/////////////
// GLOBALS //
/////////////

Texture2DMS<float4> colorTexture : register(t0);
Texture2DMS<float4> positionTexture : register(t1);
Texture2DMS<float4> normalTexture : register(t2);
Texture2DMS<float4> depthTexture : register(t3);
Texture2D depthLightTexture : register(t4);

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
	Fog fog;
	float padding;
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
	float4 depth;
	float4 lightDepth;
};

void ExtractGBufferAttributes(in PixelInputType pixel, in Texture2DMS<float4> color, in Texture2DMS<float4> pos, in Texture2DMS<float4> normal, in Texture2DMS<float4> depth, in int sampleIndex,
	out GBufferAttributes attrs)
{
	int3 screenPos = (int3)pixel.position;
	attrs.color = (float4)color.Load((int2)screenPos, sampleIndex);
	attrs.position = (float4)pos.Load((int2)screenPos, sampleIndex);
	attrs.normal = (float4)normal.Load((int2)screenPos, sampleIndex);
	attrs.depth = (float4)depth.Load((int2)screenPos, sampleIndex);
	attrs.lightDepth = depthLightTexture.Sample(SampleTypePoint, pixel.tex);
}

float3 phongBRDF(float3 lightDir, float3 viewDir, float3 normal, float3 phongDiffuseCol, float3 phongSpecularCol, float phongShininess)
{
	float3 color = phongDiffuseCol;
	float3 reflectDir = reflect(-lightDir, normal);
	float specDot = max(dot(reflectDir, viewDir), 0.0);
	color += pow(specDot, phongShininess) * phongSpecularCol;
	return color;
}

float4 ComputeLighting(PixelInputType input, GBufferAttributes attrs)
{
	float shininess = Shininess;
	float4 color;
	float3 specularColor = float3(0.5f, 0.5f, 0.5f);

	float3 lightDir = normalize(-light.LightDirection);
	float3 viewDir = (float3)normalize(-attrs.position);
	float3 n = (float3)normalize(attrs.normal);

	float3 luminance = float3(0, 0, 0);

	float illuminance = dot(lightDir, n);
	if (illuminance > 0.0) {
		float3 brdf = phongBRDF(lightDir, viewDir, n, attrs.color.rgb, specularColor.rgb, shininess);
		luminance += brdf * illuminance * float3(2, 2, 2);
	}

	color.rgb = luminance;
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
	ExtractGBufferAttributes(pixel, colorTexture, positionTexture, normalTexture, depthTexture, sampleIndex, attrs);
	float4 color = ComputeLighting(pixel, attrs);
	color = ColorCorrect(color);
	return color;
	}
	discard;
	return 0;
}