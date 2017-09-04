#ifndef NOISE_ALL
#define NOISE_ALL

#include "ClassicNoise2D.cginc" // float cnoise(float2 pos)
								// float pnoise(float2 pos, float2 rep)
#include "ClassicNoise3D.cginc" // float cnoise(float3 pos)
								// float pnoise(float3 pos, float3 rep)
#include "ClassicNoise4D.cginc" // float cnoise(float4 pos)
								// float pnoise(float4 pos, float4 rep)

#include "SimplexNoise2D.cginc" // float snoise(float2 pos)
#include "SimplexNoise3D.cginc" // float snoise(float3 pos)
#include "SimplexNoise3DGradient.cginc" // float snoise(float3 pos, out float3 gradient)
#include "SimplexNoise4D.cginc" // float snoise(float3 pos)

#include "WorleyNoise2D.cginc" // float2 worley(float2 pos)
#include "WorleyNoise2x2.cginc" // float2 worley2x2(float2 pos)
#include "WorleyNoise3D.cginc" // float2 worley(float3 pos)
#include "WorleyNoise2x2x2.cginc" // float2 worley2x2x2(float3 pos)

#endif