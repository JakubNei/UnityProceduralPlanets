using UnityEngine;
using System.Collections.Generic;


public partial class Planet
{

	Vector3[] chunkMeshVertices;
	public Vector3[] GetChunkMeshVertices()
	{
		if (chunkMeshVertices != null) return chunkMeshVertices;

		chunkMeshVertices = new Vector3[chunkConfig.NumberOfVerticesNeededTotal];

		// DEBUG
		for (int i = 0; i < chunkMeshVertices.Length; i++)
			chunkMeshVertices[i] = new Vector3(i % 5, i, 0);


		return chunkMeshVertices;
	}


	int[] chunkMeshTriangles;
	public int[] GetChunkMeshTriangles()
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

		if (chunkMeshTriangles != null) return chunkMeshTriangles;

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


		chunkMeshTriangles = indicies.ToArray();
		return chunkMeshTriangles;
	}

	HashSet<int> chunkMeshIndiciesEdge;
	public HashSet<int> GetChunkMeshIndiciesEdge()
	{
		if (chunkMeshIndiciesEdge != null) return chunkMeshIndiciesEdge;
		chunkMeshIndiciesEdge = new HashSet<int>();

		int verticesOnEdge = chunkConfig.numberOfVerticesOnEdge;

		for (int i = 0; i < verticesOnEdge; i++)
		{
			chunkMeshIndiciesEdge.Add(i); // top ling
			chunkMeshIndiciesEdge.Add(verticesOnEdge * (verticesOnEdge - 1) + i); // bottom line
		}
		for (int i = 1; i < verticesOnEdge - 1; i++)
		{
			chunkMeshIndiciesEdge.Add(verticesOnEdge * i); // left line
			chunkMeshIndiciesEdge.Add(verticesOnEdge * i + verticesOnEdge - 1); // right line
		}

		return chunkMeshIndiciesEdge;
	}


	Vector2[] chunkMeshUVs;
	public Vector2[] GetChunkMeshUVs()
	{
		if (chunkMeshUVs != null) return chunkMeshUVs;

		chunkMeshUVs = new Vector2[chunkConfig.NumberOfVerticesNeededTotal];
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
				chunkMeshUVs[i++] = uv;
			}
		}
		return chunkMeshUVs;
	}
}