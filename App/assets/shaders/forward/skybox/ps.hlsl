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

TextureCube dayMap : register(t0);
TextureCube nightMap : register(t1);

SamplerState SkyMapSampler : register(s0);

cbuffer time
{
    float timeOfDay;
};

cbuffer colors
{
    float4 nightColor = float4(0.0, 0.0, 0.2, 1.0); // Dark blue color at night
    float4 dawnColor = float4(0.5, 0.4, 0.5, 1.0); // Orange color at dawn
    float4 dayColor = float4(0.7, 0.8, 1.0, 1.0); // Sky blue color during the day
    float4 duskColor = float4(0.5, 0.4, 0.5, 1.0); // Orange color at dusk
    float4 nightHorizonColor = float4(0.07, 0.07, 0.3, 1); // Dunkle Horizontfarbe in der Nacht
    float4 dawnHorizonColor = float4(0.4, 0.2, 0.0, 1.0); // Orange color at dawn
    float4 dayHorizonColor = float4(0.4, 0.7, 0.9, 1); // Helle Horizontfarbe am Tag
    float4 duskHorizonColor = float4(0.9, 0.6, 0.4, 1.0); // Orange color at dusk
};

float4 main(PixelInputType input) : SV_Target
{
    float nightIntensity = saturate(1.0 - abs(timeOfDay) * 4.0); // Fade in night around 0.0 and 0.75
    float dawnIntensity = saturate(1.0 - abs(timeOfDay - 0.25) * 4.0); // Fade in dawn around 0.25
    float dayIntensity = saturate(1.0 - abs(timeOfDay - 0.5) * 4.0); // Fade in day around 0.5
    float duskIntensity = saturate(1.0 - abs(timeOfDay - 0.75) * 4.0); // Fade in dusk around 0.75

    float4 finalSkyColor = nightColor * nightIntensity +
                        dawnColor * dawnIntensity +
                        dayColor * dayIntensity +
                        duskColor * duskIntensity;

    float4 finalHorizonColor = nightHorizonColor * nightIntensity +
                        dawnHorizonColor * dawnIntensity +
                        dayHorizonColor * dayIntensity +
                        duskHorizonColor * duskIntensity;

    float3 daySkyColor = float3(0.7, 0.8, 1.0); // Helle Himmelsfarbe am Tag
    float3 nightSkyColor = float3(0.1, 0.1, 0.2); // Dunkle Himmelsfarbe in der Nacht

    // Mix the final color with the horizon color based on the vertical position
    float horizonBlend = saturate(input.pos.y); // Assume y ranges from -1 to 1
    float3 finalColor = lerp(finalHorizonColor, finalSkyColor, horizonBlend);

    return float4(finalColor, 1);

    //return float4(dayMap.Sample(SkyMapSampler, input.tex).rgb, 1);
}