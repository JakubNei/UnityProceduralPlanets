using System;
using UnityEngine;

public class ChunkRenderer : MonoBehaviour
{
	public ChunkData chunk;

	public ChunkData.GeneratedData generatedData; // chunk may refresh, and create new generated data so lets keep alive the data we are showing

	Material material;
	MeshFilter meshFilter;
	MeshCollider meshCollider;
	FloatingOriginTransform floatingTransform;
	MeshRenderer meshRenderer;

	public static ChunkRenderer CreateNew()
	{
		var go = new GameObject(nameof(ChunkRenderer));
		var renderer = go.AddComponent<ChunkRenderer>();
		//go.hideFlags = HideFlags.HideInHierarchy; // so Hierarchy Windows in editor doesn't lag
		renderer.CreateComponents();
		return renderer;
	}

	void CreateComponents()
	{
		MyProfiler.BeginSample("Procedural Planet / ChunkRenderer / CreateComponents");

		meshFilter = gameObject.AddComponent<MeshFilter>();
		floatingTransform = gameObject.AddComponent<FloatingOriginTransform>();
		meshRenderer = gameObject.AddComponent<MeshRenderer>();
		meshCollider = gameObject.AddComponent<MeshCollider>();

		MyProfiler.EndSample();
	}

	public void RenderChunk(ChunkData chunk)
	{
		this.chunk = chunk;

		if (chunk.GenerateUsingPlanetGlobalPos)
			floatingTransform.BigPosition = chunk.planet.BigPosition;
		else
			floatingTransform.BigPosition = chunk.planet.BigPosition + chunk.bigPositionLocalToPlanet;

		this.transform.rotation = chunk.planet.transform.rotation;

		generatedData = chunk.FullyGeneratedData;

		meshFilter.sharedMesh = generatedData.mesh;
		if (meshCollider) meshCollider.sharedMesh = generatedData.mesh;

		if (material == null || material.shader != chunk.planet.chunkConfig.chunkMaterial.shader)
		{ 
			material = new Material(chunk.planet.chunkConfig.chunkMaterial);
			meshRenderer.sharedMaterial = material;
		}

		if (material && generatedData.chunkDiffuseMap) material.mainTexture = generatedData.chunkDiffuseMap;
		if (material && generatedData.chunkTangentNormalMap) material.SetTexture("_BumpMap", generatedData.chunkTangentNormalMap);
	}

	public void Hide()
	{
		generatedData = null;
		chunk = null;

		meshFilter.sharedMesh = null;
		if (meshCollider) meshCollider.sharedMesh = null;
	}

	#if UNITY_EDITOR
	private void OnDrawGizmosSelected()
	{
		if (gameObject && gameObject.activeSelf && chunk != null && chunk.planet != null)
		{
			Gizmos.color = Color.cyan;
			//rangePosRealSubdivided.DrawGizmos();
			chunk.rangePosToCalculateScreenSizeOn_localToPlanet.DrawGizmos(chunk.planet.transform.position);

			Vector3 unityCenter = chunk.rangePosToCalculateScreenSizeOn_localToPlanet.CenterPos + chunk.planet.transform.position;

			Gizmos.DrawSphere(unityCenter, 0.1f);

			UnityEditor.Handles.Label(unityCenter, 
				"weight " + chunk.debugLastGenerationWeight.ToString() + "\n" +
				"dist " + chunk.debugLastDistanceToCamera.ToString() + "\n" +
				"radius " + chunk.debugLastRadiusWorldSpace.ToString() + "\n" +
				"slopeModifier " + chunk.SlopeModifier.ToString() + "\n"
			);
		}
	}
	#endif
}