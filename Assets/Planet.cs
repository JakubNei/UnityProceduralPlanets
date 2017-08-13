using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public partial class Planet : MonoBehaviour
{
	[System.Serializable]
	public class PlanetConfig
	{
		public RenderTexture planetHeightMap;
		public ComputeShader generatePlanetHeightMap;

		public Texture2D biomesControlMap;
		public ComputeShader generatePlanetBiomesData;
		public float radiusMin = 1000;
		public float radiusVariation = 30;
		public float seaLevel01 = 0.5f;
	}
	public PlanetConfig planetConfig;

	[System.Serializable]
	public class ChunkConfig
	{
		public bool useSkirts = false;
		public int numberOfVerticesOnEdge = 20;
		public float weightNeededToSubdivide = 0.70f;
		public float stopSegmentRecursionAtWorldSize = 10;
		public float destroyGameObjectIfNotVisibleForSeconds = 5;

		public Material chunkMaterial;
		public ComputeShader generateChunkVertices;
		public ComputeShader generateChunkHeightMap;
		public ComputeShader generateChunkDiffuseMap;
		public ComputeShader generateChunkNormapMap;
	}
	public ChunkConfig chunkConfig;



	public ulong id;

	public List<Chunk> rootChildren;


	public static HashSet<Planet> allPlanets = new HashSet<Planet>();

	public Vector3 Center { get { return transform.position; } }


	void Start()
	{
		allPlanets.Add(this);
		GeneratePlanetData();
		InitializeRootChildren();
	}

	void GeneratePlanetData()
	{
		var height = planetConfig.planetHeightMap = new RenderTexture(16 * 16, 16 * 16, 1, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
		height.depth = 0;
		height.enableRandomWrite = true;
		height.Create();

		planetConfig.generatePlanetHeightMap.SetTexture(0, "_planetHeightMap", height);
		planetConfig.generatePlanetHeightMap.Dispatch(0, height.width / 16, height.height / 16, 1);
	}


	void LateUpdate()
	{
		TrySubdivideOver(new SubdivisionData()
		{
			pos = Camera.main.transform.position,
			fieldOfView = Camera.main.fieldOfView,
		});

		var sw = Stopwatch.StartNew();
		foreach (var s in toGenerate.GetWeighted())
		{
			s.Generate();
			//if (sw.Elapsed.TotalMilliseconds > 5f)
			break;
		}
	}

	private void OnGUI()
	{
		GUILayout.Button("segments to generate: " + toGenerate.Count);
	}




	void AddRootChunk(ulong id, Vector3[] vertices, int a, int b, int c, int d)
	{
		var range = new Range()
		{
			a = vertices[a],
			b = vertices[b],
			c = vertices[c],
			d = vertices[d],
		};

		var child = Chunk.Create(
			planet: this,
			parent: null,
			generation: 0,
			range: range,
			id: id
		);

		rootChildren.Add(child);
	}

	private void InitializeRootChildren()
	{
		if (rootChildren != null && rootChildren.Count > 0) return;
		if (rootChildren == null)
			rootChildren = new List<Chunk>(6);

		var indicies = new List<uint>();

		var halfSIze = Mathf.Sqrt((planetConfig.radiusMin * planetConfig.radiusMin) / 3.0f);

		/* 3----------0
		  /|         /|
		 / |        / |
		2----------1  |       y
		|  |       |  |       |
		|  7-------|--4       |  z
		| /        | /        | /
		|/         |/         |/
		6----------5          0----------x  */

		var vertices = new[] {
			// top 4
			new Vector3(halfSIze, halfSIze, halfSIze),
			new Vector3(halfSIze, halfSIze, -halfSIze),
			new Vector3(-halfSIze, halfSIze, -halfSIze),
			new Vector3(-halfSIze, halfSIze, halfSIze),
			// bottom 4
			new Vector3(halfSIze, -halfSIze, halfSIze),
			new Vector3(halfSIze, -halfSIze, -halfSIze),
			new Vector3(-halfSIze, -halfSIze, -halfSIze),
			new Vector3(-halfSIze, -halfSIze, halfSIze)
		};

		AddRootChunk(0, vertices, 0, 1, 2, 3); // top
		AddRootChunk(1, vertices, 5, 4, 7, 6); // bottom
		AddRootChunk(2, vertices, 1, 5, 6, 2); // front
		AddRootChunk(3, vertices, 3, 7, 4, 0); // back
		AddRootChunk(4, vertices, 0, 4, 5, 1); // right
		AddRootChunk(5, vertices, 2, 6, 7, 3); // left
	}


	private void OnDrawGizmos()
	{
		if (rootChildren == null || rootChildren.Count == 0)
		{
			Gizmos.color = Color.blue;
			Gizmos.DrawSphere(this.transform.position, planetConfig.radiusMin);
		}
	}
}
