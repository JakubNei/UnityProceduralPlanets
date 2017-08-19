using UnityEngine;
using System.Collections.Generic;


public partial class Planet
{

	Vector3[] segmnentVertices;
	public Vector3[] GetSegmentVertices()
	{
		if (segmnentVertices != null) return segmnentVertices;

		segmnentVertices = new Vector3[NumberOfVerticesNeededTotal];

		// DEBUG
		for (int i = 0; i < segmnentVertices.Length; i++)
			segmnentVertices[i] = new Vector3(i % 5, i, 0);


		return segmnentVertices;
	}


	int[] segmentIndicies;
	public int[] GetSegmentIndicies()
	{
		/*
		A_____B
		|_|_|_|
		|_|_|_|
		D_|_|_C

		A_B
		| |
		D_C

		*/

		if (segmentIndicies != null) return segmentIndicies;

		var indicies = new List<int>(NumberOfVerticesNeededTotal);

		for (int line = 0; line < chunkConfig.numberOfVerticesOnEdge - 1; line++)
		{
			for (int column = 0; column < chunkConfig.numberOfVerticesOnEdge - 1; column++)
			{
				var a = line * chunkConfig.numberOfVerticesOnEdge + column;
				var b = a + 1;
				var d = a + chunkConfig.numberOfVerticesOnEdge;
				var c = d + 1;

				indicies.Add(a);
				indicies.Add(b);
				indicies.Add(c);

				indicies.Add(a);
				indicies.Add(c);
				indicies.Add(d);
			}
		}


		segmentIndicies = indicies.ToArray();
		return segmentIndicies;
	}


	Vector2[] segmentUVs;
	public Vector2[] GetSefgmentUVs()
	{
		if (segmentUVs != null) return segmentUVs;

		segmentUVs = new Vector2[NumberOfVerticesNeededTotal];
		int i = 0;

		float max = chunkConfig.numberOfVerticesOnEdge - 1;

		for (int y = 0; y < chunkConfig.numberOfVerticesOnEdge; y++)
		{
			for (int x = 0; x < chunkConfig.numberOfVerticesOnEdge; x++)
			{
				var uv = new Vector2(
					x / max,
					y / max
				);

				// ADJUST_UV;
				uv = (uv + new Vector2(0.25f, 0.25f)) / 1.5f;

				segmentUVs[i++] = uv;
			}
		}
		return segmentUVs;
	}
}