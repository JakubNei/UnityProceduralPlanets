using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Collections;
using UnityEngine.UI;

public partial class Planet
{
	public struct PointOfInterest
	{
		public BigPosition pos;
		public float fieldOfView;
	}


	class ChunkDivisionCalculation
	{
		struct ToGenerateChunk
		{
			public float weight;
			public ChunkData chunk;
		}

		List<ToGenerateChunk> toGenerateChunks = new List<ToGenerateChunk>();
		List<ChunkData> toConsiderForSubdivision = new List<ChunkData>();

		struct ToRenderChunks
		{
			public float weight;
			public ChunkData chunk;
		}
		List<ToRenderChunks> toRenderChunks = new List<ToRenderChunks>();

		PointOfInterest fromPosition;
		float weightNeededToSubdivide;
		int subdivisionMaxRecurisonDepth;
		int maxChunksToRender = 500;

		public int NumChunksToGenerate => toGenerateChunks.Count;
		public int NumChunksToRender => toRenderChunks.Count;

		public void Clear()
		{
			toGenerateChunks.Clear();
			toConsiderForSubdivision.Clear();
			toRenderChunks.Clear();
		}

		public ChunkData GetNextChunkToStartGeneration()
		{
			if (toGenerateChunks.Count == 0) return null;
			int i = toGenerateChunks.Count - 1;
			var chunk = toGenerateChunks[i].chunk;
			toGenerateChunks.RemoveAt(i);
			return chunk;
		}

		public IEnumerable<ChunkData> GetChunksToRender()
		{
			return toRenderChunks.Select(c => c.chunk);
		}

		public IEnumerator StartCoroutine(Planet planet, PointOfInterest fromPosition)
		{
			Phase_1_Start(planet, fromPosition);
			yield return null;
			while (Phase_2_Loop(100))
			{
				yield return null;
			}
			yield return null;
			Phase_3_Sort();
			yield return null;
			Phase_4_Sort();
		}

		void Phase_1_Start(Planet planet, PointOfInterest fromPosition)
		{
			Clear();
			toConsiderForSubdivision.AddRange(planet.rootChildren);

			this.fromPosition = fromPosition;
			weightNeededToSubdivide = planet.chunkConfig.weightNeededToSubdivide;
			subdivisionMaxRecurisonDepth = planet.SubdivisionMaxRecurisonDepth;
			maxChunksToRender = planet.chunkConfig.maxChunksToRender;
		}

		bool Phase_2_Loop(int step)
		{
			for (int i = toConsiderForSubdivision.Count - 1; i >= 0 && --step > 0; --i)
			{
				var chunk = toConsiderForSubdivision[i];
				toConsiderForSubdivision.RemoveAt(i);

				float weight = chunk.GetRelevanceWeight(fromPosition);

				if (chunk.WantsRefresh)
				{
					toGenerateChunks.Add(new ToGenerateChunk() { weight = weight * 2f, chunk = chunk });
				}

				if (weight > weightNeededToSubdivide && chunk.treeDepth < subdivisionMaxRecurisonDepth) // want subdivide ?
				{
					chunk.EnsureChildrenInstancesAreCreated();
					toConsiderForSubdivision.AddRange(chunk.children);
					i += chunk.children.Count;
				}
				else
				{
					if (chunk.HasFullyGeneratedData)
					{
						toRenderChunks.Add(new ToRenderChunks() { weight = weight, chunk = chunk });
					}
					else
					{
						toGenerateChunks.Add(new ToGenerateChunk() { weight = weight, chunk = chunk });
					}
				}
			}

			return toConsiderForSubdivision.Count > 0;
		}

		void Phase_3_Sort()
		{
			// bigger weight is first
			toGenerateChunks.Sort((ToGenerateChunk a, ToGenerateChunk b) => b.weight.CompareTo(a.weight));
		}

		void Phase_4_Sort()
		{
			// bigger weight is first
			toRenderChunks.Sort((ToRenderChunks a, ToRenderChunks b) => b.weight.CompareTo(a.weight));

			if (toRenderChunks.Count > maxChunksToRender)
			{ 
				toRenderChunks.RemoveRange(maxChunksToRender, toRenderChunks.Count - maxChunksToRender);
			}
		}

	}

}