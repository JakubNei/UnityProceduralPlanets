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

		public Material chunkMaterial;
		public ComputeShader generateChunkVertices;
		public ComputeShader generateChunkHeightMapPass1;
		public ComputeShader generateChunkHeightMapPass2;
		public ComputeShader generateChunkDiffuseMap;
		public ComputeShader generateChunkNormapMap;

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


	public Shader renderNormalsToTexture;
	Camera renderToTextureCamera;
	public void RenderNormalsToTexture(GameObject toRender, RenderTexture target)
	{
		int layer = 20;
		int cullingMask = 1 << layer;
		if (renderToTextureCamera == null)
		{
			var cameraHolder = new GameObject("render to texture camera holder");
			cameraHolder.transform.parent = gameObject.transform;
			renderToTextureCamera = cameraHolder.AddComponent<Camera>();
			renderToTextureCamera.enabled = false;
			renderToTextureCamera.renderingPath = RenderingPath.Forward;
			renderToTextureCamera.cullingMask = cullingMask;
			renderToTextureCamera.useOcclusionCulling = false;
			renderToTextureCamera.clearFlags = CameraClearFlags.Color;
			renderToTextureCamera.depthTextureMode = DepthTextureMode.None;
			renderToTextureCamera.backgroundColor = new Color(0.5f, 0.5f, 1);

			cameraHolder.transform.position = new Vector3(0, 0, -3001);
			renderToTextureCamera.farClipPlane = 10000f;
		}
		var originalLayer = toRender.layer;
		toRender.layer = layer;

		renderToTextureCamera.pixelRect = new Rect(0, 0, target.width, target.height);
		renderToTextureCamera.targetTexture = target;
		renderToTextureCamera.transform.LookAt(toRender.transform);
		renderToTextureCamera.RenderWithShader(renderNormalsToTexture, string.Empty);

		toRender.layer = originalLayer;
	}

	void GeneratePlanetData()
	{
		const int resolution = 1024;
		var height = planetConfig.planetHeightMap = new RenderTexture(resolution, resolution, 1, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
		height.depth = 0;
		height.enableRandomWrite = true;
		height.Create();

		planetConfig.generatePlanetHeightMap.SetTexture(0, "_planetHeightMap", height);
		planetConfig.generatePlanetHeightMap.Dispatch(0, height.width / 16, height.height / 16, 1);

		chunkVertexGPUBuffer = new ComputeBuffer(NumberOfVerticesNeededTotal, 3 * sizeof(float));
		chunkVertexCPUBuffer = new Vector3[NumberOfVerticesNeededTotal];
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
		GUILayout.Button("chunks to generate: " + toGenerate.Count);
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

		var halfSIze = Mathf.Sqrt((planetConfig.radiusStart * planetConfig.radiusStart) / 3.0f);

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
			Gizmos.DrawSphere(this.transform.position, planetConfig.radiusStart);
		}
	}
}
