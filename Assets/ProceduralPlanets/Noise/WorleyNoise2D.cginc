// Cellular noise ("Worley noise") in 2D in GLSL.
// Copyright (c) Stefan Gustavson 2011-04-19. All rights reserved.
// This code is released under the conditions of the MIT license.
// See LICENSE file for details.
// https://github.com/stegu/webgl-noise

#ifndef NOISE_WORLEY_2D
#define NOISE_WORLEY_2D

// Modulo 289 without a division (only multiplications)
#ifndef NOISE_3_MOD289_3
#define NOISE_3_MOD289_3
float3 mod289(float3 x) {
  return x - floor(x * (1.0 / 289.0)) * 289.0;
}
#endif

#ifndef NOISE_2_MOD289_2
#define NOISE_2_MOD289_2
float2 mod289(float2 x) {
  return x - floor(x * (1.0 / 289.0)) * 289.0;
}
#endif

// Modulo 7 without a division
#ifndef NOISE_3_MOD7_3
#define NOISE_3_MOD7_3
float3 mod7(float3 x) {
  return x - floor(x * (1.0 / 7.0)) * 7.0;
}
#endif

// Permutation polynomial: (34x^2 + x) mod 289
#ifndef NOISE_3_PERMUTE_3
#define NOISE_3_PERMUTE_3
float3 permute(float3 x) {
  return mod289((34.0 * x + 1.0) * x);
}
#endif

// Cellular noise, returning F1 and F2 in a float2.
// Standard 3x3 search window for good F1 and F2 values
float2 worley(float2 P) {
#define NOISE_WORLEY_2D_K 0.142857142857 // 1/7
#define NOISE_WORLEY_2D_Ko 0.428571428571 // 3/7
#define NOISE_WORLEY_2D_jitter 1.0 // Less gives more regular pattern
	float2 Pi = mod289(floor(P));
 	float2 Pf = frac(P);
	float3 oi = float3(-1.0, 0.0, 1.0);
	float3 of = float3(-0.5, 0.5, 1.5);
	float3 px = permute(Pi.x + oi);
	float3 p = permute(px.x + Pi.y + oi); // p11, p12, p13
	float3 ox = frac(p*NOISE_WORLEY_2D_K) - NOISE_WORLEY_2D_Ko;
	float3 oy = mod7(floor(p*NOISE_WORLEY_2D_K))*NOISE_WORLEY_2D_K - NOISE_WORLEY_2D_Ko;
	float3 dx = Pf.x + 0.5 + NOISE_WORLEY_2D_jitter*ox;
	float3 dy = Pf.y - of + NOISE_WORLEY_2D_jitter*oy;
	float3 d1 = dx * dx + dy * dy; // d11, d12 and d13, squared
	p = permute(px.y + Pi.y + oi); // p21, p22, p23
	ox = frac(p*NOISE_WORLEY_2D_K) - NOISE_WORLEY_2D_Ko;
	oy = mod7(floor(p*NOISE_WORLEY_2D_K))*NOISE_WORLEY_2D_K - NOISE_WORLEY_2D_Ko;
	dx = Pf.x - 0.5 + NOISE_WORLEY_2D_jitter*ox;
	dy = Pf.y - of + NOISE_WORLEY_2D_jitter*oy;
	float3 d2 = dx * dx + dy * dy; // d21, d22 and d23, squared
	p = permute(px.z + Pi.y + oi); // p31, p32, p33
	ox = frac(p*NOISE_WORLEY_2D_K) - NOISE_WORLEY_2D_Ko;
	oy = mod7(floor(p*NOISE_WORLEY_2D_K))*NOISE_WORLEY_2D_K - NOISE_WORLEY_2D_Ko;
	dx = Pf.x - 1.5 + NOISE_WORLEY_2D_jitter*ox;
	dy = Pf.y - of + NOISE_WORLEY_2D_jitter*oy;
	float3 d3 = dx * dx + dy * dy; // d31, d32 and d33, squared
	// Sort out the two smallest distances (F1, F2)
	float3 d1a = min(d1, d2);
	d2 = max(d1, d2); // Swap to keep candidates for F2
	d2 = min(d2, d3); // neither F1 nor F2 are now in d3
	d1 = min(d1a, d2); // F1 is now in d1
	d2 = max(d1a, d2); // Swap to keep candidates for F2
	d1.xy = (d1.x < d1.y) ? d1.xy : d1.yx; // Swap if smaller
	d1.xz = (d1.x < d1.z) ? d1.xz : d1.zx; // F1 is in d1.x
	d1.yz = min(d1.yz, d2.yz); // F2 is now not in d2.yz
	d1.y = min(d1.y, d1.z); // nor in  d1.z
	d1.y = min(d1.y, d2.x); // F2 is in d1.y, we're done.
	return sqrt(d1.xy);
}

#endif