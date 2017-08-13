#ifndef PLANET_CGINC
#define PLANET_CGINC






#include "noiseSimplex.cginc"


#define PACK_NORMAL(NORMAL) ((NORMAL + float3(1, 1, 1)) / float3(2, 2, 2))
#define UNPACK_NORMAL(NORMAL) (NORMAL * float3(2, 2, 2) - float3(1, 1, 1))


#define M_PI 3.1415926535897932384626433832795

float3 calestialToSpherical(float3 c /*calestial*/)
{
	float r = length(c);
	if (r == 0) return float3(0, 0, 0);

	// calculate
	float3 p = float3(
		atan(c.z / c.x),  // longitude = x
		asin(c.y / r), // latitude = y
		r // altitude = z
		);

	// normalize to 0..1 range
	p.x = p.x / (2 * M_PI) + 0.5;;
	p.y = p.y / M_PI + 0.5;

	return p;
}

float3 sphericalToCalestial(float3 c /*spherical*/)
{
	// denormalize from 0..1
	c.x = (c.x - 0.5) * (2 * M_PI);
	c.y = (c.y - 0.5) * M_PI;

	// calculate
	float3 p = float3(
		cos(c.y) * cos(c.x) * c.z,
		sin(c.y) * c.z,
		cos(c.y) * sin(c.x) * c.z
		);

	return p;
}

float3 sphericalToCalestial(float2 c /*spherical*/)
{
	// denormalize from 0..1
	c.x = (c.x - 0.5) * (2 * M_PI);
	c.y = (c.y - 0.5) * M_PI;

	// calculate
	float3 p = float3(
		cos(c.y) * cos(c.x),
		sin(c.y),
		cos(c.y) * sin(c.x)
		);

	return p;
}






float3 baseMapUvToDirFromCenter(float2 uv) {
	return sphericalToCalestial(uv);
}

float2 dirFromCenterToBaseMapUv(float3 dir) {
	return calestialToSpherical(dir).xy;
}





// https://gamedev.stackexchange.com/questions/116205/terracing-mountain-features
float terrace(float h, float bandHeight) {
	float W = bandHeight; // width of terracing bands
	float k = floor(h / W);
	float f = (h - k*W) / W;
	float s = min(2 * f, 1.0);
	return (k + s) * W;
}


float snoise(float3 pos, int octaves, float modifier)
{
	float result = 0;
	float amp = 1;
	for (int i = 0; i < octaves; i++)
	{
		result += snoise(pos) * amp;
		pos *= modifier;
		amp /= modifier;
	}
	return result;
}





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
	float2 xy = uv * float2(w - 1, h - 1); // 0,0 ... w,h

	float2 xyFloored = floor(xy);
	float2 t = xy - xyFloored; // 0,0 ... 1,1

	int2 p00 = int2(xyFloored);

	float3 v00 = map[p00 + int2(0, 0)].xyz;
	float3 v01 = map[p00 + int2(0, 1)].xyz;
	float3 v10 = map[p00 + int2(1, 0)].xyz;
	float3 v11 = map[p00 + int2(1, 1)].xyz;

	return
		(v00*(1 - t.x) + v10*t.x) * (1 - t.y) +
		(v01*(1 - t.x) + v11*t.x) * t.y;
}


float SampleLinearFloat(Texture2D<float> map, float2 uv)
{
	int w, h;
	map.GetDimensions(w, h);
	float2 xy = uv * float2(w - 1, h - 1); // 0,0 ... w,h

	float2 xyFloored = floor(xy);
	float2 t = xy - xyFloored; // 0,0 ... 1,1

	int2 p00 = int2(xyFloored);

	float v00 = map[p00 + int2(0, 0)].x;
	float v01 = map[p00 + int2(0, 1)].x;
	float v10 = map[p00 + int2(1, 0)].x;
	float v11 = map[p00 + int2(1, 1)].x;

	return
		(v00*(1 - t.x) + v10*t.x) * (1 - t.y) +
		(v01*(1 - t.x) + v11*t.x) * t.y;
}




float SampleCubicFloat(Texture2D<float> map, float2 uv)
{
	int w, h;
	map.GetDimensions(w, h);
	float2 xy = uv * float2(w - 1, h - 1); // 0,0 ... w,h

	/*
	p03--p13-------p23--p33
	|    |         |    |
	p02--p12-------p22--p32     1
	|    |         |    |     ...
	|   t.y  xy    |    |     t.y
	|    |         |    |     ...
	p01--p11--t.x--p21--p31     0...tx...1
	|    |         |    |
	p00--p10-------p20--p30
	*/

	float2 xyFloored = floor(xy);
	float2 t = xy - xyFloored; // 0,0 ... 1,1
	float2 t2 = t * t;
	float2 t3 = t2 * t;

	int2 p12 = int2(xyFloored);
	int2 p00 = p12 - int2(1, 2);


	float v00 = map[p00 + int2(0, 0)].x;
	float v01 = map[p00 + int2(0, 1)].x;
	float v02 = map[p00 + int2(0, 2)].x;
	float v03 = map[p00 + int2(0, 3)].x;

	float v10 = map[p00 + int2(1, 0)].x;
	float v11 = map[p00 + int2(1, 1)].x;
	float v12 = map[p00 + int2(1, 2)].x;
	float v13 = map[p00 + int2(1, 3)].x;

	float v20 = map[p00 + int2(2, 0)].x;
	float v21 = map[p00 + int2(2, 1)].x;
	float v22 = map[p00 + int2(2, 2)].x;
	float v23 = map[p00 + int2(2, 3)].x;

	float v30 = map[p00 + int2(3, 0)].x;
	float v31 = map[p00 + int2(3, 1)].x;
	float v32 = map[p00 + int2(3, 2)].x;
	float v33 = map[p00 + int2(3, 3)].x;


	// https://en.wikipedia.org/wiki/Cubic_Hermite_spline
#define CUBIC_HERMITE(T,T2,T3,P0,P1,P2,P3) ((2*T3-3*T2+1)*P1 + (T3-2*T2+T)*(P1-P0) + (-2*T3+3*T2)*P2 + (T3-T2)*(P3-P2))

	// first interpolate on X
	float c0 = CUBIC_HERMITE(t.x, t2.x, t3.x, v00, v10, v20, v30);
	float c1 = CUBIC_HERMITE(t.x, t2.x, t3.x, v01, v11, v21, v31);
	float c2 = CUBIC_HERMITE(t.x, t2.x, t3.x, v02, v12, v22, v32);
	float c3 = CUBIC_HERMITE(t.x, t2.x, t3.x, v03, v13, v23, v33);

	// then on Y
	float f = CUBIC_HERMITE(t.y, t2.y, t3.y, c0, c1, c2, c3);

	return f;
}



#endif