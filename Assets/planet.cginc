#ifndef PLANET_CGINC
#define PLANET_CGINC






#include "noiseSimplex.cginc"





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



float GetProceduralHeight01(float3 dir)
{
	float result = 0;

	float2 w;
	float x;

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



	/*
	{ // hill tops
	float p = snoise(dir * 10);
	if(p > 0) result -= p * 2;
	}
	*/

	/*
	{ // craters

	float2 w = worleyNoise(dir*10, 1, false);
	result += smoothstep(0.0, 0.4, w.x) * 100;
	}
	*/

	return result;

}



/*

float GetHumidity(float2 uvCenter)
{
	float2 uv = uvCenter;
	if (GetProceduralHeight01(uv) < _seaLevel01) return 1;

	const float maxDistanceToWater = 0.05;
	const float distanceToWaterIncrease = 0.001;
	float distanceToWater = 0;

	float splits = 3;
	while (distanceToWater < maxDistanceToWater) {

		float angleDelta = 2 * M_PI / splits;
		for (float angle = 0; angle < 2 * M_PI; angle += angleDelta)
		{
			uv = uvCenter + float2(
				cos(angle) * distanceToWater,
				sin(angle) * distanceToWater
				);
			if (GetProceduralHeight01(uv) < _seaLevel01) return 1 - distanceToWater / maxDistanceToWater;
		}

		distanceToWater += distanceToWaterIncrease;
		splits += 1;
	}

	return 0;
}


float3 GetProceduralAndBaseHeightMapNormal(float2 spherical, float eps) {
	float3 normal;
	float z = GetProceduralHeight(spherical);
	normal.x = float(
		(GetProceduralHeight(float2(spherical.x + eps, spherical.y)) - z)
		- (GetProceduralHeight(float2(spherical.x - eps, spherical.y)) - z)
		) / 2;
	normal.y = float(
		(GetProceduralHeight(float2(spherical.x, spherical.y + eps)) - z)
		- (GetProceduralHeight(float2(spherical.x, spherical.y - eps)) - z)
		) / 2;
	normal.z = eps;
	return normalize(normal);
}



float3 GetProceduralAndBaseHeightMapNormal(float3 direction, float3 normal, float3 tangent, float eps) {


	float3 N = normal;
	float3 T = tangent;
	float3 T2 = T - N * dot(N, T); // Gram-Schmidt orthogonalization of T
	float3 B = normalize(cross(N, T2));

	float3 result;
	float z = GetProceduralHeight(direction);
	result.x = float(
		(GetProceduralHeight(direction - T*eps) - z)
		- (GetProceduralHeight(direction + T*eps) - z)
		) / 2;
	result.y = float(
		(GetProceduralHeight(direction - B*eps) - z)
		- (GetProceduralHeight(direction + B*eps) - z)
		) / 2;
	result.z = eps;
	return normalize(result);
}



*/










#endif