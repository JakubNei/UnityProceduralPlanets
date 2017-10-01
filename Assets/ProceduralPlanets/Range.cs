using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public struct Range
{
	public WorldPos a;
	public WorldPos b;
	public WorldPos c;
	public WorldPos d;

	/*
	A -- B
	|    |
	D -- C

	front face is clock wise face
	*/


	public WorldPos CenterPos
	{
		get
		{
			return (a + b + c + d) / 4.0f;
		}
	}

	public Vector3 Normal
	{
		get
		{
			return Vector3.Normalize(
				Vector3.Cross(
					WorldPos.Normalize(b - a),
					WorldPos.Normalize(d - a)
				)
			);
		}
	}


	public Sphere ToBoundingSphere()
	{
		var center = CenterPos;
		var radius =
			Math.Max(
				Math.Max(
					WorldPos.Distance(center, a),
					WorldPos.Distance(center, b)
				),
				Math.Max(
					WorldPos.Distance(center, c),
					WorldPos.Distance(center, d)
				)
			);

		return new Sphere(center, (float)radius);
	}

	public void SetParams(ComputeShader s, string prefix)
	{
		s.SetVector(prefix + "A", a);
		s.SetVector(prefix + "B", b);
		s.SetVector(prefix + "C", c);
		s.SetVector(prefix + "D", d);
	}

	public void DrawGizmos()
	{
		Gizmos.DrawLine(a, b);
		Gizmos.DrawLine(b, c);
		Gizmos.DrawLine(c, d);
		Gizmos.DrawLine(d, a);
	}

	public void DrawGizmos(Vector3 offset)
	{
		Gizmos.DrawLine(a + offset, b + offset);
		Gizmos.DrawLine(b + offset, c + offset);
		Gizmos.DrawLine(c + offset, d + offset);
		Gizmos.DrawLine(d + offset, a + offset);
	}
}