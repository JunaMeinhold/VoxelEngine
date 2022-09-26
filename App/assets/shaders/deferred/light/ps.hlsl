// Copyright (c) 2022 Juna Meinhold
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software,
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

#include "../../common.hlsl"
#include "../../gbuffer.hlsl"
#include "../../lights.hlsl"
#include "defs.hlsl"

cbuffer cameraData : register(b0)
{
    Camera camera;
};

cbuffer directionalLightBuffer : register(b1)
{
    DirectionalLightSD directionalLight;
};

Texture2D<float4> albedoTexture : register(t0);
Texture2D<float4> positionTexture : register(t1);
Texture2D<float4> normalTexture : register(t2);
Texture2D<float4> specularTexture : register(t3);

Texture2DArray lightDepthMap : register(t4);
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
    float4 fragPosViewSpace = mul(float4(fragPosWorldSpace, 1.0), camera.view);
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
    float4 fragPosViewSpace = mul(float4(fragPosWorldSpace, 1.0), camera.view);
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

    return (float)layer / cascadeCount;
}


float3 ComputeDirectionalLight(GeometryAttributes attrs, DirectionalLightSD light)
{
    float3 color = (float3)light.color;
    float3 position = (float3)attrs.pos;
    float3 normal = normalize(attrs.normal);;

    // Ambient
    float3 ambient = 0.4f * color;

    // Diffuse
    float3 lightDir = normalize(-light.dir);
    float diff = max(dot(lightDir, normal), 0.0);
    float3 diffuse = diff * color;

    // Specular
    float3 viewDir = normalize(camera.position - position);
    float3 halfwayDir = normalize(lightDir + viewDir);
    float spec = pow(max(dot(normal, halfwayDir), 0.0), 32);
    float3 specular = spec * color * 0.5f;

    // Shadow
    float bias = max(0.05f * (1.0 - dot(normal, lightDir)), 0.005f);
    float shadow = ShadowCalculation(light, position, normal, lightDepthMap, samplerDepth);
    
    float debug = ShadowCalculationDebug(light, position, lightDepthMap);
    
    //return float3(debug, shadow, 0);

    return ambient + (1.0 - shadow) * (diffuse + specular);
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

float3 OECF_sRGBFast(float3 color)
{
    float gamma = 2.2;
    return pow(color.rgb, float3(1.0 / gamma, 1.0 / gamma, 1.0 / gamma));
}

float4 main(PixelInput input) : SV_TARGET
{
    GeometryAttributes attrs;
    ExtractGeometryData((int3)input.pos, albedoTexture, positionTexture, normalTexture, specularTexture, attrs);

    float3 color = ComputeDirectionalLight(attrs, directionalLight) * attrs.albedo;

    color = OECF_sRGBFast(color);
    color = Tonemap_ACES(color);

    return float4(color, dot(color.rgb, float3(0.299, 0.587, 0.114)));
}
