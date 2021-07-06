using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Profiling;

[RequireComponent(typeof(FloatingOriginTransform))]
public partial class Planet : MonoBehaviour, IDisposable
{
	[System.Serializable]
	public class PlanetConfig
	{
		public Texture planetHeightMap;

		public ComputeShader generatePlanetHeightMap;
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
		public int maxChunksToRender = 500;
		public bool createColliders = true;
		public int textureResolution = 512; // must be multiplier of 16
		public bool rescaleToMinMax = false;
		public bool generateUsingPlanetGlobalPos = false;
		public int NumberOfVerticesNeededTotal { get { return numberOfVerticesOnEdge * numberOfVerticesOnEdge; } }

		public Material chunkMaterial;
		public ComputeShader generateChunkHeightMap;
		public ComputeShader generateChunkDiffuseMap;
		public ComputeShader GenerateChunkNormalMapOrVertices;

		public Texture2D grass;
		public Texture2D clay;
		public Texture2D rock;
		public Texture2D snow;
		public Texture2D tundra;
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

	public List<ChunkData> rootChildren;
	public List<ChunkData> allChunks;

	public bool markedForRegeneration;

	FloatingOriginTransform floatingOrigin;

	public BigPosition BigPosition => floatingOrigin.BigPosition;
	public Vector3 Center { get { return transform.position; } }

	~Planet()
	{
		Dispose();
	}

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
		var planetLocalPos = this.BigPosition.Towards(bigPosition).normalized.ToVector3();
		craters.cpuBuffer[craters.nextIndex] = new Vector4(planetLocalPos.x, planetLocalPos.y, planetLocalPos.z, radius);
		++craters.nextIndex;
		if (craters.nextIndex >= craters.cpuBuffer.Length) craters.nextIndex = 0;
		craters.gpuBuffer.SetData(craters.cpuBuffer);

		foreach (var c in CollectAllChunks())
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

		var heightMap = new RenderTexture(planetConfig.generatedPlanetHeightMapResolution, planetConfig.generatedPlanetHeightMapResolution, 0, RenderTextureFormat.RInt, RenderTextureReadWrite.Linear);
		heightMap.filterMode = FilterMode.Trilinear;
		heightMap.wrapMode = TextureWrapMode.Repeat;
		heightMap.enableRandomWrite = true;
		heightMap.Create();
		planetConfig.planetHeightMap = heightMap;

		planetConfig.generatePlanetHeightMap.SetTexture(0, "_planetHeightMap", heightMap);
		planetConfig.generatePlanetHeightMap.Dispatch(0, planetConfig.planetHeightMap.width / 16, planetConfig.planetHeightMap.height / 16, 1);

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
		foreach (var kvp in chunksCurrentlyRendered)
		{
			ProceduralPlanets.main.ReturnChunkRendererToPool(kvp.Value);
		}

		chunksCurrentlyRendered.Clear();
	}

	Dictionary<ChunkData, ChunkRenderer> chunksCurrentlyRendered = new Dictionary<ChunkData, ChunkRenderer>();
	HashSet<ChunkData> toStopRendering = new HashSet<ChunkData>();
	List<ChunkData> toStartRendering = new List<ChunkData>();
	LeastRecentlyUsedCache<ChunkData> hiddenChunks = new LeastRecentlyUsedCache<ChunkData>();

	ChunkDivisionCalculation subdivisonCalculationInProgress = new ChunkDivisionCalculation();
	ChunkDivisionCalculation subdivisonCalculationLast = new ChunkDivisionCalculation();

	IEnumerator subdivisonCalculationCoroutine;

	// inspired by https://stackoverflow.com/a/3719378/782022
	public class LeastRecentlyUsedCache<K>
	{
		private Dictionary<K, LinkedListNode<K>> cacheMap = new Dictionary<K, LinkedListNode<K>>();
		private LinkedList<K> lruList = new LinkedList<K>();

		public int Count => lruList.Count;

		public void Remove(K key)
		{
			LinkedListNode<K> node;
			if (cacheMap.TryGetValue(key, out node))
			{
				lruList.Remove(node);
				cacheMap.Remove(key);
			}
		}

		public void Add(K key)
		{
			if (cacheMap.ContainsKey(key))
			{
				Remove(key);
			}

			LinkedListNode<K> node = new LinkedListNode<K>(key);
			lruList.AddLast(node);
			cacheMap.Add(key, node);
		}

		public K GetAndRemoveLeastRecentlyUsed()
		{
			var node = lruList.First;
			lruList.RemoveFirst();
			cacheMap.Remove(node.Value);
			return node.Value;
		}
	}

	class LRUCacheItem<K, V>
	{
		public LRUCacheItem(K k, V v)
		{
			key = k;
			value = v;
		}
		public K key;
		public V value;
	}

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

		MyProfiler.BeginSample("Procedural Planet / milisecondsBudget", " " + milisecondsBudget.ToString());

		var pointOfInterest = new PointOfInterest()
		{
			pos = FloatingOriginCamera.main.BigPosition,
			fieldOfView = FloatingOriginCamera.main.fieldOfView,
		};

		bool shouldUpdateRenderers = false;

		MyProfiler.BeginSample("Procedural Planet / Calculate desired subdivision");
		if (subdivisonCalculationCoroutine == null)
		{
			//subdivisonCalculationInProgress.GatherCurrentState(allChunks);
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

		while (hiddenChunks.Count > 20)
		{
			hiddenChunks.GetAndRemoveLeastRecentlyUsed().ReleaseGeneratedData();
		}

		GenerateChunks(frameStart, milisecondsBudget);

		MyProfiler.EndSample();
	}


	List<ChunkData> chunkGenerationFinished = new List<ChunkData>();
	List<Tuple<ChunkData, IEnumerator>> chunkGenerationCoroutines = new List<Tuple<ChunkData, IEnumerator>>();
	private void GenerateChunks(Stopwatch frameStart, int milisecondsBudget)
	{
		MyProfiler.BeginSample("Procedural Planet / Generate chunks / coroutines execution");

		int couroutinesExecuted = 0;

		bool shouldStartNext = chunkGenerationCoroutines.Count == 0; // have at least 1 running
		do
		{
			if (shouldStartNext)
			{
				while (subdivisonCalculationLast.NumChunksToGenerate > 0)
				{
					ChunkData chunk = subdivisonCalculationLast.GetNextChunkToStartGeneration();
					if (chunk == null) continue;
					if (chunk.GenerationInProgress) continue;
					chunkGenerationCoroutines.Add(new Tuple<ChunkData, IEnumerator>(chunk, chunk.StartGenerateCoroutine()));
				};
			}

			if (chunkGenerationCoroutines.Count == 0) break;

			int numWaitingForEndOfFrame = 0;
			for (int i = chunkGenerationCoroutines.Count - 1; i >= 0; --i)
			{
				var c = chunkGenerationCoroutines[i];
				MyProfiler.BeginSample("Procedural Planet / Generate chunks / coroutines execution / MoveNext()");
				bool notFinished = c.Item2.MoveNext();
				MyProfiler.EndSample();
				if (notFinished) // coroutine execution
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

		MyProfiler.AddNumberSample("Procedural Planet / Generate chunks / coroutines executed", couroutinesExecuted);
		MyProfiler.AddNumberSample("Procedural Planet / Generate chunks / concurrent coroutines", chunkGenerationCoroutines.Count);
		MyProfiler.AddNumberSample("Procedural Planet / Generate chunks / to generate", subdivisonCalculationLast.NumChunksToGenerate);
	}


	HashSet<ChunkData> toRenderChunks = new HashSet<ChunkData>();
	Stack<ChunkRenderer> freeRenderers = new Stack<ChunkRenderer>();

	private void UpdateChunkRenderers(Stopwatch frameStart, int milisecondsBudget)
	{
		MyProfiler.BeginSample("Procedural Planet / Update ChunkRenderers");

		MyProfiler.BeginSample("Procedural Planet / Update ChunkRenderers / UnionWith");
		toRenderChunks.Clear();
		toRenderChunks.UnionWith(subdivisonCalculationLast.GetChunksToRender());
		MyProfiler.EndSample();

		MyProfiler.BeginSample("Procedural Planet / Update ChunkRenderers / calculate toStartRendering");
		toStartRendering.Clear();
		foreach (var chunk in toRenderChunks)
		{
			if (!chunksCurrentlyRendered.ContainsKey(chunk)) toStartRendering.Add(chunk);
			if (!chunk.HasFullyGeneratedData) continue;
			if (toStartRendering.Count > 1) break;
		}
		MyProfiler.EndSample();

		// if chunks are regenerated, refresh their rendering
		foreach (var c in chunkGenerationFinished)
		{
			if (toRenderChunks.Contains(c) && chunksCurrentlyRendered.ContainsKey(c))
			{
				chunksCurrentlyRendered[c].RenderChunk(c);
			}
		}
		chunkGenerationFinished.Clear();

		MyProfiler.BeginSample("Procedural Planet / Update ChunkRenderers / toStopRendering / calculate");
		toStopRendering.Clear();
		foreach (var kvp in chunksCurrentlyRendered)
		{
			if (!toRenderChunks.Contains(kvp.Key)) toStopRendering.Add(kvp.Key);
		}
		MyProfiler.EndSample();

		MyProfiler.BeginSample("Procedural Planet / Update ChunkRenderers / toStopRendering / execute");
		foreach (var chunk in toStopRendering)
		{
			bool bIsAnyParentRendered = false;
			{
				ChunkData parent = chunk.parent;
				while (parent != null)
				{
					if (!toStopRendering.Contains(parent) && chunksCurrentlyRendered.ContainsKey(parent))
					{
						bIsAnyParentRendered = true;
						break;
					}

					parent = parent.parent;
				}
			}

			bool bAllChildrenRendered = chunk.AreAllChildrenRendered;

			if (bIsAnyParentRendered || bAllChildrenRendered) // make sure we hide chunk only if there is some other chunk rendering mesh in its place
			{
				freeRenderers.Push(chunksCurrentlyRendered[chunk]);
				chunksCurrentlyRendered.Remove(chunk);
				hiddenChunks.Add(chunk);
			}
		}
		MyProfiler.EndSample();

		MyProfiler.BeginSample("Procedural Planet / Update ChunkRenderers / toStartRendering / get free renderers");
		while (freeRenderers.Count < toStartRendering.Count)
		{
			var renderer = ProceduralPlanets.main.GetFreeChunkRendererFromPool();
			freeRenderers.Push(renderer);
		}
		MyProfiler.EndSample();

		MyProfiler.BeginSample("Procedural Planet / Update ChunkRenderers / toStartRendering / execute");
		foreach (var chunk in toStartRendering)
		{
			var renderer = freeRenderers.Pop();
			renderer.RenderChunk(chunk);
			chunksCurrentlyRendered.Add(chunk, renderer);
			hiddenChunks.Remove(chunk);
		}
		MyProfiler.EndSample();

		MyProfiler.BeginSample("Procedural Planet / Update ChunkRenderers / Return to pool");
		while (freeRenderers.Count > 0)
		{
			ProceduralPlanets.main.ReturnChunkRendererToPool(freeRenderers.Pop());
		}
		MyProfiler.EndSample();


		MyProfiler.AddNumberSample("Procedural Planet / Update ChunkRenderers / to start rendering", toStartRendering.Count);
		MyProfiler.AddNumberSample("Procedural Planet / Update ChunkRenderers / to stop rendering", toStopRendering.Count);
		MyProfiler.AddNumberSample("Procedural Planet / Update ChunkRenderers / to render", toRenderChunks.Count);
		MyProfiler.AddNumberSample("Procedural Planet / chunksCurrentlyRendered", chunksCurrentlyRendered.Count);

		MyProfiler.EndSample();
	}


	private void InitializeRootChildren()
	{
		if (rootChildren != null && rootChildren.Count > 0) return;
		if (rootChildren == null)
			rootChildren = new List<ChunkData>(6);

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

		var child = ChunkData.Create(
			planet: this,
			parent: null,
			treeDepth: 0,
			range: range,
			id: id
		);

		allChunks.Add(child);
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

	List<ChunkData> CollectAllChunks()
	{
		var allChunks = new List<ChunkData>();

		var toProcess = new List<ChunkData>(rootChildren);
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
