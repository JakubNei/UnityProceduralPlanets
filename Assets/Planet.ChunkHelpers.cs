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
			for (int y = 1; y < numberOfVerticesOnEdge - 1; y++)
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


	Vector2[] segmentUVs;
	public Vector2[] GetSefgmentUVs()
	{
		if (segmentUVs != null) return segmentUVs;

		segmentUVs = new Vector2[NumberOfVerticesNeededTotal];
		int i = 0;


		/*
			 A = 0,0 = black
			 /\  top line
			/\/\
		   /\/\/\
		  /\/\/\/\ middle lines
		 /\/\/\/\/\
		/\/\/\/\/\/\ bottom line
	   B            C = 1,0 = red
	     = 1,1 = yellow
		*/

		segmentUVs[i++] = new Vector2(0, 0);

		int numberOfVerticesInBetween = 0;
		// we skip first vertex as it was done manually
		for (int y = 1; y < numberOfVerticesOnEdge; y++)
		{
			for (int x = 0; x < numberOfVerticesInBetween + 2; x++)
			{
				segmentUVs[i++] = new Vector2(
					((float)x) / numberOfVerticesOnEdge,
					((float)y) / numberOfVerticesOnEdge
				);
			}
			numberOfVerticesInBetween++;
		}

		return segmentUVs;
	}
}