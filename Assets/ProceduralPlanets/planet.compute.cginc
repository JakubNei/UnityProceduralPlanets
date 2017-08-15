#ifndef PLANET_ALL_CGINC
#define PLANET_ALL_CGINC






#include "planet.common.cginc"



float2 GetUV(RWTexture2D<float4> map, int2 id)
{
	float w, h;
	map.GetDimensions(w, h);
	return id / float2(w - 1, h - 1);
}
float2 GetUV(RWTexture2D<float3> map, int2 id)
{
	float w, h;
	map.GetDimensions(w, h);
	return id / float2(w - 1, h - 1);
}
float2 GetUV(RWTexture2D<float2> map, int2 id)
{
	float w, h;
	map.GetDimensions(w, h);
	return id / float2(w - 1, h - 1);
}
float2 GetUV(RWTexture2D<float> map, int2 id)
{
	float w, h;
	map.GetDimensions(w, h);
	return id / float2(w - 1, h - 1);
}





float2 GetUV(Texture2D<float4> map, int2 id)
{
	float w, h;
	map.GetDimensions(w, h);
	return id / float2(w - 1, h - 1);
}
float2 GetUV(Texture2D<float3> map, int2 id)
{
	float w, h;
	map.GetDimensions(w, h);
	return id / float2(w - 1, h - 1);
}
float2 GetUV(Texture2D<float2> map, int2 id)
{
	float w, h;
	map.GetDimensions(w, h);
	return id / float2(w - 1, h - 1);
}
float2 GetUV(Texture2D<float> map, int2 id)
{
	float w, h;
	map.GetDimensions(w, h);
	return id / float2(w - 1, h - 1);
}




/*
float3 SampleLinearFloat3(Texture2D<float3> map, float2 uv)
{
	int w, h;
	map.GetDimensions(w, h);
	float2 pos = uv * float2(w - 1, h - 1);

	int2 off = int2(0, 1);
	float2 weight = frac(pos);
	int2 index = int2(pos);

	float3 bottom =
		lerp(
			map[index],
			map[index + off.yx],
			weight.x
		);

	float3 top =
		lerp(
			map[index + off.xy],
			map[index + off.yy],
			weight.x
		);

	return lerp(bottom, top, weight.y);
}


float SampleLinearFloat(Texture2D<float> map, float2 uv)
{
	int w, h;
	map.GetDimensions(w, h);
	float2 pos = uv * float2(w - 1, h - 1);

	int2 off = int2(0, 1);
	float2 weight = frac(pos);
	int2 index = int2(pos);

	float bottom =
		lerp(
			map[index],
			map[index + off.yx],
			weight.x
		);

	float top =
		lerp(
			map[index + off.xy],
			map[index + off.yy],
			weight.x
		);

	return lerp(bottom, top, weight.y);
}
*/


float SampleCubicFloat(Texture2D<float> map, float2 uv)
{
	int w, h;
	map.GetDimensions(w, h);
	float2 xy = uv * float2(w - 1, h - 1); // 0,0 ... w,h

	// p03--p13-------p23--p33
	//  |    |         |    |
	// p02--p12-------p22--p32     1
	//  |    |         |    |     ...
	//  |   t.y  xy    |    |     t.y
	//  |    |         |    |     ...
	// p01--p11--t.x--p21--p31     0...tx...1
	//  |    |         |    |
	// p00--p10-------p20--p30

	float2 t = frac(xy); // 0,0 ... 1,1
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


#endif
