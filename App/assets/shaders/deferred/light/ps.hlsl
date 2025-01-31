#include "../../gbuffer.hlsl"
#include "../../lights.hlsl"
#include "../../camera.hlsl"
#include "../../shadows.hlsl"

struct PixelInput
{
	float4 pos : SV_Position;
	float2 tex : TEXCOORD;
};

cbuffer directionalLightBuffer : register(b0)
{
	DirectionalLightSD directionalLight;
};

Texture2D<float4> GBufferA : register(t0);
Texture2D<float4> GBufferB : register(t1);
Texture2D<float4> GBufferC : register(t2);
Texture2D<float4> GBufferD : register(t3);
Texture2D<float> DepthTex : register(t4);

Texture2DArray lightDepthMap : register(t5);
Texture2D<float> aoTexture : register(t6);

SamplerState linearClampSampler : register(s0);

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

float3 ComputeDirectionalLight(GeometryAttributes attrs, float3 position, float3 V, DirectionalLightSD light)
{
	float3 radiance = (float3)light.color;
	float3 L = normalize(-light.dir);
	float3 N = normalize(attrs.normal);
	float NdotL = dot(N, L);

	float visibility = 1;

	if (light.castsShadows)
	{
		visibility = ShadowFactorDirectionalLightCascaded(lightDepthMap, linearClampSampler, light, position, NdotL);
	}

	return BlinnPhong(radiance, NdotL, L, V, N, attrs.albedo, 16.0f) * visibility;
}

float3 ComputePointLight(GeometryAttributes attrs, float3 position, float3 V, PointLight light)
{
	float3 N = attrs.normal;
	float3 LN = light.position.xyz - position;
	float distance = length(LN);
	float3 L = normalize(LN);
	float NdotL = dot(N, L);

	float attenuation = Attenuation(distance, light.range);
	float3 radiance = light.color.rgb * attenuation;
	return BlinnPhong(radiance, NdotL, L, V, N, attrs.albedo, 32);
}

float4 main(PixelInput input) : SV_TARGET
{
	GeometryAttributes attrs;
	ExtractGeometryData(input.tex, GBufferA, GBufferB, GBufferC, GBufferD, linearClampSampler, attrs);
	float depth = DepthTex.SampleLevel(linearClampSampler, input.tex, 0);
	float3 position = GetPositionWS(input.tex, depth);
	float3 V = normalize(GetCameraPos() - position);

	float ao = aoTexture.SampleLevel(linearClampSampler, input.tex, 0);

	float3 color = ComputeDirectionalLight(attrs, position, V, directionalLight) * attrs.albedo;

	color += attrs.albedo * 0.5f * ao;

	return float4(color, 1);
}