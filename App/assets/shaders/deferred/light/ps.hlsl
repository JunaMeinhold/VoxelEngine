 #include "../../common.hlsl"
#include "../../gbuffer.hlsl"
#include "../../lights.hlsl"
#include "../../camera.hlsl"
#include "defs.hlsl"

cbuffer directionalLightBuffer : register(b0)
{
    DirectionalLightSD directionalLight;
};

Texture2D<float4> albedoTexture : register(t0);
Texture2D<float4> positionTexture : register(t1);
Texture2D<float4> normalTexture : register(t2);
Texture2D<float4> specularTexture : register(t3);
Texture2D<float> depthTexture : register(t4);

Texture2DArray lightDepthMap : register(t5);

SamplerState samplerLinearClamp : register(s0);
SamplerState samplerDepth : register(s4);

float ShadowCalculation(DirectionalLightSD light, float3 fragPosWorldSpace, float3 normal, Texture2DArray depthTex, SamplerState state)
{
    float cascadePlaneDistances[16] = (float[16]) light.cascades;
    float farPlane = 100;

    float w;
    float h;
    uint cascadeCount;
    depthTex.GetDimensions(w, h, cascadeCount);

    // select cascade layer
    float4 fragPosViewSpace = mul(float4(fragPosWorldSpace, 1.0), view);
    float depthValue = abs(fragPosViewSpace.z);
    float cascadePlaneDistance;
    uint layer = cascadeCount;
    for (uint i = 0; i < cascadeCount; ++i)
    {
        if (depthValue < cascadePlaneDistances[i])
        {
            cascadePlaneDistance = cascadePlaneDistances[i];
            layer = i;
            break;
        }
    }

    float4 fragPosLightSpace = mul(float4(fragPosWorldSpace, 1.0), light.views[layer]);
    fragPosLightSpace.y = -fragPosLightSpace.y;
    float3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;
    float currentDepth = projCoords.z;
    projCoords = projCoords * 0.5 + 0.5;

    // calculate bias (based on depth map resolution and slope)
    normal = normalize(normal);
    float bias = max(0.005 * (1.0 - dot(normal, light.dir)), 0.0005);
    const float biasModifier = 0.5f;
    if (layer == cascadeCount)
    {
        bias *= 1 / (farPlane * biasModifier);
    }
    else
    {
        bias *= 1 / (cascadePlaneDistance * biasModifier);
    }

    // PCF
    float shadow = 0.0;
    float2 texelSize = 1.0 / float2(w, h);
    [unroll]
    for (int x = -1; x <= 1; ++x)
    {
        [unroll]
        for (int y = -1; y <= 1; ++y)
        {
            float pcfDepth = depthTex.Sample(state, float3(projCoords.xy + float2(x, y) * texelSize, layer)).r;
            shadow += (currentDepth - bias) > pcfDepth ? 1.0 : 0.0;
        }
    }

    shadow /= 9;

        // keep the shadow at 0.0 when outside the far_plane region of the light's frustum.
    if (currentDepth > 1.0)
    {
        shadow = 0.0;
    }

    return shadow;
}

float ShadowCalculationDebug(DirectionalLightSD light, float3 fragPosWorldSpace, Texture2DArray depthTex)
{
    float cascadePlaneDistances[16] = (float[16]) light.cascades;
    float farPlane = 100;

    float w;
    float h;
    uint cascadeCount;
    depthTex.GetDimensions(w, h, cascadeCount);

    // select cascade layer
    float4 fragPosViewSpace = mul(float4(fragPosWorldSpace, 1.0), view);
    float depthValue = abs(fragPosViewSpace.z);
    float cascadePlaneDistance;
    uint layer = cascadeCount;
    for (uint i = 0; i < cascadeCount; ++i)
    {
        if (depthValue < cascadePlaneDistances[i])
        {
            cascadePlaneDistance = cascadePlaneDistances[i];
            layer = i;
            break;
        }
    }

    return (float) layer / cascadeCount;
}

#define PI 3.14159265358979323846

float Attenuation(float distance, float range)
{
    float att = saturate(1.0f - (distance * distance / (range * range)));
    return att * att;
}

float3 BlinnPhong(float3 radiance, float3 L, float3 V, float3 N, float3 baseColor, float shininess)
{
    float NdotL = max(0, dot(N, L));
    float3 diffuse = radiance * NdotL;

    const float kEnergyConservation = (8.0 + shininess) / (8.0 * PI);
    float3 halfwayDir = normalize(L + V);
    float spec = kEnergyConservation * pow(max(dot(N, halfwayDir), 0.0), shininess);

    float3 specular = radiance * spec;

    return (diffuse + specular) * baseColor;
}

float3 ComputeDirectionalLight(GeometryAttributes attrs, float3 position, float3 V, DirectionalLightSD light)
{
    float3 radiance = (float3) light.color;
    float3 L = normalize(-light.dir);
    float3 N = normalize(attrs.normal);

    float shadow = 0;

    if (light.castsShadows)
    {
        // Shadow
        float bias = max(0.05f * (1.0 - dot(N, L)), 0.005f);
        shadow = ShadowCalculation(light, position, N, lightDepthMap, samplerDepth);
    }

    return (1.0 - shadow) * BlinnPhong(radiance, L, V, N, attrs.albedo, 32);
}

float3 ComputePointLight(GeometryAttributes attrs, float3 position, float3 V, PointLight light)
{
    float3 N = attrs.normal;
    float3 LN = light.position.xyz - position;
    float distance = length(LN);
    float3 L = normalize(LN);

    float attenuation = Attenuation(distance, light.range);
    float3 radiance = light.color.rgb * attenuation;
    return BlinnPhong(radiance, L, V, N, attrs.albedo, 32);
}

float3 Tonemap_ACES(const float3 x)
{
    // Narkowicz 2015, "ACES Filmic Tone Mapping Curve"
    const float a = 2.51;
    const float b = 0.03;
    const float c = 2.43;
    const float d = 0.59;
    const float e = 0.14;
    return (x * (a * x + b)) / (x * (c * x + d) + e);
}

float3 ReinhardTonemapping(float3 hdrColor, float exposure)
{
    return hdrColor / (hdrColor + 1.0f) * exposure;
}

float3 OECF_sRGBFast(float3 color)
{
    float gamma = 2.0;
    return pow(color.rgb, float3(1.0 / gamma, 1.0 / gamma, 1.0 / gamma));
}

float4 main(PixelInput input) : SV_TARGET
{
    GeometryAttributes attrs;
    ExtractGeometryData(input.tex, albedoTexture, positionTexture, normalTexture, specularTexture, samplerLinearClamp, attrs);
    float depth = depthTexture.SampleLevel(samplerLinearClamp, input.tex, 0);
    float3 position = GetPositionWS(input.tex, depth);
    float3 V = normalize(GetCameraPos() - position);

    float3 color = ComputeDirectionalLight(attrs, position, V, directionalLight) * attrs.albedo;

    color += attrs.albedo * 0.2f;

    return float4(color, 1);
}