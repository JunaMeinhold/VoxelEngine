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

cbuffer MatrixBuffer : register(b0)
{
    MVPBuffer mvp;
};

cbuffer WorldData : register(b1)
{
	float3 chunkOffset;
	float padd;
};

PixelInputType main(int aData : POSITION)
{
	PixelInputType output;

	float3 position = float3(float(aData & (63)), float((aData >> 6) & (63)), float((aData >> 12) & (63)));

    output.position = mul(float4(position, 1), mvp.model);
    output.position = mul(output.position, mvp.view);
    output.position = mul(output.position, mvp.proj);

	return output;
}