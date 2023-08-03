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

#include "../../../common.hlsl"
#include "defs.hlsl"

//////////////////////
// CONSTANT BUFFERS //
//////////////////////
cbuffer MatrixBuffer : register(b0)
{
    MVPBuffer mvp;
};

cbuffer WorldData : register(b1)
{
    float3 chunkOffset;
    float padd;
};

cbuffer TexData : register(b2)
{
    BlockDescription descs[256];
};

////////////////////////////////////////////////////////////////////////////////
// Vertex Shader
////////////////////////////////////////////////////////////////////////////////
PixelInputType main(int aData : POSITION, float3 offset : POSITION1)
{
    PixelInputType output;

    float3 position = float3(float(aData & (63)), float((aData >> 6) & (63)), float((aData >> 12) & (63))) + offset;

    output.position = mul(float4(position, 1), mvp.model);
    output.pos = output.position;
    output.position = mul(output.position, mvp.view);
    output.position = mul(output.position, mvp.proj);

    output.texID = int((aData >> 18) & (31));

    output.brightness = (float((aData >> 23) & (15)) + 2) / 8.0;

    int normal = int((aData >> 27) & (7));

    position += chunkOffset;

    if (normal < 2)
    {
        output.uv = position.xz * 1; // 1 == uvSize[output.texID]
        output.brightness *= normal == 0 ? 1.3 : 0.85;
    }
    else
    {
        output.uv = (normal < 4 ? position.zy : position.xy) * 1; // 1 == uvSize[output.texID]
    }

	[branch]
    switch (normal)
    {
        case 0:
            output.normal = float3(0, 1, 0);
            output.texID = ((descs[output.texID].packedY >> 8) & 0xff);
            break;
        case 1:
            output.normal = float3(0, -1, 0);
            output.texID = ((descs[output.texID].packedY) & 0xff);
            break;
        case 2:
            output.normal = float3(1, 0, 0);
            output.texID = ((descs[output.texID].packedX >> 8) & 0xff);
            break;
        case 3:
            output.normal = float3(-1, 0, 0);
            output.texID = ((descs[output.texID].packedX) & 0xff);
            break;
        case 4:
            output.normal = float3(0, 0, 1);
            output.texID = ((descs[output.texID].packedZ >> 8) & 0xff);
            break;
        case 5:
            output.normal = float3(0, 0, -1);
            output.texID = ((descs[output.texID].packedZ) & 0xff);
            break;
        default:
            output.normal = float3(0, 0, 0);
            break;
    }

    output.uv.y = 1 - output.uv.y;
    output.depth = output.position.z / output.position.w;

    output.normal = mul(output.normal, (float3x3) mvp.model);
    output.normal = normalize(output.normal);

    return output;
}