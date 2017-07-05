using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public partial class Planet
{
	void GatherWeights(WeightedSegmentsList toGenerate, Segment segment, int recursionDepth)
	{
		//if (toGenerate.Count > 500) return; // SAFE


		var weight = segment.GetGenerationWeight(toGenerate.data);

		if (segment.generationBegan == false)
		{
			toGenerate.Add(segment, weight);
		}

		if (recursionDepth < SubdivisionMaxRecurisonDepth)
		{
			if (weight > WeightNeededToSubdivide)
			{
				segment.EnsureChildrenInstancesAreCreated();

				foreach (var child in segment.children)
				{
					GatherWeights(toGenerate, child, recursionDepth + 1);
				}

				if (segment.children.All(c => c.isGenerationDone))
				{
					segment.SetVisible(false);
				}
				else
				{
					segment.SetVisible(true);
					segment.HideAllChildren();
				}

				return;
			}
		}

		if (segment.isGenerationDone)
		{
			segment.SetVisible(true);
			segment.HideAllChildren();
		}
	}


	class WeightedSegmentsList : Dictionary<Segment, float>
	{
		public SubdivisionData data;

		public new void Add(Segment segment, float weight)
		{
			PrivateAdd1(segment, weight);

			// we have to generate all our parents first
			while (segment.parent != null && segment.parent.generationBegan == false)
			{
				segment = segment.parent;
				var w = segment.GetGenerationWeight(data);
				PrivateAdd1(segment, Mathf.Max(w, weight));
			}
		}
		private void PrivateAdd1(Segment segment, float weight)
		{
			PrivateAdd2(segment, weight);

			// if we want to show this chunk, our neighbours have the same weight, because we cant be shown without our neighbours
			if (segment.parent != null)
			{
				foreach (var neighbour in segment.parent.children)
				{
					if (neighbour.generationBegan == false)
					{
						var w = neighbour.GetGenerationWeight(data);
						PrivateAdd2(neighbour, Mathf.Max(w, weight));
					}
				}
			}
		}
		private void PrivateAdd2(Segment segment, float weight)
		{
			if (segment.generationBegan) return;

			float w;
			if (this.TryGetValue(segment, out w))
			{
				if (w > weight) return; // the weight already present is bigger, dont change it
			}

			this[segment] = weight;
		}
		public IEnumerable<Segment> GetWeighted(int maxCount = 100)
		{
			return this.OrderByDescending(i => i.Value).Take(maxCount).Select(i => i.Key);
		}
		public Segment GetMostImportantChunk()
		{
			return this.OrderByDescending(i => i.Value).FirstOrDefault().Key;
		}
	}


	public struct SubdivisionData
	{
		public Vector3 pos;
		public float fieldOfView;
	}

	//Queue<Segment> toGenerateOrdered;
	WeightedSegmentsList toGenerate = new WeightedSegmentsList();
	public void TrySubdivideOver(SubdivisionData data)
	{
		toGenerate.Clear();
		toGenerate.data = data;


		foreach (var rootSegment in rootSegments)
		{
			if (rootSegment.generationBegan == false)
			{
				// first generate rootCunks
				toGenerate.Add(rootSegment, float.MaxValue);
			}
			else
			{
				// then their children
				GatherWeights(toGenerate, rootSegment, 0);
			}
		}

		//toGenerateOrdered = new Queue<Segment>(toGenerate.GetWeighted());
	}


}