#ifndef PLANET_ALL_CGINC
// Upgrade NOTE: excluded shader from OpenGL ES 2.0 because it uses non-square matrices
#pragma exclude_renderers gles
#define PLANET_ALL_CGINC






#include "planet.common.cginc"



float2 getUv(RWTexture2D<float4> map, int2 id)
{
	float w, h;
	map.GetDimensions(w, h);
	return id / float2(w - 1, h - 1);
}
float2 getUv(RWTexture2D<float3> map, int2 id)
{
	float w, h;
	map.GetDimensions(w, h);
	return id / float2(w - 1, h - 1);
}
float2 getUv(RWTexture2D<float2> map, int2 id)
{
	float w, h;
	map.GetDimensions(w, h);
	return id / float2(w - 1, h - 1);
}
float2 getUv(RWTexture2D<float> map, int2 id)
{
	float w, h;
	map.GetDimensions(w, h);
	return id / float2(w - 1, h - 1);
}







float2 getUv(RWTexture2D<double2> map, int2 id)
{
	float w, h;
	map.GetDimensions(w, h);
	return id / float2(w - 1, h - 1);
}
float2 getUv(RWTexture2D<double> map, int2 id)
{
	float w, h;
	map.GetDimensions(w, h);
	return id / float2(w - 1, h - 1);
}







float2 getUv(Texture2D<float4> map, int2 id)
{
	float w, h;
	map.GetDimensions(w, h);
	return id / float2(w - 1, h - 1);
}
float2 getUv(Texture2D<float3> map, int2 id)
{
	float w, h;
	map.GetDimensions(w, h);
	return id / float2(w - 1, h - 1);
}
float2 getUv(Texture2D<float2> map, int2 id)
{
	float w, h;
	map.GetDimensions(w, h);
	return id / float2(w - 1, h - 1);
}
float2 getUv(Texture2D<float> map, int2 id)
{
	float w, h;
	map.GetDimensions(w, h);
	return id / float2(w - 1, h - 1);
}







float2 getUv(Texture2D<double2> map, int2 id)
{
	float w, h;
	map.GetDimensions(w, h);
	return id / float2(w - 1, h - 1);
}
float2 getUv(Texture2D<double> map, int2 id)
{
	float w, h;
	map.GetDimensions(w, h);
	return id / float2(w - 1, h - 1);
}




float2 sampleCubicFloat2(Texture2D<float2> map, float2 uv)
{
	int w, h;
	map.GetDimensions(w, h);
	float2 xy = uv * float2(w - 1, h - 1);

	// p03--p13-------p23--p33
	//  |    |         |    |
	// p02--p12-------p22--p32     1
	//  |    |         |    |     ...
	//  |   t.y  xy    |    |     t.y
	//  |    |         |    |     ...
	// p01--p11--t.x--p21--p31     0...tx...1
	//  |    |         |    |
	// p00--p10-------p20--p30

	float2 xyFloored = floor(xy);
	float2 t = xy - xyFloored;
	float4 tx = cubic(t.x);
	float4 ty = cubic(t.y);

	int2 p12 = int2(xyFloored);
	int2 p00 = p12 - int2(1, 2);

	float4x2 v0 = float4x2(
		map[p00 + int2(0, 0)],
		map[p00 + int2(1, 0)],
		map[p00 + int2(2, 0)],
		map[p00 + int2(3, 0)]
		);

	float4x2 v1 = float4x2(
		map[p00 + int2(0, 1)],
		map[p00 + int2(1, 1)],
		map[p00 + int2(2, 1)],
		map[p00 + int2(3, 1)]
		);

	float4x2 v2 = float4x2(
		map[p00 + int2(0, 2)],
		map[p00 + int2(1, 2)],
		map[p00 + int2(2, 2)],
		map[p00 + int2(3, 2)]
		);

	float4x2 v3 = float4x2(
		map[p00 + int2(0, 3)],
		map[p00 + int2(1, 3)],
		map[p00 + int2(2, 3)],
		map[p00 + int2(3, 3)]
		);

	// first interpolate on X
	float4x2 c = float4x2(
		mul(tx, v0),
		mul(tx, v1),
		mul(tx, v2),
		mul(tx, v3)
		);

	// then on Y
	float2 f = mul(ty, c);

	return f;
}



float sampleCubicFloat(Texture2D<float> map, float2 uv)
{
	int w, h;
	map.GetDimensions(w, h);
	float2 xy = uv * float2(w - 1, h - 1);

	// p03--p13-------p23--p33
	//  |    |         |    |
	// p02--p12-------p22--p32     1
	//  |    |         |    |     ...
	//  |   t.y  xy    |    |     t.y
	//  |    |         |    |     ...
	// p01--p11--t.x--p21--p31     0...tx...1
	//  |    |         |    |
	// p00--p10-------p20--p30

	float2 t = frac(xy);
	float4 tx = cubic(t.x);
	float4 ty = cubic(t.y);

	int2 p12 = int2(xy);
	int2 p00 = p12 - int2(1, 2);

	float4x4 v = float4x4(

		map[p00 + int2(0, 0)],
		map[p00 + int2(1, 0)],
		map[p00 + int2(2, 0)],
		map[p00 + int2(3, 0)],

		map[p00 + int2(0, 1)],
		map[p00 + int2(1, 1)],
		map[p00 + int2(2, 1)],
		map[p00 + int2(3, 1)],

		map[p00 + int2(0, 2)],
		map[p00 + int2(1, 2)],
		map[p00 + int2(2, 2)],
		map[p00 + int2(3, 2)],

		map[p00 + int2(0, 3)],
		map[p00 + int2(1, 3)],
		map[p00 + int2(2, 3)],
		map[p00 + int2(3, 3)]

	);

	// first interpolate 4 rows (16 values) on x axis
	float4 c = mul(v, tx);

	// then one final row on y axis
	float f = dot(ty, c);

	return f;
}













float getProceduralHeight01(float3 dir)
{
	float result = 0;

	float2 w;
	float x;


	result += 1 - abs(snoise(dir * 5, 5, 2));

	/*
	{ // terraces
	float3 pos = dir * 10;
	int octaves = 2;
	float freqModifier = 3;
	float ampModifier = 1/freqModifier;
	float amp = 1;
	for (int i = 0; i < octaves; i++)
	{
	float p = snoise(pos, 4, 10);
	result += terrace(p, 0.5) * amp;
	pos *= freqModifier;
	amp *= ampModifier;
	}
	}
	*/
	// small noise
	/*


	{ //big detail
	//continents
	result += abs(snoise(dir*0.5, 5, 4));
	//w = worleyNoise(dir * 2);
	//result += (w.x - w.y) * 2;
	//oceans
	result -= abs(snoise(dir*2.2, 4, 4));
	//big rivers
	x = snoise(dir * 3, 3, 2);
	result += -exp(-pow(x * 55, 2)) * 0.2;
	//craters
	//w = worleyNoise(dir);
	//result += smoothstep(0.0, 0.1, w.x);
	}


	{ //small detail
	float p = snoise(dir * 10, 5, 10) * 100;
	float t = 0.3;
	t = clamp(snoise(dir * 2), 0.1, 1.0);
	result += terrace(p, 0.2)*0.005;
	result += p*0.005;
	//small rivers
	float x = snoise(dir * 3);
	//result += -exp(-pow(x*55,2));
	}


	{
	float p = snoise(dir * 10, 5, 10);
	//result += terrace(p, 0.15)*10;
	//result += p * 0.1;
	}

	{
	//float p = snoise(dir*10, 5, 10);
	//result += terrace(p, 0.1)/1;
	}


	*/

	return result;

}

#endif
