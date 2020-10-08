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
			public Chunk chunk;
		}

		List<ToGenerateChunk> toGenerateChunks = new List<ToGenerateChunk>();
		List<Chunk> toConsiderForSubdivision = new List<Chunk>();
		List<Chunk> toRenderChunks = new List<Chunk>();

		PointOfInterest fromPosition;
		float weightNeededToSubdivide;
		int subdivisionMaxRecurisonDepth;

		public int NumChunksToGenerate => toGenerateChunks.Count;
		public int NumChunksToRender => toRenderChunks.Count;

		public void Clear()
		{
			toGenerateChunks.Clear();
			toConsiderForSubdivision.Clear();
			toRenderChunks.Clear();
		}

		public Chunk GetNextChunkToGenerate()
		{
			if (toGenerateChunks.Count == 0) return null;
			int i = toGenerateChunks.Count - 1;
			var chunk = toGenerateChunks[i].chunk;
			toGenerateChunks.RemoveAt(i);
			return chunk;
		}

		public List<Chunk> GetChunksToRender()
		{
			return toRenderChunks;
		}

		public IEnumerator StartCoroutine(Planet planet, PointOfInterest fromPosition)
		{
			Phase_1_Start (planet, fromPosition);
			yield return null;
			while (Phase_2_Loop(100))
			{
				yield return null;
			}
			yield return null;
			Phase_3_Sort();
		}

		void Phase_1_Start(Planet planet, PointOfInterest fromPosition)
		{
			toGenerateChunks.Clear();
			toRenderChunks.Clear();
			toConsiderForSubdivision.Clear();

			toConsiderForSubdivision.AddRange(planet.rootChildren);

			this.fromPosition = fromPosition;
			weightNeededToSubdivide = planet.chunkConfig.weightNeededToSubdivide;
			subdivisionMaxRecurisonDepth = planet.SubdivisionMaxRecurisonDepth;
		}

		bool Phase_2_Loop(int step)
		{
			for (int i = toConsiderForSubdivision.Count - 1; i >= 0 && --step > 0; --i)
			{
				var chunk = toConsiderForSubdivision[i];
				toConsiderForSubdivision.RemoveAt(i);

				float weight = chunk.GetRelevanceWeight(fromPosition);

				if (weight > weightNeededToSubdivide && chunk.treeDepth < subdivisionMaxRecurisonDepth) // want subdivide ?
				{
					if (chunk.HasFullyGeneratedData) // children require generated data from parent
					{ 
						chunk.EnsureChildrenInstancesAreCreated();
					
						//toConsiderForSubdivision.AddRange(chunk.children);
						//i += chunk.children.Count;
						//continue;

						bool areAllChildrenGenerated = true;
						for (int j = 0; j < chunk.children.Count; ++j)
						{
							if (!chunk.children[j].HasFullyGeneratedData) 
							{
								areAllChildrenGenerated = false;
								break;
							}
						}

						if (areAllChildrenGenerated) // must show all children at once
						{
							toConsiderForSubdivision.AddRange(chunk.children);
							i += chunk.children.Count;

							if (chunk.WantsRefresh)
							{
								toGenerateChunks.Add(new ToGenerateChunk() { weight = weight * 0.1f, chunk = chunk });
							}
						}
						else
						{
							toRenderChunks.Add(chunk);
							if (chunk.WantsRefresh)
							{
								toGenerateChunks.Add(new ToGenerateChunk() { weight = weight * 2f, chunk = chunk });
							}

							for (int j = 0; j < chunk.children.Count; ++j)
							{
								toGenerateChunks.Add(new ToGenerateChunk() { weight = weight, chunk = chunk.children[j] });
							}
						}
					}
					else
					{
						toGenerateChunks.Add(new ToGenerateChunk() { weight = weight * 4, chunk = chunk });
					}
				}
				else
				{
					if (chunk.HasFullyGeneratedData) 
					{
						toRenderChunks.Add(chunk);
						if (chunk.WantsRefresh)
						{
							toGenerateChunks.Add(new ToGenerateChunk() { weight = weight * 2f, chunk = chunk });
						}
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
			toGenerateChunks.Sort((ToGenerateChunk a, ToGenerateChunk b) => b.weight.CompareTo(a.weight));
		}

	}

}