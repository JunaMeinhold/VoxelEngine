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

cbuffer CamBuffer : register(b1)
{
	float3 viewPos;
	float reserved;
	matrix viewMatrix;
	matrix projectionMatrix;
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

float ShadowCalculation(float4 projectedEyeDir, float bias)
{
	float currentDepth = projectedEyeDir.z / projectedEyeDir.w;
	float2 projCoords;
	projCoords.x = projectedEyeDir.x / projectedEyeDir.w / 2.0f + 0.5f;
	projCoords.y = -projectedEyeDir.y / projectedEyeDir.w / 2.0f + 0.5f;
	float closestDepth = depthMapTexture.Sample(SampleTypePoint, projCoords).r;

	float shadow = currentDepth - bias > closestDepth ? 1.0 : 0.0;
	if (currentDepth > 1.0)
		shadow = 0.0;
	return shadow;
}

float4 ComputeLighting(PixelInputType input, GBufferAttributes attrs)
{
	float4 color = attrs.color;
	float3 position = (float3)attrs.position;
	float3 normal = normalize((float3)attrs.normal);
	float4 lightColor = float4(1.0, 1.0, 1.0, 1.0);
	float4 positionViewSpace = float4(position, 1);
	positionViewSpace = mul(positionViewSpace, viewMatrix);
	positionViewSpace = mul(positionViewSpace, projectionMatrix);

	// ambient
	float4 ambient = 0.15 * lightColor;

	// diffuse
	float3 lightDir = normalize(-light.LightDirection);
	float diff = max(dot(lightDir, normal), 0.0);
	float4 diffuse = diff * lightColor;

	// specular
	float3 viewDir = normalize(viewPos - (float3)positionViewSpace);
	float spec = 0.0;
	float3 halfwayDir = normalize(lightDir + viewDir);
	spec = pow(max(dot(normal, halfwayDir), 0.0), 64.0);
	float4 specular = spec * lightColor;

	// project
	float4 positionLightSpace = float4(position, 1);
	positionLightSpace = mul(positionLightSpace, light.view);
	positionLightSpace = mul(positionLightSpace, light.projection);

	// calculate shadow
	//float bias = max(0.05 * (1.0 - dot(normal, lightDir)), 0.005);
	float shadow = ShadowCalculation(positionLightSpace, 0.005f);
	float4 lighting = (ambient + ((1.0 - shadow) * (diffuse + specular))) * color;
	lighting.a = attrs.color.w;
	return lighting;
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