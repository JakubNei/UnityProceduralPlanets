
using UnityEngine;

public static class MyMath
{
	/// <summary>
	/// transforms unitCube position into unitSphere,
	/// implementation license: public domain,
	/// uses math from http://mathproofs.blogspot.cz/2005/07/mapping-cube-to-sphere.html
	/// </summary>
	/// <param name="unitCube">unitCube.xyz is in inclusive range [-1, 1]</param>
	/// <returns></returns>
	public static Vector3 UnitCubeToUnitSphere(Vector3 unitCube)
	{
		var unitCubePow2 = Vector3.Scale(unitCube, unitCube);
		var unitCubePow2Div2 = unitCubePow2 / 2;
		var unitCubePow2Div3 = unitCubePow2 / 3;
		var unitSphere = new Vector3(
			unitCube.x * Mathf.Sqrt(1 - unitCubePow2Div2.y - unitCubePow2Div2.z + unitCubePow2.y * unitCubePow2Div3.z),
			unitCube.y * Mathf.Sqrt(1 - unitCubePow2Div2.z - unitCubePow2Div2.x + unitCubePow2.z * unitCubePow2Div3.x),
			unitCube.z * Mathf.Sqrt(1 - unitCubePow2Div2.x - unitCubePow2Div2.y + unitCubePow2.x * unitCubePow2Div3.y)
		);
		return unitSphere;
	}


	private static Vector4 Cubic(float v)
	{
		Vector4 n = new Vector4(1, 2, 3, 4) - new Vector4(v, v, v, v);
		Vector4 s = Vector3.Scale(Vector3.Scale(n, n), n);
		float x = s.x;
		float y = s.y - 4 * s.x;
		float z = s.z - 4 * s.y + 6 * s.x;
		float w = 6 - x - y - z;
		return new Vector4(x, y, z, w) * (1 / 6.0f);
	}

	/*
	public static float sampleCubicFloat(Texture2D<float> map, Vector2 uv)
	{
		int w, h;
		map.GetDimensions(w, h);
		Vector2 xy = uv * Vector2(w - 1, h - 1);

		// p03--p13-------p23--p33
		//  |    |         |    |
		// p02--p12-------p22--p32     1
		//  |    |         |    |     ...
		//  |   t.y  xy    |    |     t.y
		//  |    |         |    |     ...
		// p01--p11--t.x--p21--p31     0...tx...1
		//  |    |         |    |
		// p00--p10-------p20--p30

		Vector2 t = frac(xy);
		Vector4 tx = Cubic(t.x);
		Vector4 ty = Cubic(t.y);

		int2 p12 = int2(xy);
		int2 p00 = p12 - int2(1, 2);


		Matrix4x4 v = new Matrix4x4(

			map[p00 + int2(0, 0)],
			map[p00 + int2(1, 0)],
			map[p00 + int2(2, 0)],
			map[p00 + int2(3, 0)],

			map[p00 + int2(0, 1)],
			map[p00 + int2(1, 1)],
			map[p00 + int2(2, 1)],
			map[p00 + int2(3, 1)],

			map[p00 + int2(0, 2)],
			map[p00 + int2(1, 2)],
			map[p00 + int2(2, 2)],
			map[p00 + int2(3, 2)],

			map[p00 + int2(0, 3)],
			map[p00 + int2(1, 3)],
			map[p00 + int2(2, 3)],
			map[p00 + int2(3, 3)]

		);

		// first interpolate 4 rows (16 values) on x axis
		Vector4 c = v * tx;

		// then one final row on y axis
		float f = Vector4.Dot(ty, c);

		return f;
	}*/
}