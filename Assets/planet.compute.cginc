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





float3 SampleLinearFloat3(Texture2D<float3> map, float2 uv)
{
	int w, h;
	map.GetDimensions(w, h);
	float2 pos = uv * float2(w - 1, h - 1);

	int2 off = int2(0, 1);
	float2 weight = frac(pos);
	int2 index = int2(floor(pos));

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
	int2 index = int2(floor(pos));

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

	float2 xyFloored = floor(xy);
	float2 t = xy - xyFloored; // 0,0 ... 1,1
	float4 tx = cubic(t.x);
	float4 ty = cubic(t.y);

	int2 p12 = int2(xyFloored);
	int2 p00 = p12 - int2(1, 2);


	float v00 = map[p00 + int2(0, 0)];
	float v01 = map[p00 + int2(0, 1)];
	float v02 = map[p00 + int2(0, 2)];
	float v03 = map[p00 + int2(0, 3)];

	float v10 = map[p00 + int2(1, 0)];
	float v11 = map[p00 + int2(1, 1)];
	float v12 = map[p00 + int2(1, 2)];
	float v13 = map[p00 + int2(1, 3)];

	float v20 = map[p00 + int2(2, 0)];
	float v21 = map[p00 + int2(2, 1)];
	float v22 = map[p00 + int2(2, 2)];
	float v23 = map[p00 + int2(2, 3)];

	float v30 = map[p00 + int2(3, 0)];
	float v31 = map[p00 + int2(3, 1)];
	float v32 = map[p00 + int2(3, 2)];
	float v33 = map[p00 + int2(3, 3)];


#define CUBIC(T,P0,P1,P2,P3) (T.x*P0 + T.y*P1 + T.z*P2 + T.w*P3)

	// first interpolate on X
	float c0 = CUBIC(tx, v00, v10, v20, v30);
	float c1 = CUBIC(tx, v01, v11, v21, v31);
	float c2 = CUBIC(tx, v02, v12, v22, v32);
	float c3 = CUBIC(tx, v03, v13, v23, v33);

	// then on Y
	float f = CUBIC(ty, c0, c1, c2, c3);

	return f;
}


#endif
