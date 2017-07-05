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



	public Vector3 CenterPos
	{
		get
		{
			return (a + b + c) / 3.0f;
		}
	}

	public Vector3 Normal
	{
		get
		{
			return Vector3.Normalize(Vector3.Cross(
				b - a,
				c - a
			));
		}
	}


	public Range(Vector3 a, Vector3 b, Vector3 c)
	{
		this.a = a;
		this.b = b;
		this.c = c;
	}

	public Sphere ToBoundingSphere()
	{
		var c = CenterPos;
		var radius = (float)Math.Sqrt(
			Math.Max(
				Math.Max(
					Vector3.Distance(a, b),
					Vector3.Distance(a, c)
				),
				Vector3.Distance(b, c)
			)
		);
		return new Sphere(c, radius);
	}

	// http://gamedev.stackexchange.com/questions/23743/whats-the-most-efficient-way-to-find-barycentric-coordinates
	public Vector3 CalculateBarycentric(Vector3 p)
	{
		var v0 = b - a;
		var v1 = c - a;
		var v2 = p - a;
		var d00 = Vector3.Dot(v0, v0);
		var d01 = Vector3.Dot(v0, v1);
		var d11 = Vector3.Dot(v1, v1);
		var d20 = Vector3.Dot(v2, v0);
		var d21 = Vector3.Dot(v2, v1);
		var denom = d00 * d11 - d01 * d01;
		var result = new Vector3();
		result.y = (d11 * d20 - d01 * d21) / denom;
		result.z = (d00 * d21 - d01 * d20) / denom;
		result.x = 1.0f - result.y - result.z;
		return result;
	}
}