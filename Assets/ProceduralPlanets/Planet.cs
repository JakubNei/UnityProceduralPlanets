using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Events;

public partial class Planet : MonoBehaviour
{
	[System.Serializable]
	public class PlanetConfig
	{
		public Texture planetHeightMap;

		public ComputeShader generatePlanetHeightMapPass1;
		public ComputeShader generatePlanetHeightCalculateHumidity;
		public ComputeShader generatePlanetHeightMapPass2;
		public int generatedPlanetHeightMapResolution = 2048; // must be multiplier of 16

		public Texture2D biomesControlMap;
		public ComputeShader generatePlanetBiomesData;
		public float radiusStart = 1000; // earth is 6 371 000 m
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
		public bool createColliders = true;
		public int textureResolution = 256; // must be multiplier of 16
		public bool rescaleToMinMax = true;
		public int NumberOfVerticesNeededTotal { get { return numberOfVerticesOnEdge * numberOfVerticesOnEdge; } }

		public Material chunkMaterial;
		public ComputeShader generateChunkVertices;
		public ComputeShader generateChunkHeightMapPass1;
		public ComputeShader generateChunkHeightMapPass2;
		public ComputeShader generateChunkDiffuseMap;
		public ComputeShader generateChunkNormapMap;
		public ComputeShader generateSlopeAndCurvatureMap;
		public ComputeShader generateChunkBiomesMap;

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

	void Awake()
	{
		allPlanets.Add(this);
		InitializeOther();
		InitializeRootChildren();
		GeneratePlanetData();
	}
	void InitializeOther()
	{
		//chunkVertexGPUBuffer = new ComputeBuffer(chunkConfig.NumberOfVerticesNeededTotal, 3 * sizeof(float));
		chunkVertexCPUBuffer = new Vector3[chunkConfig.NumberOfVerticesNeededTotal];
	}

	void GeneratePlanetData()
	{
		MyProfiler.BeginSample("Procedural Planet / generate base height map");

		var heightMap = new RenderTexture(planetConfig.generatedPlanetHeightMapResolution, planetConfig.generatedPlanetHeightMapResolution, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
		heightMap.filterMode = FilterMode.Trilinear;
		heightMap.wrapMode = TextureWrapMode.Repeat;
		heightMap.enableRandomWrite = true;
		heightMap.Create();
		planetConfig.planetHeightMap = heightMap;

		var heightMapTemp = RenderTexture.GetTemporary(heightMap.descriptor);
		heightMapTemp.filterMode = FilterMode.Trilinear;
		heightMapTemp.wrapMode = TextureWrapMode.Repeat;
		heightMapTemp.enableRandomWrite = true;
		heightMapTemp.Create();

		planetConfig.generatePlanetHeightMapPass1.SetTexture(0, "_planetHeightMap", heightMapTemp);
		planetConfig.generatePlanetHeightMapPass1.Dispatch(0, planetConfig.planetHeightMap.width / 16, planetConfig.planetHeightMap.height / 16, 1);

		planetConfig.generatePlanetHeightMapPass2.SetTexture(0, "_planetHeightMapIn", heightMapTemp);
		planetConfig.generatePlanetHeightMapPass2.SetTexture(0, "_planetHeightMapOut", planetConfig.planetHeightMap);
		planetConfig.generatePlanetHeightMapPass2.Dispatch(0, planetConfig.planetHeightMap.width / 16, planetConfig.planetHeightMap.height / 16, 1);

		RenderTexture.ReleaseTemporary(heightMapTemp);

		MyProfiler.EndSample();
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.R))
		{
			GeneratePlanetData();
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

	Queue<ChunkRenderer> chunkRenderersToReuse = new Queue<ChunkRenderer>();

	ChunkRenderer GetFreeChunkRenderer()
	{
		if (chunkRenderersToReuse.Count > 0)
		{
			return chunkRenderersToReuse.Dequeue();
		}

		return ChunkRenderer.CreateFor(this);
	}
	void ReturnChunkRendererToPool(ChunkRenderer chunkRenderer)
	{
		chunkRenderer.Hide();
		chunkRenderersToReuse.Enqueue(chunkRenderer);
	}

	Dictionary<Chunk, ChunkRenderer> chunksBeingRendered = new Dictionary<Chunk, ChunkRenderer>();
	List<Chunk> toStopRendering = new List<Chunk>();
	List<Chunk> toStartRendering = new List<Chunk>();

	ChunkDivisionCalculation subdivisonCalculationInProgress = new ChunkDivisionCalculation();
	ChunkDivisionCalculation subdivisonCalculationLast = new ChunkDivisionCalculation();

	IEnumerator chunkGenerationCoroutine;
	IEnumerator subdivisonCalculationCoroutine;

	void LateUpdate()
	{
		var milisecondsBudget = (int)(Time.deltaTime * 1000f - 1000f / 90f);

		var sw = Stopwatch.StartNew();

		MyProfiler.BeginSample("Procedural Planet / Generate chunks coroutine execution");

		do
		{
			if (chunkGenerationCoroutine == null)
			{
				Chunk chunk = subdivisonCalculationLast.GetNextChunkToGenerate();
				if (chunk == null) break;
				if (!chunk.generationBegan) chunkGenerationCoroutine = chunk.StartGenerateCoroutine();
			}

			if (chunkGenerationCoroutine != null)
			{
				if (chunkGenerationCoroutine.MoveNext())
				{
					if (chunkGenerationCoroutine.Current is WaitForEndOfFrame)
					{
						break;
					}
				}
				else
				{
					chunkGenerationCoroutine = null;
				}
			}
		} while (sw.ElapsedMilliseconds < milisecondsBudget);

		MyProfiler.EndSample();



		MyProfiler.BeginSample("Procedural Planet / Calculate desired subdivision");

		var pointOfInterest = new PointOfInterest()
		{
			pos = Camera.main.transform.position,
			fieldOfView = Camera.main.fieldOfView,
		};

		if (subdivisonCalculationCoroutine == null)
		{
			subdivisonCalculationCoroutine = subdivisonCalculationInProgress.StartCoroutine(this, pointOfInterest);
		}
		else if (!subdivisonCalculationCoroutine.MoveNext())
		{
			subdivisonCalculationCoroutine = null;
			var temp = subdivisonCalculationInProgress;
			subdivisonCalculationInProgress = subdivisonCalculationLast;
			subdivisonCalculationLast = temp;
		}

		MyProfiler.EndSample();




		MyProfiler.BeginSample("Procedural Planet / Update ChunkRenderers");

		var toRenderChunks = subdivisonCalculationLast.GetChunksToRender();
		toStopRendering.Clear();
		foreach (var kvp in chunksBeingRendered)
		{
			if (!toRenderChunks.Contains(kvp.Key)) toStopRendering.Add(kvp.Key);
		}

		foreach (var chunk in toStopRendering)
		{
			ReturnChunkRendererToPool(chunksBeingRendered[chunk]);
			chunksBeingRendered.Remove(chunk);
		}

		toStartRendering.Clear();
		foreach (var chunk in toRenderChunks)
		{
			if (!chunksBeingRendered.ContainsKey(chunk)) toStartRendering.Add(chunk);
		}

		foreach (var chunk in toStartRendering)
		{
			var renderer = GetFreeChunkRenderer();
			renderer.RenderChunk(chunk);
			chunksBeingRendered.Add(chunk, renderer);
		}

		MyProfiler.EndSample();

	}

	private void OnGUI()
	{
		//GUILayout.Button("chunks to generate: " + toGenerateChunks.Count);
		//GUILayout.Button("chunks to render: " + toRenderChunks.Count);
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
			treeDepth: 0,
			range: range,
			id: id
		);

		rootChildren.Add(child);
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
