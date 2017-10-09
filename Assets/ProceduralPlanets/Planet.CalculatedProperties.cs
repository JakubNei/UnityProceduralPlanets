using UnityEngine;

public partial class Planet
{

	int subdivisionMaxRecurisonDepthCached = -1;
	public int SubdivisionMaxRecurisonDepth
	{
		get
		{
			if (subdivisionMaxRecurisonDepthCached == -1)
			{
				var planetCircumference = 2 * Mathf.PI * planetConfig.radiusStart;
				var oneRootChunkCircumference = planetCircumference / 6.0f;

				subdivisionMaxRecurisonDepthCached = 0;
				while (oneRootChunkCircumference > chunkConfig.stopSegmentRecursionAtWorldSize)
				{
					oneRootChunkCircumference /= 2;
					subdivisionMaxRecurisonDepthCached++;
				}
			}
			return subdivisionMaxRecurisonDepthCached;
		}
	}

}