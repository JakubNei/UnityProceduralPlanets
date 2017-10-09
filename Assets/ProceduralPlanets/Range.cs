using System;
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
			return (a + b + c + d) / 4.0f;
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
		var radius =
			Math.Max(
				Math.Max(
					Vector3.Distance(center, a),
					Vector3.Distance(center, b)
				),
				Math.Max(
					Vector3.Distance(center, c),
					Vector3.Distance(center, d)
				)
			);

		return new Sphere(center, (float)radius);
	}


	public Vector3 GetPosAt(Vector2 uv)
	{
		return Vector3.Lerp(
			Vector3.Lerp(a, b, uv.x),
			Vector3.Lerp(d, c, uv.x),
			uv.y
		);
	}

	public Vector3 GetUnityDirAt(Vector2 uv)
	{
		var unitCube = GetPosAt(uv);
		var unitSphere = MyMath.UnitCubeToUnitSphere(unitCube);
		return unitSphere;
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