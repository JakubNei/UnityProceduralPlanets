// Cellular noise ("Worley noise") in 3D in GLSL.
// Copyright (c) Stefan Gustavson 2011-04-19. All rights reserved.
// This code is released under the conditions of the MIT license.
// See LICENSE file for details.
// https://github.com/stegu/webgl-noise

#ifndef NOISE_WORLEY_3D
#define NOISE_WORLEY_3D

// Modulo 289 without a division (only multiplications)
#ifndef NOISE_3_MOD289_3
#define NOISE_3_MOD289_3
float3 mod289(float3 x) {
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
// 3x3x3 search region for good F2 everywhere, but a lot
// slower than the 2x2x2 version.
// The code below is a bit scary even to its author,
// but it has at least half decent performance on a
// modern GPU. In any case, it beats any software
// implementation of Worley noise hands down.

float2 worley(float3 P) {
#define NOISE_WORLEY_3D_K 0.142857142857 // 1/7
#define NOISE_WORLEY_3D_Ko 0.428571428571 // 1/2-NOISE_WORLEY_3D_K/2
#define NOISE_WORLEY_3D_K2 0.020408163265306 // 1/(7*7)
#define NOISE_WORLEY_3D_Kz 0.166666666667 // 1/6
#define NOISE_WORLEY_3D_Kzo 0.416666666667 // 1/2-1/6*2
#define NOISE_WORLEY_3D_jitter 1.0 // smaller NOISE_WORLEY_3D_jitter gives more regular pattern

	float3 Pi = mod289(floor(P));
 	float3 Pf = frac(P) - 0.5;

	float3 Pfx = Pf.x + float3(1.0, 0.0, -1.0);
	float3 Pfy = Pf.y + float3(1.0, 0.0, -1.0);
	float3 Pfz = Pf.z + float3(1.0, 0.0, -1.0);

	float3 p = permute(Pi.x + float3(-1.0, 0.0, 1.0));
	float3 p1 = permute(p + Pi.y - 1.0);
	float3 p2 = permute(p + Pi.y);
	float3 p3 = permute(p + Pi.y + 1.0);

	float3 p11 = permute(p1 + Pi.z - 1.0);
	float3 p12 = permute(p1 + Pi.z);
	float3 p13 = permute(p1 + Pi.z + 1.0);

	float3 p21 = permute(p2 + Pi.z - 1.0);
	float3 p22 = permute(p2 + Pi.z);
	float3 p23 = permute(p2 + Pi.z + 1.0);

	float3 p31 = permute(p3 + Pi.z - 1.0);
	float3 p32 = permute(p3 + Pi.z);
	float3 p33 = permute(p3 + Pi.z + 1.0);

	float3 ox11 = frac(p11*NOISE_WORLEY_3D_K) - NOISE_WORLEY_3D_Ko;
	float3 oy11 = mod7(floor(p11*NOISE_WORLEY_3D_K))*NOISE_WORLEY_3D_K - NOISE_WORLEY_3D_Ko;
	float3 oz11 = floor(p11*NOISE_WORLEY_3D_K2)*NOISE_WORLEY_3D_Kz - NOISE_WORLEY_3D_Kzo; // p11 < 289 guaranteed

	float3 ox12 = frac(p12*NOISE_WORLEY_3D_K) - NOISE_WORLEY_3D_Ko;
	float3 oy12 = mod7(floor(p12*NOISE_WORLEY_3D_K))*NOISE_WORLEY_3D_K - NOISE_WORLEY_3D_Ko;
	float3 oz12 = floor(p12*NOISE_WORLEY_3D_K2)*NOISE_WORLEY_3D_Kz - NOISE_WORLEY_3D_Kzo;

	float3 ox13 = frac(p13*NOISE_WORLEY_3D_K) - NOISE_WORLEY_3D_Ko;
	float3 oy13 = mod7(floor(p13*NOISE_WORLEY_3D_K))*NOISE_WORLEY_3D_K - NOISE_WORLEY_3D_Ko;
	float3 oz13 = floor(p13*NOISE_WORLEY_3D_K2)*NOISE_WORLEY_3D_Kz - NOISE_WORLEY_3D_Kzo;

	float3 ox21 = frac(p21*NOISE_WORLEY_3D_K) - NOISE_WORLEY_3D_Ko;
	float3 oy21 = mod7(floor(p21*NOISE_WORLEY_3D_K))*NOISE_WORLEY_3D_K - NOISE_WORLEY_3D_Ko;
	float3 oz21 = floor(p21*NOISE_WORLEY_3D_K2)*NOISE_WORLEY_3D_Kz - NOISE_WORLEY_3D_Kzo;

	float3 ox22 = frac(p22*NOISE_WORLEY_3D_K) - NOISE_WORLEY_3D_Ko;
	float3 oy22 = mod7(floor(p22*NOISE_WORLEY_3D_K))*NOISE_WORLEY_3D_K - NOISE_WORLEY_3D_Ko;
	float3 oz22 = floor(p22*NOISE_WORLEY_3D_K2)*NOISE_WORLEY_3D_Kz - NOISE_WORLEY_3D_Kzo;

	float3 ox23 = frac(p23*NOISE_WORLEY_3D_K) - NOISE_WORLEY_3D_Ko;
	float3 oy23 = mod7(floor(p23*NOISE_WORLEY_3D_K))*NOISE_WORLEY_3D_K - NOISE_WORLEY_3D_Ko;
	float3 oz23 = floor(p23*NOISE_WORLEY_3D_K2)*NOISE_WORLEY_3D_Kz - NOISE_WORLEY_3D_Kzo;

	float3 ox31 = frac(p31*NOISE_WORLEY_3D_K) - NOISE_WORLEY_3D_Ko;
	float3 oy31 = mod7(floor(p31*NOISE_WORLEY_3D_K))*NOISE_WORLEY_3D_K - NOISE_WORLEY_3D_Ko;
	float3 oz31 = floor(p31*NOISE_WORLEY_3D_K2)*NOISE_WORLEY_3D_Kz - NOISE_WORLEY_3D_Kzo;

	float3 ox32 = frac(p32*NOISE_WORLEY_3D_K) - NOISE_WORLEY_3D_Ko;
	float3 oy32 = mod7(floor(p32*NOISE_WORLEY_3D_K))*NOISE_WORLEY_3D_K - NOISE_WORLEY_3D_Ko;
	float3 oz32 = floor(p32*NOISE_WORLEY_3D_K2)*NOISE_WORLEY_3D_Kz - NOISE_WORLEY_3D_Kzo;

	float3 ox33 = frac(p33*NOISE_WORLEY_3D_K) - NOISE_WORLEY_3D_Ko;
	float3 oy33 = mod7(floor(p33*NOISE_WORLEY_3D_K))*NOISE_WORLEY_3D_K - NOISE_WORLEY_3D_Ko;
	float3 oz33 = floor(p33*NOISE_WORLEY_3D_K2)*NOISE_WORLEY_3D_Kz - NOISE_WORLEY_3D_Kzo;

	float3 dx11 = Pfx + NOISE_WORLEY_3D_jitter*ox11;
	float3 dy11 = Pfy.x + NOISE_WORLEY_3D_jitter*oy11;
	float3 dz11 = Pfz.x + NOISE_WORLEY_3D_jitter*oz11;

	float3 dx12 = Pfx + NOISE_WORLEY_3D_jitter*ox12;
	float3 dy12 = Pfy.x + NOISE_WORLEY_3D_jitter*oy12;
	float3 dz12 = Pfz.y + NOISE_WORLEY_3D_jitter*oz12;

	float3 dx13 = Pfx + NOISE_WORLEY_3D_jitter*ox13;
	float3 dy13 = Pfy.x + NOISE_WORLEY_3D_jitter*oy13;
	float3 dz13 = Pfz.z + NOISE_WORLEY_3D_jitter*oz13;

	float3 dx21 = Pfx + NOISE_WORLEY_3D_jitter*ox21;
	float3 dy21 = Pfy.y + NOISE_WORLEY_3D_jitter*oy21;
	float3 dz21 = Pfz.x + NOISE_WORLEY_3D_jitter*oz21;

	float3 dx22 = Pfx + NOISE_WORLEY_3D_jitter*ox22;
	float3 dy22 = Pfy.y + NOISE_WORLEY_3D_jitter*oy22;
	float3 dz22 = Pfz.y + NOISE_WORLEY_3D_jitter*oz22;

	float3 dx23 = Pfx + NOISE_WORLEY_3D_jitter*ox23;
	float3 dy23 = Pfy.y + NOISE_WORLEY_3D_jitter*oy23;
	float3 dz23 = Pfz.z + NOISE_WORLEY_3D_jitter*oz23;

	float3 dx31 = Pfx + NOISE_WORLEY_3D_jitter*ox31;
	float3 dy31 = Pfy.z + NOISE_WORLEY_3D_jitter*oy31;
	float3 dz31 = Pfz.x + NOISE_WORLEY_3D_jitter*oz31;

	float3 dx32 = Pfx + NOISE_WORLEY_3D_jitter*ox32;
	float3 dy32 = Pfy.z + NOISE_WORLEY_3D_jitter*oy32;
	float3 dz32 = Pfz.y + NOISE_WORLEY_3D_jitter*oz32;

	float3 dx33 = Pfx + NOISE_WORLEY_3D_jitter*ox33;
	float3 dy33 = Pfy.z + NOISE_WORLEY_3D_jitter*oy33;
	float3 dz33 = Pfz.z + NOISE_WORLEY_3D_jitter*oz33;

	float3 d11 = dx11 * dx11 + dy11 * dy11 + dz11 * dz11;
	float3 d12 = dx12 * dx12 + dy12 * dy12 + dz12 * dz12;
	float3 d13 = dx13 * dx13 + dy13 * dy13 + dz13 * dz13;
	float3 d21 = dx21 * dx21 + dy21 * dy21 + dz21 * dz21;
	float3 d22 = dx22 * dx22 + dy22 * dy22 + dz22 * dz22;
	float3 d23 = dx23 * dx23 + dy23 * dy23 + dz23 * dz23;
	float3 d31 = dx31 * dx31 + dy31 * dy31 + dz31 * dz31;
	float3 d32 = dx32 * dx32 + dy32 * dy32 + dz32 * dz32;
	float3 d33 = dx33 * dx33 + dy33 * dy33 + dz33 * dz33;

	// Sort out the two smallest distances (F1, F2)
#if 0
	// Cheat and sort out only F1
	float3 d1 = min(min(d11,d12), d13);
	float3 d2 = min(min(d21,d22), d23);
	float3 d3 = min(min(d31,d32), d33);
	float3 d = min(min(d1,d2), d3);
	d.x = min(min(d.x,d.y),d.z);
	return float2(sqrt(d.x)); // F1 duplicated, no F2 computed
#else
	// Do it right and sort out both F1 and F2
	float3 d1a = min(d11, d12);
	d12 = max(d11, d12);
	d11 = min(d1a, d13); // Smallest now not in d12 or d13
	d13 = max(d1a, d13);
	d12 = min(d12, d13); // 2nd smallest now not in d13
	float3 d2a = min(d21, d22);
	d22 = max(d21, d22);
	d21 = min(d2a, d23); // Smallest now not in d22 or d23
	d23 = max(d2a, d23);
	d22 = min(d22, d23); // 2nd smallest now not in d23
	float3 d3a = min(d31, d32);
	d32 = max(d31, d32);
	d31 = min(d3a, d33); // Smallest now not in d32 or d33
	d33 = max(d3a, d33);
	d32 = min(d32, d33); // 2nd smallest now not in d33
	float3 da = min(d11, d21);
	d21 = max(d11, d21);
	d11 = min(da, d31); // Smallest now in d11
	d31 = max(da, d31); // 2nd smallest now not in d31
	d11.xy = (d11.x < d11.y) ? d11.xy : d11.yx;
	d11.xz = (d11.x < d11.z) ? d11.xz : d11.zx; // d11.x now smallest
	d12 = min(d12, d21); // 2nd smallest now not in d21
	d12 = min(d12, d22); // nor in d22
	d12 = min(d12, d31); // nor in d31
	d12 = min(d12, d32); // nor in d32
	d11.yz = min(d11.yz,d12.xy); // nor in d12.yz
	d11.y = min(d11.y,d12.z); // Only two more to go
	d11.y = min(d11.y,d11.z); // Done! (Phew!)
	return sqrt(d11.xy); // F1, F2
#endif
}

#endif