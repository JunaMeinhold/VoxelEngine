struct DirectionalLightSD
{
    matrix views[16];
    float4 cascades[4];
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