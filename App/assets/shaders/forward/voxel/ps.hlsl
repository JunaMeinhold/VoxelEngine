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
#include "defs.hlsl"

Texture2DArray shaderTexture : register(t0);
Texture2D lightDepthTexture : register(t1);

SamplerState Sampler : register(s0);
SamplerState SamplerDepth : register(s1);

cbuffer CameraBuffer : register(b0)
{
    Camera camera;
};

cbuffer LightBuffer : register(b1)
{
    DirectionalLight light;
};

float ShadowCalculation(float4 projectedEyeDir, float bias)
{
    projectedEyeDir = mul(light.view, projectedEyeDir);
    projectedEyeDir = mul(light.proj, projectedEyeDir);

    float shadow = 0.0;
    float w;
    float h;
    lightDepthTexture.GetDimensions(w, h);
    float2 texelSize = 1.0 / float2(w, h);

    float currentDepth = projectedEyeDir.z / projectedEyeDir.w;

    if (currentDepth > 1)
        return 0;

    float2 projCoords;
    projCoords.x = projectedEyeDir.x / projectedEyeDir.w / 2.0f + 0.5f;
    projCoords.y = -projectedEyeDir.y / projectedEyeDir.w / 2.0f + 0.5f;

	[unroll]
    for (int x = -8; x <= 8; x++)
    {
		[unroll]
        for (int y = -8; y <= 8; y++)
        {
            float pcfDepth = lightDepthTexture.Sample(SamplerDepth, projCoords.xy + float2(x, y) * texelSize).r;
            shadow += currentDepth - bias > pcfDepth ? 1.0 : 0.0;
        }
    }
    shadow /= 289;
    return shadow;
}

float4 main(PixelInputType input) : SV_Target
{
    float3 position = (float3)input.pos;
	float4 albedo = float4(shaderTexture.Sample(Sampler, float3(input.uv, input.texID)).rgb * input.brightness, 1.0);

    // Ambient
    float4 ambient = 0.15 * light.color;

    // Diffuse
    float3 lightDir = normalize(-light.direction);
    float diff = max(dot(lightDir, input.normal), 0.0);
    float4 diffuse = diff * light.color;

    // Specular
    float3 viewDir = normalize(camera.position - position);
    float3 halfwayDir = normalize(lightDir + viewDir);
    float spec = pow(max(dot(input.normal, halfwayDir), 0.0), 1);
    float4 specular = spec * light.color;

    // Shadow
    float bias = max(0.05f * (1.0 - dot(input.normal, lightDir)), 0.005f);
    float shadow = ShadowCalculation(input.pos, bias);

    // Compute output
    float4 lighting = (ambient + ((1.0 - shadow) * (diffuse + specular)));
    float4 output = lighting * albedo;
 

    // Gamma correct
	float gamma = 1.25;
	output.rgb = pow(output.rgb, float3(1.0 / gamma, 1.0 / gamma, 1.0 / gamma));
    output.a = albedo.a;
	return output;
}