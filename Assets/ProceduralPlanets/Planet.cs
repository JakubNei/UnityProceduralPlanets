using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(FloatingOriginTransform))]
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

	[System.Serializable]
	public struct Craters
	{
		public ComputeBuffer gpuBuffer;
		public Vector4[] cpuBuffer;
		public int nextIndex;
	}
	public Craters craters;


	public ulong id;

	public List<Chunk> rootChildren;

	public bool markedForRegeneration;

	FloatingOriginTransform floatingOrigin;

	public BigPosition BigPosition => floatingOrigin.BigPosition;
	public Vector3 Center { get { return transform.position; } }

	
	void Start()
	{
		ProceduralPlanets.main.AddPlanet(this);
		InitializeRootChildren();
		GeneratePlanetData();

		floatingOrigin = GetComponent<FloatingOriginTransform>();

		craters.cpuBuffer = new Vector4[100];
		craters.gpuBuffer = new ComputeBuffer(craters.cpuBuffer.Length, 4 * sizeof(float));
	}

	public float GetGravityAtDistanceFromCenter(double distanceFromCenter)
	{
		double m = planetConfig.radiusHeightMapMultiplier;
		double r = planetConfig.radiusStart;
		
		double altidute = distanceFromCenter - r;
		if (altidute > r * 0.5f) return 0f;
		if (altidute < m) return 9.81f;

		return Mathf.SmoothStep(9.81f, 0, (float)((altidute - m) / (r * 0.5f)));
	}

	public void AddCrater(BigPosition bigPosition, float radius)
	{ 
		var planetLocalPos = this.BigPosition.Towards(bigPosition).ToVector3();
		craters.cpuBuffer[craters.nextIndex] = new Vector4(planetLocalPos.x, planetLocalPos.y, planetLocalPos.z, radius);
		++craters.nextIndex;
		if (craters.nextIndex >= craters.cpuBuffer.Length) craters.nextIndex = 0;
		craters.gpuBuffer.SetData(craters.cpuBuffer);

		foreach(var c in CollectAllChunks())
		{
			var s = c.rangePosToCalculateScreenSizeOn_localToPlanet.ToBoundingSphere();
			if (Vector3.Distance(s.center, planetLocalPos) < s.radius + radius)
			{
				c.MarkForRefresh();
			}
		}
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

	void ReGeneratePlanet()
	{
		subdivisonCalculationCoroutine = null;
		chunkGenerationCoroutines.Clear();
		subdivisonCalculationInProgress.Clear();
		subdivisonCalculationLast.Clear();

		GeneratePlanetData();
		ResetChunkRenderers();

		var allChunks = CollectAllChunks();
		foreach (var chunk in allChunks)
		{
			chunk.MarkForRefresh();
		}
	}

	public void ResetChunkRenderers()
	{
		foreach (var kvp in chunksBeingRendered)
		{
			ProceduralPlanets.main.ReturnChunkRendererToPool(kvp.Value);
		}

		chunksBeingRendered.Clear();
	}

	Dictionary<Chunk, ChunkRenderer> chunksBeingRendered = new Dictionary<Chunk, ChunkRenderer>();
	List<Chunk> toStopRendering = new List<Chunk>();
	List<Chunk> toStartRendering = new List<Chunk>();

	ChunkDivisionCalculation subdivisonCalculationInProgress = new ChunkDivisionCalculation();
	ChunkDivisionCalculation subdivisonCalculationLast = new ChunkDivisionCalculation();

	IEnumerator subdivisonCalculationCoroutine;

	void LateUpdate()
	{
		if (markedForRegeneration)
		{
			markedForRegeneration = false;
			ReGeneratePlanet();
			return;
		}

		var frameStart = Stopwatch.StartNew();

		var targetFrameTime = 1000.0f / 60; // set here instead of Application.targetFrameRate, so frame time is limited only by this script
		var currentFrameTime = 1000.0f * Time.unscaledDeltaTime;
		int milisecondsBudget = Mathf.FloorToInt(targetFrameTime - currentFrameTime);
		if (milisecondsBudget < 0) milisecondsBudget = 0;
		if (milisecondsBudget > 15) milisecondsBudget = 15;

		MyProfiler.AddAvergaNumberSample("milisecondsBudget", milisecondsBudget);


		var pointOfInterest = new PointOfInterest()
		{
			pos = FloatingOriginCamera.main.BigPosition,
			fieldOfView = FloatingOriginCamera.main.fieldOfView,
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


	List<Chunk> chunkGenerationFinished = new List<Chunk>();
	List<Tuple<Chunk, IEnumerator>> chunkGenerationCoroutines = new List<Tuple<Chunk, IEnumerator>>();
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
					if (chunk.GenerationInProgress) continue;
					chunkGenerationCoroutines.Add(new Tuple<Chunk, IEnumerator>(chunk, chunk.StartGenerateCoroutine()));
				};
			}

			if (chunkGenerationCoroutines.Count == 0) break;

			int numWaitingForEndOfFrame = 0;
			for (int i = chunkGenerationCoroutines.Count - 1; i >= 0; --i)
			{
				var c = chunkGenerationCoroutines[i];
				if (c.Item2.MoveNext()) // coroutine execution
				{
					if (c.Item2.Current is WaitForEndOfFrame)
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
					chunkGenerationFinished.Add(c.Item1);
				}

				if (frameStart.ElapsedMilliseconds > milisecondsBudget) break;
			}

			shouldStartNext = numWaitingForEndOfFrame >= chunkGenerationCoroutines.Count;

			shouldStartNext = shouldStartNext && chunkGenerationCoroutines.Count < 5;

		} while (frameStart.ElapsedMilliseconds < milisecondsBudget);

		MyProfiler.EndSample();

		MyProfiler.AddAvergaNumberSample("Procedural Planet / Generate chunks / coroutines executed", couroutinesExecuted);
		MyProfiler.AddAvergaNumberSample("Procedural Planet / Generate chunks / concurrent coroutines", chunkGenerationCoroutines.Count);
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


		// refresh again generated chunk that is currently being shown
		foreach (var c in chunkGenerationFinished)
		{
			if (toRenderChunks.Contains(c) && chunksBeingRendered.ContainsKey(c))
			{
				chunksBeingRendered[c].RenderChunk(c);
			}
		}
		chunkGenerationFinished.Clear();


		MyProfiler.BeginSample("Procedural Planet / Update ChunkRenderers / Return to pool");
		foreach (var chunk in toStopRendering)
		{
			ProceduralPlanets.main.ReturnChunkRendererToPool(chunksBeingRendered[chunk]);
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
			var renderer = ProceduralPlanets.main.GetFreeChunkRenderer(this);
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

	private void OnDrawGizmosSelected()
	{
		if (gameObject && gameObject.activeSelf)
		{
			Gizmos.color = Color.red;
			for (int i = 0; i < craters.nextIndex; ++i)
			{ 
				var c = craters.cpuBuffer[i];
				Gizmos.DrawWireSphere(this.transform.position + new Vector3(c.x, c.y, c.z), c.w);
			}
		}
	}
}
