using System;
using UnityEngine;

public class ChunkRenderer : MonoBehaviour
{
	Planet planet;

	public Chunk chunk;

	Material material;
	MeshFilter meshFilter;
	MeshCollider meshCollider;

	public static ChunkRenderer CreateFor(Planet planet)
	{
		var go = new GameObject(nameof(ChunkRenderer));
		go.transform.parent = planet.transform;
		var renderer = go.AddComponent<ChunkRenderer>();
		renderer.planet = planet;
		renderer.CreateComponents();
		return renderer;
	}

	void CreateComponents()
	{
		MyProfiler.BeginSample("Procedural Planet / Create Components");

		meshFilter = gameObject.AddComponent<MeshFilter>();

		material = new Material(planet.chunkConfig.chunkMaterial);
		var meshRenderer = gameObject.AddComponent<MeshRenderer>();
		meshRenderer.sharedMaterial = material;

		if (planet.chunkConfig.createColliders)
		{
			MyProfiler.BeginSample("Procedural Planet / Create GameObject / Collider");
			meshCollider = gameObject.AddComponent<MeshCollider>();
	
			MyProfiler.EndSample();
		}

		MyProfiler.EndSample();
	}

	public void RenderChunk(Chunk chunk)
	{
		this.chunk = chunk;

		if (chunk.GenerateUsingPlanetGlobalPos)
			gameObject.transform.localPosition = Vector3.zero;
		else
			gameObject.transform.localPosition = chunk.offsetFromPlanetCenter;

		meshFilter.sharedMesh = chunk.generatedData.mesh;
		if (meshCollider) meshCollider.sharedMesh = chunk.generatedData.mesh;

		if (material && chunk.generatedData.chunkDiffuseMap) material.mainTexture = chunk.generatedData.chunkDiffuseMap;
		if (material && chunk.generatedData.chunkNormalMap) material.SetTexture("_BumpMap", chunk.generatedData.chunkNormalMap);
	
	}

	public void Hide()
	{
		meshFilter.sharedMesh = null;
		if (meshCollider) meshCollider.sharedMesh = null;
	}


	private void OnDrawGizmosSelected()
	{
		if (gameObject && gameObject.activeSelf && chunk != null)
		{
			Gizmos.color = Color.cyan;
			//rangePosRealSubdivided.DrawGizmos();
			chunk.rangePosToCalculateScreenSizeOn.DrawGizmos(planet.transform.position);
		}
	}

	

}