struct DirectionalLightSD
{
    float4x4 views[8];
    float4 cascades[2];
    float4 color;
    float3 dir;
    bool castsShadows;
};

struct PointLight
{
    float4 color;
    float3 position;
    float range;
};