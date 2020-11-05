using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralPlanets : MonoBehaviour
{
	HashSet<Planet> allPlanets = new HashSet<Planet>();

	public static ProceduralPlanets main { get; private set; }

	private void Awake()
	{
		main = this;
	}

	public void AddPlanet(Planet planet)
	{
		allPlanets.Add(planet);
	}

	public void RemovePlanet(Planet planet)
	{
		allPlanets.Remove(planet);
	}
    public Planet GetClosestPlanet(BigPosition position)
	{
		Planet closestPlanet = null;
		var closestDistance = double.MaxValue;
		foreach (var p in allPlanets)
		{
			var d = BigPosition.Distance(p.BigPosition, position) - p.planetConfig.radiusStart;
			if (d < closestDistance)
			{
				closestDistance = d;
				closestPlanet = p;
			}
		}
		return closestPlanet;
	}


	
	Queue<ChunkRenderer> chunkRenderersToReuse = new Queue<ChunkRenderer>();

	public ChunkRenderer GetFreeChunkRenderer()
	{
		if (chunkRenderersToReuse.Count > 0)
		{
			return chunkRenderersToReuse.Dequeue();
		}

		var r = ChunkRenderer.CreateNew();
		return r;
	}
	public void ReturnChunkRendererToPool(ChunkRenderer chunkRenderer)
	{
		chunkRenderer.Hide();
		chunkRenderersToReuse.Enqueue(chunkRenderer);
	}
}
