struct MVPBuffer
{
	float4x4 proj;
	float4x4 view;
	float4x4 model;
};

struct VPBuffer
{
	float4x4 proj;
	float4x4 view;
};

struct DirectionalLight
{
	float3 direction;
	float reserved;
	float4 color;
	float4x4 view;
	float4x4 proj;
};