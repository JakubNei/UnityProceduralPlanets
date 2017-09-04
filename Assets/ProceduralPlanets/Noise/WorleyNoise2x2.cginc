// Cellular noise ("Worley noise") in 2D in GLSL.
// Copyright (c) Stefan Gustavson 2011-04-19. All rights reserved.
// This code is released under the conditions of the MIT license.
// See LICENSE file for details.
// https://github.com/stegu/webgl-noise

#ifndef NOISE_WORLEY_2X2
#define NOISE_WORLEY_2X2

// Modulo 289 without a division (only multiplications)
#ifndef NOISE_2_MOD289_2
#define NOISE_2_MOD289_2
float2 mod289(float2 x) {
  return x - floor(x * (1.0 / 289.0)) * 289.0;
}
#endif

#ifndef NOISE_4_MOD289_4
#define NOISE_4_MOD289_4
float4 mod289(float4 x) {
  return x - floor(x * (1.0 / 289.0)) * 289.0;
}
#endif

// Modulo 7 without a division
#ifndef NOISE_4_MOD7_4
#define NOISE_4_MOD7_4
float4 mod7(float4 x) {
  return x - floor(x * (1.0 / 7.0)) * 7.0;
}
#endif

// Permutation polynomial: (34x^2 + x) mod 289
#ifndef NOISE_4_PERMUTE_4
#define NOISE_4_PERMUTE_4
float4 permute(float4 x) {
  return mod289((34.0 * x + 1.0) * x);
}
#endif

// Cellular noise, returning F1 and F2 in a float2.
// Speeded up by using 2x2 search window instead of 3x3,
// at the expense of some strong pattern artifacts.
// F2 is often wrong and has sharp discontinuities.
// If you need a smooth F2, use the slower 3x3 version.
// F1 is sometimes wrong, too, but OK for most purposes.
float2 worley2x2(float2 P) {
#define NOISE_WORLEY_2X2_K 0.142857142857 // 1/7
#define NOISE_WORLEY_2X2_K2 0.0714285714285 // NOISE_WORLEY_2X2_K/2
#define NOISE_WORLEY_2X2_jitter 0.8 // NOISE_WORLEY_2X2_jitter 1.0 makes F1 wrong more often
	float2 Pi = mod289(floor(P));
 	float2 Pf = frac(P);
	float4 Pfx = Pf.x + float4(-0.5, -1.5, -0.5, -1.5);
	float4 Pfy = Pf.y + float4(-0.5, -0.5, -1.5, -1.5);
	float4 p = permute(Pi.x + float4(0.0, 1.0, 0.0, 1.0));
	p = permute(p + Pi.y + float4(0.0, 0.0, 1.0, 1.0));
	float4 ox = mod7(p)*NOISE_WORLEY_2X2_K+NOISE_WORLEY_2X2_K2;
	float4 oy = mod7(floor(p*NOISE_WORLEY_2X2_K))*NOISE_WORLEY_2X2_K+NOISE_WORLEY_2X2_K2;
	float4 dx = Pfx + NOISE_WORLEY_2X2_jitter*ox;
	float4 dy = Pfy + NOISE_WORLEY_2X2_jitter*oy;
	float4 d = dx * dx + dy * dy; // d11, d12, d21 and d22, squared
	// Sort out the two smallest distances
#if 0
	// Cheat and pick only F1
	d.xy = min(d.xy, d.zw);
	d.x = min(d.x, d.y);
	return float2(sqrt(d.x)); // F1 duplicated, F2 not computed
#else
	// Do it right and find both F1 and F2
	d.xy = (d.x < d.y) ? d.xy : d.yx; // Swap if smaller
	d.xz = (d.x < d.z) ? d.xz : d.zx;
	d.xw = (d.x < d.w) ? d.xw : d.wx;
	d.y = min(d.y, d.z);
	d.y = min(d.y, d.w);
	return sqrt(d.xy);
#endif
}

#endif