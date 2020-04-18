﻿using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public partial class Planet
{

	void GatherWeights(WeightedSegmentsList toGenerate, Chunk chunk, int recursionDepth)
	{
		if (toGenerate.Count > 100) return; // precaution

		var weight = chunk.GetGenerationWeight(toGenerate.data);

		if (!chunk.generationBegan)
		{
			toGenerate.Add(chunk, weight);
		}

		if (recursionDepth < SubdivisionMaxRecurisonDepth && weight > chunkConfig.weightNeededToSubdivide)
		{
			chunk.EnsureChildrenInstancesAreCreated();

			foreach (var child in chunk.children)
			{
				GatherWeights(toGenerate, child, recursionDepth + 1);
			}

			if (chunk.children.All(c => c.isGenerationDone))
			{
				chunk.SetVisible(false);
			}
			else
			{
				chunk.SetVisible(true);
				chunk.HideAllChildren();
			}
		}
		else if (chunk.isGenerationDone)
		{
			chunk.SetVisible(true);
			//chunk.HideAllChildren();
			chunk.DestroyAllChildren();
		}
	}


	class WeightedSegmentsList : Dictionary<Chunk, float>
	{
		public PointOfInterest data;

		public new void Add(Chunk chunk, float weight)
		{
			PrivateAdd1(chunk, weight);

			// we have to generate all our parents first
			while (chunk.parent != null && chunk.parent.generationBegan == false)
			{
				chunk = chunk.parent;
				var w = chunk.GetGenerationWeight(data);
				PrivateAdd1(chunk, Mathf.Max(w, weight));
			}
		}
		private void PrivateAdd1(Chunk chunk, float weight)
		{
			PrivateAdd2(chunk, weight);

			// if we want to show this chunk, our neighbours have the same weight, because we cant be shown without our neighbours
			if (chunk.parent != null)
			{
				foreach (var neighbour in chunk.parent.children)
				{
					if (neighbour.generationBegan == false)
					{
						var w = neighbour.GetGenerationWeight(data);
						PrivateAdd2(neighbour, Mathf.Max(w, weight));
					}
				}
			}
		}
		private void PrivateAdd2(Chunk chunk, float weight)
		{
			if (chunk.generationBegan) return;

			float w;
			if (this.TryGetValue(chunk, out w))
			{
				if (w > weight) return; // the weight already present is bigger, dont change it
			}

			this[chunk] = weight;
		}
		public IEnumerable<Chunk> GetWeighted(int maxCount = 100)
		{
			return this.OrderByDescending(i => i.Value).Take(maxCount).Select(i => i.Key);
		}
		public Chunk GetMostImportantChunk()
		{
			return this.OrderByDescending(i => i.Value).FirstOrDefault().Key;
		}
	}


	public struct PointOfInterest
	{
		public Vector3 pos;
		public float fieldOfView;
	}

	//Queue<Segment> toGenerateOrdered;
	WeightedSegmentsList toGenerate = new WeightedSegmentsList();
	public void CalculateChunksToGenerate(PointOfInterest data)
	{
		toGenerate.Clear();
		toGenerate.data = data;

		foreach (var chunk in rootChildren)
		{
			GatherWeights(toGenerate, chunk, 0);
		}

		//toGenerateOrdered = new Queue<Segment>(toGenerate.GetWeighted());
	}


}