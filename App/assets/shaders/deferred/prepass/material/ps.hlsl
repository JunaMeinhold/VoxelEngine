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

#include "defs.hlsl"
#include "../../common.hlsl"
#include "../../material.hlsl"

Texture2D ambientTexture : register(t0);
Texture2D diffuseTexture : register(t1);
Texture2D specularTexture : register(t2);
Texture2D specularHighlightTexture : register(t3);
Texture2D bumpTexture : register(t4);
Texture2D displacmentTexture : register(t5);
Texture2D stencilDecalTexture : register(t6);
Texture2D alphaTexture : register(t7);
Texture2D metallicTexture : register(t8);
Texture2D roughnessTexture : register(t9);

cbuffer MaterialBuffer : register(b0)
{
	Material material;
};

SamplerState SampleTypeWrap : register(s0);

float3 NormalSampleToWorldSpace(float3 normalMapSample, float3 unitNormalW, float3 tangentW)
{
	// Uncompress each component from [0,1] to [-1,1].
	float3 normalT = 2.0f * normalMapSample - 1.0f;

	// Build orthonormal basis.
	float3 N = unitNormalW;
	float3 T = normalize(tangentW - dot(tangentW, N) * N);
	float3 B = cross(N, T);

	float3x3 TBN = float3x3(T, B, N);

	// Transform from tangent space to world space.
	float3 bumpedNormalW = mul(normalT, TBN);

	return bumpedNormalW;
}

GeometryData main(PixelInput input)
{
    float3 albedo;
    float4 pos = input.pos;
    float3 normal;
    float3 specular;
    float alpha;
    float metallic;
    float specCoeff;
    float roughness;

	if (material.HasDiffuseTextureMap)
	{
        albedo = diffuseTexture.Sample(SampleTypeWrap, (float2) input.tex).rgb;
    }
	else
	{
        albedo = float3(1, 1, 1);
    }

	if (material.HasAlphaTextureMap)
	{
        alpha = alphaTexture.Sample(SampleTypeWrap, (float2) input.tex).r;
    }
	else
	{
        alpha = material.Transparency;
    }

	if (material.HasBumpMap)
	{
		float4 bumpMap = bumpTexture.Sample(SampleTypeWrap, (float2)input.tex);
        normal = NormalSampleToWorldSpace((float3) bumpMap, input.normal, input.tangent);
    }
	else
	{
        normal = input.normal;
        normal = (float3)normalize(normal);
    }

	if (material.HasSpecularTextureMap)
	{
		specular = specularTexture.Sample(SampleTypeWrap, (float2)input.tex);
        specCoeff = material.SpecularCoefficient;
    }
	else
	{
        specular = material.SpecularColor;
        specCoeff = material.SpecularCoefficient;
	}

	if (material.HasMetallicMap)
	{
        metallic = metallicTexture.Sample(SampleTypeWrap, (float2) input.tex).r;
    }
    else
    {
        metallic = 0;
    }

	if (material.HasRoughnessMap)
	{
        roughness = roughnessTexture.Sample(SampleTypeWrap, (float2) input.tex).r;
    }
	else
	{
        roughness = 0.5f;
    }

    return PackGeometryData(albedo, pos, normal, specular, alpha, metallic, specCoeff, roughness);
}