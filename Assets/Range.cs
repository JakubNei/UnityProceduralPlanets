using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public struct Range
{
	public Vector3 a;
	public Vector3 b;
	public Vector3 c;
	public Vector3 d;

	/*
	A -- B
	|    |
	D -- C

	front face is clock wise face
	*/


	public Vector3 CenterPos
	{
		get
		{
			return (a + b + c +d) / 4.0f;
		}
	}

	public Vector3 Normal
	{
		get
		{
			return Vector3.Normalize(
				Vector3.Cross(
					Vector3.Normalize(b - a),
					Vector3.Normalize(d - a)
				)
			);
		}
	}


	public Sphere ToBoundingSphere()
	{
		var center = CenterPos;
		var radius = (float)Math.Sqrt(
			Math.Max(
				Math.Max(
					Vector3.Distance(center, a),
					Vector3.Distance(center, b)
				),
				Math.Max(
					Vector3.Distance(center, c),
					Vector3.Distance(center, d)
				)
			)
		);
		return new Sphere(center, radius);
	}

	public void SetParams(ComputeShader s, string prefix)
	{
		s.SetVector(prefix + "A", a);
		s.SetVector(prefix + "B", b);
		s.SetVector(prefix + "C", c);
		s.SetVector(prefix + "D", d);
	}
}