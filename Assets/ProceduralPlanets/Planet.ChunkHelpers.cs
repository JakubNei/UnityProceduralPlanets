using UnityEngine;
using System.Collections.Generic;


public partial class Planet
{

	Vector3[] chunkVertices;
	public Vector3[] GetChunkVertices()
	{
		if (chunkVertices != null) return chunkVertices;

		chunkVertices = new Vector3[chunkConfig.NumberOfVerticesNeededTotal];

		// DEBUG
		for (int i = 0; i < chunkVertices.Length; i++)
			chunkVertices[i] = new Vector3(i % 5, i, 0);


		return chunkVertices;
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

		var indicies = new List<int>(chunkConfig.NumberOfVerticesNeededTotal);

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


	Vector2[] chunkUVs;
	public Vector2[] GetChunkUVs()
	{
		if (chunkUVs != null) return chunkUVs;

		chunkUVs = new Vector2[chunkConfig.NumberOfVerticesNeededTotal];
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
				chunkUVs[i++] = uv;
			}
		}
		return chunkUVs;
	}
}