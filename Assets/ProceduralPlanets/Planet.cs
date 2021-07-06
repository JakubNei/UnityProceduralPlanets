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
		chunkGenerationCoroutinesInProgress.Clear();
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

	LeastRecentlyUsedCache<ChunkData> considerChunkForCleanup = new LeastRecentlyUsedCache<ChunkData>();

	ChunkDivisionCalculation subdivisonCalculationInProgress = new ChunkDivisionCalculation();
	ChunkDivisionCalculation subdivisonCalculationLast = new ChunkDivisionCalculation();

	IEnumerator subdivisonCalculationCoroutine;

	// inspired by https://stackoverflow.com/a/3719378/782022
	public class LeastRecentlyUsedCache<K>
	{
		private Dictionary<K, LinkedListNode<Tuple<int, K>>> cacheMap = new Dictionary<K, LinkedListNode<Tuple<int, K>>>();
		private LinkedList<Tuple<int, K>> lruList = new LinkedList<Tuple<int, K>>();

		public int Count => cacheMap.Count;

		public void Remove(K key)
		{
			LinkedListNode<Tuple<int, K>> node;
			if (cacheMap.TryGetValue(key, out node))
			{
				lruList.Remove(node);
				cacheMap.Remove(key);
			}
		}

		public void Add(K key)
		{
			LinkedListNode<Tuple<int, K>> node;
			if (cacheMap.TryGetValue(key, out node))
			{
				lruList.Remove(node);
			}
			else
			{
				node = new LinkedListNode<Tuple<int, K>>(new Tuple<int, K>(Time.frameCount, key));
				cacheMap.Add(key, node);
			}

			lruList.AddLast(node);
		}

		public K GetAndRemoveLeastRecentlyUsed(int addedOverFramesAgo = 120)
		{
			var node = lruList.First;
			if (node.Value.Item1 > Time.frameCount - addedOverFramesAgo) return default(K);
			lruList.Remove(node);
			cacheMap.Remove(node.Value.Item2);
			return node.Value.Item2;
		}
	}



	class SlidingWindowAverageDouble
	{
		public double AverageValue => SumValue / (double)Samples.Count;
		double SumValue = 0;
		int MaxSamples = 100;
		Queue<double> Samples = new Queue<double>();
		public void AddSample(double sample)
		{
			while (Samples.Count > MaxSamples)
			{
				SumValue -= Samples.Dequeue();
			}

			Samples.Enqueue(sample);
			SumValue += sample;
		}
	}

	SlidingWindowAverageDouble averageFrameTime = new SlidingWindowAverageDouble();

	void LateUpdate()
	{
		if (markedForRegeneration)
		{
			markedForRegeneration = false;
			ReGeneratePlanet();
			return;
		}

		var frameStart = Stopwatch.StartNew();

		double targetFrameTime = 1.0f / 60; // set here instead of Application.targetFrameRate, so frame time is limited only by this script
		double currentFrameTime = 1.0f * Time.unscaledDeltaTime;
		averageFrameTime.AddSample(currentFrameTime);
		long timeBudgetInTicks = (long)(TimeSpan.TicksPerSecond * (targetFrameTime - averageFrameTime.AverageValue));
		var timeBudget = new TimeSpan(timeBudgetInTicks);
		if (timeBudget.TotalMilliseconds > 10) timeBudget = new TimeSpan(0, 0, 0, 0, 10);
		if (timeBudget.TotalMilliseconds < 1) timeBudget = new TimeSpan(0, 0, 0, 0, 1);

		MyProfiler.BeginSample("Procedural Planet / milisecondsBudget", " " + timeBudget.TotalMilliseconds.ToString());

		var pointOfInterest = new PointOfInterest()
		{
			pos = FloatingOriginCamera.main.BigPosition,
			fieldOfView = FloatingOriginCamera.main.fieldOfView,
		};

		UpdateChunkRenderers(frameStart, timeBudget);

		MyProfiler.BeginSample("Procedural Planet / ReleaseGeneratedData");
		int countToCleanup = chunkGenerationJustFinished.Count; // try to free same amount of memory that was just needed for generation
		if (countToCleanup <= 1) countToCleanup = 1;
		while (--countToCleanup >= 0 && considerChunkForCleanup.Count > 0)
		{
			var chunk = considerChunkForCleanup.GetAndRemoveLeastRecentlyUsed();
			if (chunk == null) break;
			chunk.ReleaseGeneratedData();
		}
		MyProfiler.EndSample();

		GenerateChunks(frameStart, timeBudget);

		MyProfiler.BeginSample("Procedural Planet / Calculate desired subdivision");
		if (subdivisonCalculationCoroutine == null)
		{
			subdivisonCalculationCoroutine = subdivisonCalculationInProgress.StartCoroutine(this, pointOfInterest);
		}
		do // do at least once
		{
			if (!subdivisonCalculationCoroutine.MoveNext())
			{
				subdivisonCalculationCoroutine = null;
				var temp = subdivisonCalculationInProgress;
				subdivisonCalculationInProgress = subdivisonCalculationLast;
				subdivisonCalculationLast = temp;
				break;
			}
		} while (frameStart.Elapsed > timeBudget && subdivisonCalculationCoroutine != null);
		MyProfiler.EndSample();

		MyProfiler.EndSample();
	}


	List<ChunkData> chunkGenerationJustFinished = new List<ChunkData>();
	List<Tuple<ChunkData, IEnumerator>> chunkGenerationCoroutinesInProgress = new List<Tuple<ChunkData, IEnumerator>>();
	private void GenerateChunks(Stopwatch frameStart, TimeSpan timeBudget)
	{
		MyProfiler.BeginSample("Procedural Planet / Generate chunks / start new");
		if (chunkGenerationCoroutinesInProgress.Count <= 300)
		{
			// start only one new coroutine every frame, so their steps are interleaved
			// we don't want all coroutines to do the same thing at once, because when they all request mesh from GPU at once, they may cause big render thread stall
			while (subdivisonCalculationLast.NumChunksToGenerate > 0)
			{
				ChunkData chunk = subdivisonCalculationLast.GetNextChunkToStartGeneration();
				if (chunk == null) continue;
				if (chunk.GenerationInProgress) continue;
				chunkGenerationCoroutinesInProgress.Add(new Tuple<ChunkData, IEnumerator>(chunk, chunk.StartGenerateCoroutine()));
				break;
			};
		}
		MyProfiler.EndSample();

		MyProfiler.BeginSample("Procedural Planet / Generate chunks / execution");
		chunkGenerationJustFinished.Clear();
		//do
		{
			for (int i = chunkGenerationCoroutinesInProgress.Count - 1; i >= 0; --i)
			{
				var c = chunkGenerationCoroutinesInProgress[i];
				MyProfiler.BeginSample("Procedural Planet / Generate chunks / execution / MoveNext()");
				bool finishedExecution = !c.Item2.MoveNext();
				MyProfiler.EndSample();
				if (finishedExecution)
				{
					chunkGenerationCoroutinesInProgress.RemoveAt(i);
					chunkGenerationJustFinished.Add(c.Item1);
					considerChunkForCleanup.Add(c.Item1);
				}

				if (frameStart.Elapsed > timeBudget) break;
			}
		}
		// we dont want to execute again, as some coroutines might just be idling, waiting for getMeshDataReadbackRequest.done
		//while (frameStart.Elapsed < timeBudget);
		MyProfiler.EndSample();

		MyProfiler.AddNumberSample("Procedural Planet / Generate chunks / concurrent coroutines", chunkGenerationCoroutinesInProgress.Count);
		MyProfiler.AddNumberSample("Procedural Planet / Generate chunks / to generate", subdivisonCalculationLast.NumChunksToGenerate);
	}


	HashSet<ChunkData> toRenderChunks = new HashSet<ChunkData>();
	Stack<ChunkRenderer> freeRenderers = new Stack<ChunkRenderer>();

	private void UpdateChunkRenderers(Stopwatch frameStart, TimeSpan timeBudget)
	{
		MyProfiler.BeginSample("Procedural Planet / Update ChunkRenderers");

		if (subdivisonCalculationLast == null) return;

		MyProfiler.BeginSample("Procedural Planet / Update ChunkRenderers / UnionWith");
		toRenderChunks.Clear();
		toRenderChunks.UnionWith(subdivisonCalculationLast.GetChunksToRender());
		MyProfiler.EndSample();

		MyProfiler.BeginSample("Procedural Planet / Update ChunkRenderers / calculate toStartRendering");
		toStartRendering.Clear();
		foreach (var chunk in toRenderChunks)
		{
			if (!chunk.HasFullyGeneratedData) continue;
			if (!chunksCurrentlyRendered.ContainsKey(chunk)) toStartRendering.Add(chunk);
			//if (toStartRendering.Count > 10) break;
		}
		MyProfiler.EndSample();

		// if chunks are regenerated, refresh their rendering
		foreach (var chunk in chunkGenerationJustFinished)
		{
			if (chunksCurrentlyRendered.ContainsKey(chunk))
			{
				chunksCurrentlyRendered[chunk].RenderChunk(chunk);
				considerChunkForCleanup.Remove(chunk);
			}
		}

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
				considerChunkForCleanup.Add(chunk);
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
			considerChunkForCleanup.Remove(chunk);
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
