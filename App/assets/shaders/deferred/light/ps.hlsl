#include "../../gbuffer.hlsl"
#include "../../lights.hlsl"
#include "../../camera.hlsl"
#include "../../shadows.hlsl"

struct PixelInput
{
	float4 pos : SV_Position;
	float2 tex : TEXCOORD;
};

StructuredBuffer<Light> LightBuffer;
StructuredBuffer<ShadowData> ShadowDataBuffer;

cbuffer LightParams
{
	uint lightCount;
	float3 ambient;
};

Texture2D<float4> GBufferA;
Texture2D<float4> GBufferB;
Texture2D<float4> GBufferC;
Texture2D<float4> GBufferD;
Texture2D<float> DepthTex;

Texture2DArray CSMDepthBuffer;
Texture2D<float> AOBuffer;

SamplerState linearClampSampler;

#define PI 3.14159265358979323846

float Attenuation(float distance, float range)
{
	float att = saturate(1.0f - (distance * distance / (range * range)));
	return att * att;
}

float3 BlinnPhong(float3 radiance, float NdotL, float3 L, float3 V, float3 N, float3 baseColor, float shininess)
{
	NdotL = max(0, NdotL);
	float3 diffuse = radiance * NdotL;

	const float kEnergyConservation = (8.0 + shininess) / (8.0 * PI);
	float3 halfwayDir = normalize(L + V);
	float spec = kEnergyConservation * pow(max(dot(N, halfwayDir), 0.0), shininess);

	float3 specular = radiance * spec;

	return (diffuse + specular) * baseColor;
}

float3 ComputeDirectionalLight(GeometryAttributes attrs, float3 position, float3 V, Light light)
{
	float3 radiance = light.color.rgb;
	float3 L = normalize(-light.direction);
	float3 N = normalize(attrs.normal);
	float NdotL = dot(N, L);

	float visibility = 1;

	if (light.castsShadows)
	{
		ShadowData data = ShadowDataBuffer[light.shadowMapIndex];
		//visibility = ShadowFactorDirectionalLightCascaded(CSMDepthBuffer, linearClampSampler, data, position + GetCameraPos(), NdotL);
	}

	return BlinnPhong(radiance, NdotL, L, V, N, attrs.albedo, 32) * visibility;
}

float3 ComputePointLight(GeometryAttributes attrs, float3 position, float3 V, Light light)
{
	float3 N = attrs.normal;
	float3 LN = light.position.xyz - position;
	float distance = length(LN);
	float3 L = normalize(LN);
	float NdotL = dot(N, L);

	float attenuation = Attenuation(distance, light.range);
	float3 radiance = light.color.rgb * attenuation;
	return BlinnPhong(radiance, NdotL, L, V, N, attrs.albedo, 16);
}

float4 main(PixelInput input) : SV_TARGET
{
	GeometryAttributes attrs;
	ExtractGeometryData(input.tex, GBufferA, GBufferB, GBufferC, GBufferD, linearClampSampler, attrs);
	float depth = DepthTex.SampleLevel(linearClampSampler, input.tex, 0);
	float3 position = GetPositionRWS(input.tex, depth);
	float3 V = normalize(-position);

	float ao = AOBuffer.SampleLevel(linearClampSampler, input.tex, 0);

	float3 lo = 0;
	[loop]
	for (uint i = 0; i < lightCount; i++)
	{
		Light light = LightBuffer[i];
		[branch]
			switch (light.type)
			{
			case POINT_LIGHT:
				lo += ComputePointLight(attrs, position, V, light);
				break;
			case DIRECTIONAL_LIGHT:
				lo += ComputeDirectionalLight(attrs, position, V, light);
				break;
			}
	}

	float3 color = attrs.albedo * lo;

	color += attrs.albedo * (ambient * ao);

	return float4(color, 1);
}