













double d_sqrt(double x)
{
	return sqrt(float(x));
}
double2 d_sqrt(double2 a) { return double2(d_sqrt(a.x), d_sqrt(a.y)); }
double3 d_sqrt(double3 a) { return double3(d_sqrt(a.x), d_sqrt(a.y), d_sqrt(a.z)); }
double4 d_sqrt(double4 a) { return double4(d_sqrt(a.x), d_sqrt(a.y), d_sqrt(a.z), d_sqrt(a.w)); }

double d_floor(double x)
{
	return floor(float(x));
}
double d_acos(double x)
{
	return acos(float(x));
}






double d_length(double3 a)
{
	return d_sqrt(a.x*a.x + a.y*a.y + a.z*a.z);
}

double3 d_lerp(double3 a, double3 b, double t)
{
	return a * (1 - t) + b * t;
}


double d_dot(double3 a, double3 b)
{
	return a.x * b.x + a.y * b.y + a.z * b.z;
}

double3 d_normalize(double3 n)
{
	return n/d_length(n);
}
double3 d_cross(double3 a, double3 b)
{
	return double3(
		a.y*b.z - a.z*b.y,
		a.z*b.x - a.x*b.z,
		a.x*b.y - a.y*b.x
	);
}










// taken from http://outerra.blogspot.cz/2017/06/fp64-approximations-for-sincos-for.html
// which used http://lolengine.net/wiki/doc/maths/remez

//sin approximation, error < 5e-9
double d_sina_9(double x)
{
	//minimax coefs for sin for 0..pi/2 range
	const double a3 = -1.666665709650470145824129400050267289858e-1L;
	const double a5 = 8.333017291562218127986291618761571373087e-3L;
	const double a7 = -1.980661520135080504411629636078917643846e-4L;
	const double a9 = 2.600054767890361277123254766503271638682e-6L;

	const double m_2_pi = 0.636619772367581343076L;
	const double m_pi_2 = 1.57079632679489661923L;

	double y = abs(x * m_2_pi);
	double q = d_floor(y);
	int quadrant = int(q);

	double t = (quadrant & 1) != 0 ? 1 - y + q : y - q;
	t *= m_pi_2;

	double t2 = t * t;
	double r = fma(fma(fma(fma(a9, t2, a7), t2, a5), t2, a3), t2*t, t);

	r = x < 0 ? -r : r;

	return (quadrant & 2) != 0 ? -r : r;
}

//sin approximation, error < 2e-11
double d_sina_11(double x)
{
	//minimax coefs for sin for 0..pi/2 range
	const double a3 = -1.666666660646699151540776973346659104119e-1L;
	const double a5 = 8.333330495671426021718370503012583606364e-3L;
	const double a7 = -1.984080403919620610590106573736892971297e-4L;
	const double a9 = 2.752261885409148183683678902130857814965e-6L;
	const double ab = -2.384669400943475552559273983214582409441e-8L;

	const double m_2_pi = 0.636619772367581343076L;
	const double m_pi_2 = 1.57079632679489661923L;

	double y = abs(x * m_2_pi);
	double q = d_floor(y);
	int quadrant = int(q);

	double t = (quadrant & 1) != 0 ? 1 - y + q : y - q;
	t *= m_pi_2;

	double t2 = t * t;
	double r = fma(fma(fma(fma(fma(ab, t2, a9), t2, a7), t2, a5), t2, a3), t2*t, t);

	r = x < 0 ? -r : r;

	return (quadrant & 2) != 0 ? -r : r;
}
double d_sin(double x) { return d_sina_11(x); }

// proximation, error < 5e-9
double d_cosa_9(double x)
{
	//sin(x + PI/2) = cos(x)
	return d_sina_9(x + double(1.57079632679489661923L));
}

//cos approximation, error < 2e-11
double d_cosa_11(double x)
{
	//sin(x + PI/2) = cos(x)
	return d_sina_11(x + double(1.57079632679489661923L));
}
double d_cos(double x) { return d_cosa_11(x); }












double3 d_slerp(double3 start, double3 end, double percent)
{
	// Dot product - the cosine of the angle between 2 vectors.
	double d = d_dot(start, end);
	// Clamp it to be in the range of Acos()
	// This may be unnecessary, but doubleing point
	// precision can be a fickle mistress.
	d = clamp(d, -1.0, 1.0);
	// Acos(dot) returns the angle between start and end,
	// And multiplying that by percent returns the angle between
	// start and the final result.
	double theta = d_acos(d)*percent;
	double3 relativeVec = end - start*d; // Orthonormal basis
	relativeVec /= d_length(relativeVec);
	// The final result.
	return ((start*d_cos(theta)) + (relativeVec*d_sin(theta)));
}

float3 slerp(float3 start, float3 end, float percent)
{
	// Dot product - the cosine of the angle between 2 vectors.
	float d = dot(start, end);
	// Clamp it to be in the range of Acos()
	// This may be unnecessary, but floating point
	// precision can be a fickle mistress.
	d = clamp(d, -1.0, 1.0);
	// Acos(dot) returns the angle between start and end,
	// And multiplying that by percent returns the angle between
	// start and the final result.
	float theta = acos(d)*percent;
	float3 relativeVec = normalize(end - start*d); // Orthonormal basis
												   // The final result.
	return ((start*cos(theta)) + (relativeVec*sin(theta)));
}
