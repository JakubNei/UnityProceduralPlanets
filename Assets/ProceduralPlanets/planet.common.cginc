#ifndef PLANET_COMMON_CGINC
#define PLANET_COMMON_CGINC






#include "noiseSimplex.cginc"

#define MAX_INT 2147483647

#define PACK_NORMAL(NORMAL) ((NORMAL + float3(1, 1, 1)) / float3(2, 2, 2))
#define UNPACK_NORMAL(NORMAL) (NORMAL * float3(2, 2, 2) - float3(1, 1, 1))


#define M_PI 3.1415926535897932384626433832795

float3 calestialToSpherical(float3 c /*calestial*/)
{
	float r = length(c);
	if (r == 0) return 0;

	// calculate
	float3 p = float3(
		atan2(c.z, c.x),  // longitude = x
		asin(c.y / r), // latitude = y
		r // altitude = z
		);

	// normalize to 0..1 range
	p.x = p.x / (M_PI * 2) + 0.5;
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



// from http://www.java-gaming.org/index.php?topic=35123.0
// maybe it's https://en.wikipedia.org/wiki/Centripetal_Catmull%E2%80%93Rom_spline
float4 cubic(float v)
{
	float4 n = float4(1.0, 2.0, 3.0, 4.0) - v;
	float4 s = n * n * n;
	float x = s.x;
	float y = s.y - 4.0 * s.x;
	float z = s.z - 4.0 * s.y + 6.0 * s.x;
	float w = 6.0 - x - y - z;
	return float4(x, y, z, w) * (1.0 / 6.0);
}
double4 cubic(double v)
{
	double4 n = double4(1.0, 2.0, 3.0, 4.0) - v;
	double4 s = n * n * n;
	double x = s.x;
	double y = s.y - 4.0 * s.x;
	double z = s.z - 4.0 * s.y + 6.0 * s.x;
	double w = 6.0 - x - y - z;
	return double4(x, y, z, w) * (1.0 / 6.0);
}








float3 rgbToHsv(float3 c)
{
	float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
	float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
	float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

	float d = q.x - min(q.w, q.y);
	float e = 1.0e-10;
	return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

float3 hsvToRgb(float3 c)
{
	float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
	float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
	return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
}


// -1 <= unitCube.x && unitCube.x <= 1
// -1 <= unitCube.y && unitCube.y <= 1
// -1 <= unitCube.z && unitCube.z <= 1
// uses math from http://mathproofs.blogspot.cz/2005/07/mapping-cube-to-sphere.html
// implementation license: public domain
float3 unitCubeToUnitSphere(float3 unitCube)
{
	float3 unitCubePow2 = unitCube * unitCube;
	float3 unitCubePow2Div2 = unitCubePow2 / 2;
	float3 unitCubePow2Div3 = unitCubePow2 / 3;
	return unitCube * sqrt(1 - unitCubePow2Div2.yzx - unitCubePow2Div2.zxy + unitCubePow2.yzx * unitCubePow2Div3.zxy);
}



#endif