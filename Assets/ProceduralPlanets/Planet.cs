using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Events;

public partial class Planet : MonoBehaviour, IDisposable
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
		public float weightNeededToSubdivide = 0.6f;
		public float stopSegmentRecursionAtWorldSize = 10;
		public bool createColliders = true;
		public int textureResolution = 512; // must be multiplier of 16
		public bool rescaleToMinMax = false;
		public bool generateUsingPlanetGlobalPos = false;
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


	public BigPosition BigPosition => new BigPosition(Vector3.zero);
	public Vector3 Center { get { return transform.position; } }

	void Awake()
	{
		allPlanets.Add(this);
		InitializeRootChildren();
		GeneratePlanetData();
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
			ReGeneratePlanet();
		}
	}

	void ReGeneratePlanet()
	{
		GeneratePlanetData();
		ResetChunkRenderers();

		var allChunks = CollectAllChunks();
		foreach (var chunk in allChunks)
		{
			chunk.MarkForReGeneration();
		}
	}


	Queue<ChunkRenderer> chunkRenderersToReuse = new Queue<ChunkRenderer>();

	void ResetChunkRenderers()
	{
		foreach (var kvp in chunksBeingRendered)
		{
			ReturnChunkRendererToPool(kvp.Value);
		}

		chunksBeingRendered.Clear();
	}

	ChunkRenderer GetFreeChunkRenderer()
	{
		if (chunkRenderersToReuse.Count > 0)
		{
			return chunkRenderersToReuse.Dequeue();
		}

		var r = ChunkRenderer.CreateFor(this);
		return r;
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

	IEnumerator subdivisonCalculationCoroutine;

	void LateUpdate()
	{
		var frameStart = Stopwatch.StartNew();

		var milisecondsBudget = (int)(Time.deltaTime * 1000f - 1000f / 90f);
		milisecondsBudget = 2;


		var pointOfInterest = new PointOfInterest()
		{
			pos = FloatingOriginController.Instance.BigPosition,
			fieldOfView = Camera.main.fieldOfView,
		};

		bool shouldUpdateRenderers = false;

		MyProfiler.BeginSample("Procedural Planet / Calculate desired subdivision");
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
			shouldUpdateRenderers = true;
		}
		MyProfiler.EndSample();

		if (shouldUpdateRenderers)
		{
			UpdateChunkRenderers(frameStart, milisecondsBudget);
		}


		GenerateChunks(frameStart, milisecondsBudget);
	}



	List<IEnumerator> chunkGenerationCoroutines = new List<IEnumerator>();
	private void GenerateChunks(Stopwatch frameStart, int milisecondsBudget)
	{
		MyProfiler.BeginSample("Procedural Planet / Generate chunks / coroutines execution");

		int couroutinesExecuted = 0;

		bool shouldStartNext = chunkGenerationCoroutines.Count == 0;
		do
		{
			if (shouldStartNext)
			{
				while (subdivisonCalculationLast.NumChunksToGenerate > 0)
				{
					Chunk chunk = subdivisonCalculationLast.GetNextChunkToGenerate();
					if (chunk == null) continue;
					if (chunk.generationBegan) continue;
					chunkGenerationCoroutines.Add(chunk.StartGenerateCoroutine());
				};
			}

			if (chunkGenerationCoroutines.Count == 0) break;

			int numWaitingForEndOfFrame = 0;
			for (int i = chunkGenerationCoroutines.Count - 1; i >= 0; --i)
			{
				var c = chunkGenerationCoroutines[i];
				if (c.MoveNext()) // coroutine execution
				{
					if (c.Current is WaitForEndOfFrame)
					{
						++numWaitingForEndOfFrame;
					}
					else
					{
						++couroutinesExecuted;
					}
				}
				else
				{
					// finished
					chunkGenerationCoroutines.RemoveAt(i);
				}

				if (frameStart.ElapsedMilliseconds > milisecondsBudget) break;
			}

			shouldStartNext = numWaitingForEndOfFrame >= chunkGenerationCoroutines.Count;

			shouldStartNext = shouldStartNext && chunkGenerationCoroutines.Count < 5;

		} while (frameStart.ElapsedMilliseconds < milisecondsBudget);

		MyProfiler.EndSample();

		MyProfiler.AddAvergaNumberSample("Procedural Planet / Generate chunks / coroutines executed", couroutinesExecuted);
		MyProfiler.AddAvergaNumberSample("Procedural Planet / Generate chunks / concurent coroutines", chunkGenerationCoroutines.Count);
		MyProfiler.AddAvergaNumberSample("Procedural Planet / Generate chunks / to generate", subdivisonCalculationLast.NumChunksToGenerate);
	}


	HashSet<Chunk> toRenderChunks = new HashSet<Chunk>();

	private void UpdateChunkRenderers(Stopwatch frameStart, int milisecondsBudget)
	{
		MyProfiler.BeginSample("Procedural Planet / Update ChunkRenderers");

		MyProfiler.BeginSample("Procedural Planet / Update ChunkRenderers / Calculations 1");
		toRenderChunks.Clear();
		toRenderChunks.UnionWith(subdivisonCalculationLast.GetChunksToRender());
		toStopRendering.Clear();
		foreach (var kvp in chunksBeingRendered)
		{
			if (!toRenderChunks.Contains(kvp.Key)) toStopRendering.Add(kvp.Key);
		}
		MyProfiler.EndSample();

		MyProfiler.BeginSample("Procedural Planet / Update ChunkRenderers / Return to pool");
		foreach (var chunk in toStopRendering)
		{
			ReturnChunkRendererToPool(chunksBeingRendered[chunk]);
			chunksBeingRendered.Remove(chunk);
		}
		MyProfiler.EndSample();

		MyProfiler.BeginSample("Procedural Planet / Update ChunkRenderers / Calculations 2");
		toStartRendering.Clear();
		foreach (var chunk in toRenderChunks)
		{
			if (!chunksBeingRendered.ContainsKey(chunk)) toStartRendering.Add(chunk);
		}
		MyProfiler.EndSample();

		MyProfiler.AddAvergaNumberSample("Procedural Planet / Update ChunkRenderers / to start rendering", toStartRendering.Count);
		MyProfiler.AddAvergaNumberSample("Procedural Planet / Update ChunkRenderers / to render", toRenderChunks.Count);

		var freeRenderers = new Stack<ChunkRenderer>();
		MyProfiler.BeginSample("Procedural Planet / Update ChunkRenderers / toStartRendering / get free renderers");
		foreach (var chunk in toStartRendering)
		{
			var renderer = GetFreeChunkRenderer();
			freeRenderers.Push(renderer);
		}
		MyProfiler.EndSample();

		MyProfiler.BeginSample("Procedural Planet / Update ChunkRenderers / toStartRendering / RenderChunk");
		foreach (var chunk in toStartRendering)
		{
			var renderer = freeRenderers.Pop();
			renderer.RenderChunk(chunk);
			chunksBeingRendered.Add(chunk, renderer);
			//if (frameStart.ElapsedMilliseconds > milisecondsBudget) break;
		}
		MyProfiler.EndSample();

		MyProfiler.EndSample();
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
			new BigPosition(1, 1, 1),
			new BigPosition(1, 1, -1),
			new BigPosition(-1, 1, -1),
			new BigPosition(-1, 1, 1),
			// bottom 4
			new BigPosition(1, -1, 1),
			new BigPosition(1, -1, -1),
			new BigPosition(-1, -1, -1),
			new BigPosition(-1, -1, 1)
		};

		AddRootChunk(0, corners, 0, 1, 2, 3); // top
		AddRootChunk(1, corners, 5, 4, 7, 6); // bottom
		AddRootChunk(2, corners, 1, 5, 6, 2); // front
		AddRootChunk(3, corners, 3, 7, 4, 0); // back
		AddRootChunk(4, corners, 0, 4, 5, 1); // right
		AddRootChunk(5, corners, 2, 6, 7, 3); // left
	}
	void AddRootChunk(ulong id, BigPosition[] corners, int a, int b, int c, int d)
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

	void OnDestroy()
	{
		Dispose();
	}

	List<Chunk> CollectAllChunks()
	{
		var allChunks = new List<Chunk>();

		var toProcess = new List<Chunk>(rootChildren);
		for (int i = toProcess.Count - 1; i >= 0; --i)
		{
			var c = toProcess[i];
			toProcess.RemoveAt(i);
			allChunks.Add(c);

			toProcess.AddRange(c.children);
			i += c.children.Count;
		}

		return allChunks;
	}

	public void Dispose()
	{
		var allChunks = CollectAllChunks();
		foreach (var chunk in allChunks)
		{
			chunk.Dispose();
		}
	}
}
