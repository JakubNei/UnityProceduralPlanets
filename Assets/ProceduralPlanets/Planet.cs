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
		public float radiusStart = 1000;
		public float radiusHeightMapMultiplier = 30;
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
		public bool createColliders = true;
		public int textureResolution = 256; // must be multiplier of 16
		public bool rescaleToMinMax = true;

		public Material chunkMaterial;
		public ComputeShader generateChunkVertices;
		public ComputeShader generateChunkHeightMapPass1;
		public ComputeShader generateChunkHeightMapPass2;
		public ComputeShader generateChunkDiffuseMap;
		public ComputeShader generateChunkNormapMap;
		public ComputeShader generateSlopeAndCurvatureMap;

		public Texture2D grass;
		public Texture2D clay;
		public Texture2D rock;
	}
	public ChunkConfig chunkConfig;



	public ulong id;

	public List<Chunk> rootChildren;


	public static HashSet<Planet> allPlanets = new HashSet<Planet>();


	public ComputeBuffer chunkVertexGPUBuffer;
	public Vector3[] chunkVertexCPUBuffer;

	public Vector3 Center { get { return transform.position; } }

	void Start()
	{
		allPlanets.Add(this);
		GeneratePlanetData();
		InitializeRootChildren();


	}


	void GeneratePlanetData()
	{
		MyProfiler.BeginSample("Procedural Planet / Initialize planet & generate base height map");

		const int resolution = 2048;
		var height = planetConfig.planetHeightMap = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
		height.filterMode = FilterMode.Trilinear;
		height.wrapMode = TextureWrapMode.Repeat;
		height.enableRandomWrite = true;
		height.Create();

		planetConfig.generatePlanetHeightMap.SetTexture(0, "_planetHeightMap", height);
		planetConfig.generatePlanetHeightMap.Dispatch(0, height.width / 16, height.height / 16, 1);

		chunkVertexGPUBuffer = new ComputeBuffer(NumberOfVerticesNeededTotal, 3 * sizeof(float));
		chunkVertexCPUBuffer = new Vector3[NumberOfVerticesNeededTotal];

		MyProfiler.EndSample();
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.R))
		{
			MarkForRegeneration(rootChildren);
		}
	}

	static void MarkForRegeneration(IEnumerable<Chunk> chunks)
	{
		foreach (var c in chunks)
		{
			c.MarkForRegeneration();
			MarkForRegeneration(c.children);
		}
	}

	void LateUpdate()
	{
		MyProfiler.BeginSample("Procedural Planet / Calculate desired subdivision");
		TrySubdivideOver(new SubdivisionData()
		{
			pos = Camera.main.transform.position,
			fieldOfView = Camera.main.fieldOfView,
		});
		MyProfiler.EndSample();

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
		GUILayout.Button("chunks to generate: " + toGenerate.Count);
	}




	void AddRootChunk(ulong id, WorldPos[] corners, int a, int b, int c, int d)
	{
		var range = new Range()
		{
			a = corners[a],
			b = corners[b],
			c = corners[c],
			d = corners[d],
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

		/* 3----------0
		  /|         /|
		 / |        / |
		2----------1  |       y
		|  |       |  |       |
		|  7-------|--4       |  z
		| /        | /        | /
		|/         |/         |/
		6----------5          0----------x  */

		var corners = new[] {
			// top 4
			new WorldPos(1, 1, 1),
			new WorldPos(1, 1, -1),
			new WorldPos(-1, 1, -1),
			new WorldPos(-1, 1, 1),
			// bottom 4
			new WorldPos(1, -1, 1),
			new WorldPos(1, -1, -1),
			new WorldPos(-1, -1, -1),
			new WorldPos(-1, -1, 1)
		};

		AddRootChunk(0, corners, 0, 1, 2, 3); // top
		AddRootChunk(1, corners, 5, 4, 7, 6); // bottom
		AddRootChunk(2, corners, 1, 5, 6, 2); // front
		AddRootChunk(3, corners, 3, 7, 4, 0); // back
		AddRootChunk(4, corners, 0, 4, 5, 1); // right
		AddRootChunk(5, corners, 2, 6, 7, 3); // left
	}


	private void OnDrawGizmos()
	{
		if (rootChildren == null || rootChildren.Count == 0)
		{
			Gizmos.color = Color.blue;
			Gizmos.DrawSphere(this.transform.position, planetConfig.radiusStart);
		}
	}
}
