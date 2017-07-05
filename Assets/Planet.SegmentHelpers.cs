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
			 A
			 /\  top line
			/\/\
		   /\/\/\
		  /\/\/\/\ middle lines
		 /\/\/\/\/\
		/\/\/\/\/\/\ bottom line
	   B           C

		*/
		if (segmentIndicies != null) return segmentIndicies;

		var indicies = new List<int>();
		// make triangles indicies list
		{
			int lineStartIndex = 0;
			int nextLineStartIndex = 1;
			indicies.Add(0);
			indicies.Add(1);
			indicies.Add(2);

			int numberOfVerticesInBetween = 0;
			// we skip first triangle as it was done manually
			// we skip last row of vertices as there are no triangles under it
			for (int y = 1; y < chunkNumberOfVerticesOnEdge - 1; y++)
			{
				lineStartIndex = nextLineStartIndex;
				nextLineStartIndex = lineStartIndex + numberOfVerticesInBetween + 2;

				for (int x = 0; x <= numberOfVerticesInBetween + 1; x++)
				{
					indicies.Add(lineStartIndex + x);
					indicies.Add(nextLineStartIndex + x);
					indicies.Add(nextLineStartIndex + x + 1);

					if (x <= numberOfVerticesInBetween) // not a last triangle in line
					{
						indicies.Add(lineStartIndex + x);
						indicies.Add(nextLineStartIndex + x + 1);
						indicies.Add(lineStartIndex + x + 1);
					}
				}

				numberOfVerticesInBetween++;
			}
		}
		segmentIndicies = indicies.ToArray();
		return segmentIndicies;
	}

}