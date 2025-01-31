#ifndef LIGHT_H_INCLUDED
#define LIGHT_H_INCLUDED

#define POINT_LIGHT 0
#define SPOT_LIGHT 1
#define DIRECTIONAL_LIGHT 2

struct DirectionalLightSD
{
	float4x4 views[8];
	float4 cascades[2];
	float4 color;
	float3 dir;
	bool castsShadows;
	uint cascadeCount;
};

struct PointLight
{
	float4 color;
	float3 position;
	float range;
};

#endif