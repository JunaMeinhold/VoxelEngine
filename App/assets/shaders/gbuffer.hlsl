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

struct GeometryAttributes
{
    float3 albedo;
    float3 pos;
    float3 normal;
    float3 specular;
    float alpha;
    float metallic;
    float specCoeff;
    float roughness;
};

void ExtractGeometryData(
	in int3 screenPos,
	in Texture2D<float4> color,
	in Texture2D<float4> pos,
	in Texture2D<float4> normal,
    in Texture2D<float4> specular,
	out GeometryAttributes attrs)
{
    float4 a = (float4) color.Load((int3) screenPos);
    float4 b = (float4) pos.Load((int3) screenPos);
    float4 c = (float4) normal.Load((int3) screenPos);
    float4 d = (float4) specular.Load((int3) screenPos);

    attrs.albedo = a.rgb;
    attrs.alpha = a.a;
    attrs.pos.rgb = b.xyz;
    attrs.metallic = b.w;
    attrs.normal = c.xyz;
    attrs.specCoeff = c.w;
    attrs.specular = d.rgb;
    attrs.roughness = d.w;
}

struct GeometryData
{
    float4 albedo : SV_TARGET0;
    float4 position : SV_TARGET1;
    float4 normal : SV_TARGET2;
    float4 specular : SV_TARGET3;
};

/*
albedo	    = vector3 (3*float32)
pos		    = vector3 (3*float32)
normal	    = vector3 (3*float32)
specular	= vector3 (3*float32)
alpha		= float32
metallic	= float32
specCoeff	= float32
roughness	= float32
Tex.	float32	    float32	    float32	    float32
0:	    albedo 	    albedo 	    albedo 	    alpha
1:	    pos		    pos 		pos		    metallic
2:	    normal 	    normal 	    normal 	    specCoeff
3:	    specular	specular	specular	roughness
*/

GeometryData PackGeometryData(
in float3 albedo,
in float4 pos,
in float3 normal,
in float3 specular,
in float alpha,
in float metallic,
in float specCoeff,
in float roughness)
{
    GeometryData data;
    data.albedo.rgb = albedo;
    data.albedo.a = alpha;
    data.position.xyz = pos.rgb;
    data.position.w = metallic;
    data.normal.xyz = normal;
    data.normal.w = specCoeff;
    data.specular.rgb = specular;
    data.specular.w = roughness;
    return data;
}