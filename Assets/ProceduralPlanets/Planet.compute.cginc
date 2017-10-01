#ifndef PLANET_ALL_CGINC
// Upgrade NOTE: excluded shader from OpenGL ES 2.0 because it uses non-square matrices
#pragma exclude_renderers gles
#define PLANET_ALL_CGINC






#include "Planet.Common.cginc"



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








// BIOMES

int selectBiome(float3 dir, float2 slopeXY, float humidity, float altidute)
{
	float slope = length(slopeXY);


	float distanceToPoles = smoothstep(0.4, 1, abs(dir.z));
	float snowWeight = altidute + distanceToPoles + snoise(dir * 100, 5, 2) * 0.1;


	if (slope > 0.3)
		return 0; // rock
	else {
		if (snowWeight > 1.5)
			return 1; // snow
		else if (snowWeight > 1.2)
			return 2; // tundra
		else if (slope < 0.25)
			return 3; // grass
		else
			return 4; // clay
	}
}




#endif