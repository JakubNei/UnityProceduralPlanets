using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

[Serializable]
public class Chunk : IDisposable
{

	public Planet planet;

	public ulong id;
	public Chunk parent;
	public int treeDepth;

	public Range rangeUnitCubePosRealSubdivided;
	public Range rangeUnitCubePosToGenerateInto;
	public Range rangePosToCalculateScreenSizeOn_localToPlanet;


	public Vector3 offsetFromPlanetCenter;

	public float chunkRangeMaxAngleDeg;


	// float slope = (abs(sx0 - s0) + abs(sy0 - s0)) / _chunkRelativeSize * (w / 256.0);
	//public float ChunkRelativeSize { get { return chunkRadius / planetConfig.radiusStart; } }

	//public bool GenerateUsingPlanetGlobalPos { get { return chunkRangeMaxAngleDeg > 2; } }
	public bool GenerateUsingPlanetGlobalPos { get { return chunkConfig.generateUsingPlanetGlobalPos; } }
	//public int SlopeModifier { get { return (int)Mathf.Pow(2, generation); } }
	//public float SlopeModifier { get { return (float)((planetConfig.radiusStart / chunkRadius / 4 * (heightMapResolution / 1024.0)) / 44.0 * HeightRange); } }
	public float SlopeModifier { get { return (float)(Mathf.Pow(2, treeDepth) * (HeightMapResolution / 1024.0) * HeightRange); } }
	public float HeightRange { get { return heightMax - heightMin; } }

	public BigPosition bigPositionLocalToPlanet => new BigPosition(GenerateUsingPlanetGlobalPos ? Vector3.zero : offsetFromPlanetCenter);

	public Planet.PlanetConfig planetConfig { get { return planet.planetConfig; } }
	public Planet.ChunkConfig chunkConfig { get { return planet.chunkConfig; } }

	[Serializable]
	public class GeneratedData : IDisposable
	{
		public RenderTexture chunkHeightMap;
		public RenderTexture chunkWorldNormalMap;
		public RenderTexture chunkTangentNormalMap;
		public RenderTexture chunkDiffuseMap;
		public RenderTexture chunkSlopeMap;
		public Mesh mesh;
		~GeneratedData()
		{
			Dispose();
		}

		public void Dispose()
		{
			if (mesh) Mesh.Destroy(mesh);
			mesh = null;

			if (chunkHeightMap) chunkHeightMap.Release();
			chunkHeightMap = null;
			if (chunkWorldNormalMap) chunkWorldNormalMap.Release();
			chunkWorldNormalMap = null;
			if (chunkTangentNormalMap) chunkTangentNormalMap.Release();
			chunkTangentNormalMap = null;
			if (chunkDiffuseMap) chunkDiffuseMap.Release();
			chunkDiffuseMap = null;
			if (chunkSlopeMap) chunkSlopeMap.Release();
			chunkSlopeMap = null;
		}
	};

	public GeneratedData FullyGeneratedData { get; private set; }
	GeneratedData generatingData;
	public bool GenerationInProgress => generatingData != null;
	public bool HasFullyGeneratedData => FullyGeneratedData != null;
	public bool WantsRefresh;


	public float heightMin = 0;
	public float heightMax = 1;

	public ChildPosition childPosition;

	public enum ChildPosition
	{
		NoneNoParent = 0,
		TopLeft = 1,
		TopRight = 2,
		BottomLeft = 3,
		BottomRight = 4,
	}



	[NonSerialized]
	public List<Chunk> children = new List<Chunk>(4);
	public float chunkRadius;

	int HeightMapResolution { get { return chunkConfig.textureResolution; } }
	int NormalMapResolution { get { return HeightMapResolution; } }
	int DiffuseMapResolution { get { return NormalMapResolution; } }
	int SlopeMapResolution { get { return HeightMapResolution; } }
	int BiomesMapResolution { get { return 16; } }

	public static Chunk Create(Planet planet, Range range, ulong id, Chunk parent = null, int treeDepth = 0, ChildPosition childPosition = ChildPosition.NoneNoParent)
	{
		MyProfiler.BeginSample("Procedural Planet / Create chunk");

		var chunk = new Chunk();

		chunk.planet = planet;
		chunk.id = id;
		chunk.parent = parent;
		chunk.treeDepth = treeDepth;
		chunk.childPosition = childPosition;
		chunk.rangeUnitCubePosRealSubdivided = range;

		//chunk.rangePosToGenerateInto = new Range
		//{
		//	a = chunk.rangeUnitCubePos.a.normalized * planet.planetConfig.radiusStart,
		//	b = chunk.rangeUnitCubePos.b.normalized * planet.planetConfig.radiusStart,
		//	c = chunk.rangeUnitCubePos.c.normalized * planet.planetConfig.radiusStart,
		//	d = chunk.rangeUnitCubePos.d.normalized * planet.planetConfig.radiusStart,
		//};

		if (chunk.chunkConfig.useSkirts)
		{
			var ratio = ((chunk.chunkConfig.numberOfVerticesOnEdge - 1) / 2.0f) / ((chunk.chunkConfig.numberOfVerticesOnEdge - 1 - 2) / 2.0f);
			var center = chunk.rangeUnitCubePosRealSubdivided.CenterPos;
			var a = chunk.rangeUnitCubePosRealSubdivided.a - center;
			var b = chunk.rangeUnitCubePosRealSubdivided.b - center;
			var c = chunk.rangeUnitCubePosRealSubdivided.c - center;
			var d = chunk.rangeUnitCubePosRealSubdivided.d - center;

			chunk.rangeUnitCubePosToGenerateInto.a = a * ratio + center;
			chunk.rangeUnitCubePosToGenerateInto.b = b * ratio + center;
			chunk.rangeUnitCubePosToGenerateInto.c = c * ratio + center;
			chunk.rangeUnitCubePosToGenerateInto.d = d * ratio + center;
		}
		else
		{
			chunk.rangeUnitCubePosToGenerateInto = chunk.rangeUnitCubePosRealSubdivided;
		}


		chunk.rangePosToCalculateScreenSizeOn_localToPlanet = new Range
		{
			a = chunk.rangeUnitCubePosToGenerateInto.a.normalized * planet.planetConfig.radiusStart,
			b = chunk.rangeUnitCubePosToGenerateInto.b.normalized * planet.planetConfig.radiusStart,
			c = chunk.rangeUnitCubePosToGenerateInto.c.normalized * planet.planetConfig.radiusStart,
			d = chunk.rangeUnitCubePosToGenerateInto.d.normalized * planet.planetConfig.radiusStart,
		};

		chunk.chunkRadius = chunk.rangePosToCalculateScreenSizeOn_localToPlanet.ToBoundingSphere().radius;


		//chunk.rangeDirToGenerateInto = new Range
		//{
		//	a = chunk.rangePosToGenerateInto.a.normalized,
		//	b = chunk.rangePosToGenerateInto.b.normalized,
		//	c = chunk.rangePosToGenerateInto.c.normalized,
		//	d = chunk.rangePosToGenerateInto.d.normalized,
		//};


		//chunk.rangeLocalPosToGenerateInto = new Range
		//{
		//	a = chunk.rangePosToGenerateInto.a - chunk.offsetFromPlanetCenter,
		//	b = chunk.rangePosToGenerateInto.b - chunk.offsetFromPlanetCenter,
		//	c = chunk.rangePosToGenerateInto.c - chunk.offsetFromPlanetCenter,
		//	d = chunk.rangePosToGenerateInto.d - chunk.offsetFromPlanetCenter,
		//};

		chunk.chunkRangeMaxAngleDeg = Mathf.Max(
			Vector3.Angle(chunk.rangeUnitCubePosToGenerateInto.a, chunk.rangeUnitCubePosToGenerateInto.b),
			Vector3.Angle(chunk.rangeUnitCubePosToGenerateInto.b, chunk.rangeUnitCubePosToGenerateInto.c),
			Vector3.Angle(chunk.rangeUnitCubePosToGenerateInto.c, chunk.rangeUnitCubePosToGenerateInto.d),
			Vector3.Angle(chunk.rangeUnitCubePosToGenerateInto.d, chunk.rangeUnitCubePosToGenerateInto.a),
			Vector3.Angle(chunk.rangeUnitCubePosToGenerateInto.a, chunk.rangeUnitCubePosToGenerateInto.c),
			Vector3.Angle(chunk.rangeUnitCubePosToGenerateInto.b, chunk.rangeUnitCubePosToGenerateInto.d)
		);

		chunk.offsetFromPlanetCenter = chunk.rangeUnitCubePosToGenerateInto.CenterPos.normalized * planet.planetConfig.radiusStart;

		MyProfiler.EndSample();

		return chunk;
	}

	public void MarkForRefresh()
	{
		WantsRefresh = true;
		generatingData = null;
	}

	private void AddChild(Vector3 a, Vector3 b, Vector3 c, Vector3 d, ChildPosition cp, ushort index)
	{
		var range = new Range()
		{
			a = a,
			b = b,
			c = c,
			d = d,
		};

		var child = Create(
			planet: planet,
			parent: this,
			range: range,
			id: id << 2 | index,
			treeDepth: treeDepth + 1,
			childPosition: cp
		);

		children.Add(child);
	}

	public void EnsureChildrenInstancesAreCreated()
	{
		if (children.Count <= 0)
		{
			/*
			a----ab---b
			|    |    |
			ad--mid---bc
			|    |    |
			d----dc---c
			*/

			var a = rangeUnitCubePosRealSubdivided.a;
			var b = rangeUnitCubePosRealSubdivided.b;
			var c = rangeUnitCubePosRealSubdivided.c;
			var d = rangeUnitCubePosRealSubdivided.d;
			var ab = (a + b) / 2.0f;
			var ad = (a + d) / 2.0f;
			var bc = (b + c) / 2.0f;
			var dc = (d + c) / 2.0f;
			var mid = (ab + ad + dc + bc) / 4.0f;

			AddChild(a, ab, mid, ad, ChildPosition.TopLeft, 0);
			AddChild(ab, b, bc, mid, ChildPosition.TopRight, 1);
			AddChild(ad, mid, dc, d, ChildPosition.BottomLeft, 2);
			AddChild(mid, bc, c, dc, ChildPosition.BottomRight, 3);
		}
	}


	AsyncGPUReadbackRequest getMeshDataReadbackRequest;

	public IEnumerator StartGenerateCoroutine()
	{
		WantsRefresh = false;
		generatingData = new GeneratedData();

		MyProfiler.BeginSample("Procedural Planet / Generate chunk / Height map");
		GenerateHeightMap();
		MyProfiler.EndSample();
		yield return null;

		MyProfiler.BeginSample("Procedural Planet / Generate chunk / Normal map");
		GenerateNormalMap();
		MyProfiler.EndSample();
		yield return null;

		MyProfiler.BeginSample("Procedural Planet / Generate chunk / Mesh / Generate on GPU");
		GenerateMesh();
		MyProfiler.EndSample();

		MyProfiler.BeginSample("Procedural Planet / Generate chunk / Mesh / Get data from GPU to CPU / Request");
		RequestMeshData();
		MyProfiler.EndSample();

		MyProfiler.BeginSample("Procedural Planet / Generate chunk / Diffuse map");
		GenerateDiffuseMap();
		MyProfiler.EndSample();
		yield return null;

		while (!getMeshDataReadbackRequest.done) yield return new WaitForEndOfFrame();

		MyProfiler.BeginSample("Procedural Planet / Generate chunk / Mesh / Create on CPU");
		CreateMesh();
		MyProfiler.EndSample();
		yield return null;

		MyProfiler.BeginSample("Procedural Planet / Generate chunk / Mesh / Upload to GPU");
		UploadMesh();
		MyProfiler.EndSample();
		yield return null;

		CleanupAfterGeneration();

		FullyGeneratedData = generatingData;
		generatingData = null;
	}


	void SetAll(ComputeShader c, int kernelIndex)
	{
		rangeUnitCubePosToGenerateInto.SetParams(c, "_rangeUnitCubePos");

		c.SetInt("_numberOfVerticesOnEdge", chunkConfig.numberOfVerticesOnEdge);

		c.SetFloat("_heightMin", heightMin);
		c.SetFloat("_heightMax", heightMax);

		c.SetFloat("_radiusStart", planetConfig.radiusStart);
		c.SetFloat("_radiusHeightMapMultiplier", planetConfig.radiusHeightMapMultiplier);

		c.SetFloat("_planetRadiusStart", planetConfig.radiusStart);
		c.SetFloat("_planetRadiusHeightMapMultiplier", planetConfig.radiusHeightMapMultiplier);

		c.SetFloat("_chunkRadius", chunkRadius);
		c.SetInt("_generation", (int)treeDepth);

		c.SetTexture(kernelIndex, "_planetHeightMap", planetConfig.planetHeightMap);
		if (generatingData.chunkHeightMap != null) c.SetTexture(kernelIndex, "_chunkHeightMap", generatingData.chunkHeightMap);
		if (generatingData.chunkWorldNormalMap != null) c.SetTexture(kernelIndex, "_chunkNormalMap", generatingData.chunkWorldNormalMap);
		if (generatingData.chunkSlopeMap != null) c.SetTexture(kernelIndex, "_chunkSlopeMap", generatingData.chunkSlopeMap);

		c.SetFloat("_slopeModifier", SlopeModifier);


		var parentUvStart = Vector2.zero;
		if (childPosition == ChildPosition.TopLeft) parentUvStart = new Vector2(0, 0);
		else if (childPosition == ChildPosition.TopRight) parentUvStart = new Vector2(0.5f, 0);
		else if (childPosition == ChildPosition.BottomLeft) parentUvStart = new Vector2(0, 0.5f);
		else if (childPosition == ChildPosition.BottomRight) parentUvStart = parentUvStart = new Vector2(0.5f, 0.5f);


		if (chunkConfig.useSkirts)
		{
			var off = 1.0f / (chunkConfig.numberOfVerticesOnEdge - 1) / 2.0f;
			if (childPosition == ChildPosition.TopLeft) parentUvStart += new Vector2(off, off);
			else if (childPosition == ChildPosition.TopRight) parentUvStart += new Vector2(-off, off);
			else if (childPosition == ChildPosition.BottomLeft) parentUvStart += new Vector2(off, -off);
			else if (childPosition == ChildPosition.BottomRight) parentUvStart += new Vector2(-off, -off);
		}

		c.SetVector("_parentUvStart", parentUvStart);

	}


	void GenerateHeightMap()
	{
		if (chunkConfig.rescaleToMinMax)
		{
			var heightRough = new RenderTexture(HeightMapResolution / 2, HeightMapResolution / 2, 0, RenderTextureFormat.RInt, RenderTextureReadWrite.Linear);
			heightRough.wrapMode = TextureWrapMode.Clamp;
			heightRough.filterMode = FilterMode.Bilinear;
			heightRough.enableRandomWrite = true;
			heightRough.Create();

			var c = chunkConfig.generateChunkHeightMap;
			SetAll(c, 0);
			c.SetTexture(0, "_planetHeightMap", planetConfig.planetHeightMap);
			rangeUnitCubePosToGenerateInto.SetParams(c, "_rangeUnitCubePos");
			c.SetFloat("_heightMin", 0);
			c.SetFloat("_heightMax", 1);

			c.SetTexture(0, "_chunkHeightMap", heightRough);
			c.Dispatch(0, heightRough.width / 16, heightRough.height / 16, 1);

			MyProfiler.BeginSample("find texture min max");
			var result = FindTextureMinMax.Find(heightRough, RenderTextureFormat.RInt);
			MyProfiler.EndSample();
			heightMax = result.max.x;
			heightMin = result.min.x;

			var r = 0.1f;
			if (parent != null) r = parent.HeightRange / 5.0f;
			heightMax += r;
			heightMin -= r;

			//DEBUG
			//heightMax = 1; heightMin = 0;

			heightRough.Release();
		}

		{
			if (generatingData.chunkHeightMap == null)
			{
				generatingData.chunkHeightMap = new RenderTexture(HeightMapResolution, HeightMapResolution, 0, RenderTextureFormat.RInt, RenderTextureReadWrite.Linear);
				generatingData.chunkHeightMap.wrapMode = TextureWrapMode.Clamp;
				generatingData.chunkHeightMap.filterMode = FilterMode.Bilinear;
				generatingData.chunkHeightMap.enableRandomWrite = true;
				generatingData.chunkHeightMap.Create();
			}

			var c = chunkConfig.generateChunkHeightMap;
			SetAll(c, 0);
			c.SetTexture(0, "_planetHeightMap", planetConfig.planetHeightMap);
			rangeUnitCubePosToGenerateInto.SetParams(c, "_rangeUnitCubePos");
			c.SetFloat("_heightMin", heightMin);
			c.SetFloat("_heightMax", heightMax);

			c.SetBool("_hasParent", parent != null);
			if (parent != null && parent.HasFullyGeneratedData && parent.FullyGeneratedData.chunkHeightMap)
				c.SetTexture(0, "_parentChunkHeightMap", parent.FullyGeneratedData.chunkHeightMap);

			c.SetTexture(0, "_chunkHeightMap", generatingData.chunkHeightMap);
			c.Dispatch(0, generatingData.chunkHeightMap.width / 16, generatingData.chunkHeightMap.height / 16, 1);
		}
	}


	//ComputeBuffer vertexGPUBuffer { get { return planet.chunkVertexGPUBuffer; } }
	ComputeBuffer vertexGPUBuffer;

	void GenerateMesh()
	{
		vertexGPUBuffer = new ComputeBuffer(chunkConfig.NumberOfVerticesNeededTotal, 3 * 3 * sizeof(float));
		var c = chunkConfig.generateChunkVertices;
		var verticesOnEdge = chunkConfig.numberOfVerticesOnEdge;

		var kernelIndex = 0;
		if (GenerateUsingPlanetGlobalPos) kernelIndex = c.FindKernel("generateUsingPlanetGlobalPos");
		else kernelIndex = c.FindKernel("generateUsingChunkLocalPos");

		SetAll(c, kernelIndex);
		rangeUnitCubePosToGenerateInto.SetParams(c, "_rangeUnitCubePos");
		c.SetInt("_numberOfVerticesOnEdge", verticesOnEdge);
		c.SetFloat("_planetRadiusStart", planetConfig.radiusStart);
		c.SetFloat("_planetRadiusHeightMapMultiplier", planetConfig.radiusHeightMapMultiplier);
		c.SetTexture(kernelIndex, "_chunkHeightMap", generatingData.chunkHeightMap);

		c.SetFloat("_heightMin", heightMin);
		c.SetFloat("_heightMax", heightMax);
		c.SetFloat("_moveEdgeVerticesDown", chunkConfig.useSkirts ? chunkRadius / 20.0f : 0);

		c.SetBuffer(kernelIndex, "_craterSpherePositionRadius", planet.craters.gpuBuffer);

		c.SetBuffer(kernelIndex, "_vertices", vertexGPUBuffer);
		c.Dispatch(kernelIndex, verticesOnEdge, verticesOnEdge, 1);

	}

	[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
	struct PerVertexData
	{
		public Vector3 position;
		public Vector3 normal;
		public Vector3 tangent;
	}

	NativeArray<PerVertexData> vertexCPUBuffer;


	void RequestMeshData()
	{
		MyProfiler.BeginSample("Procedural Planet / Generate chunk / Mesh / Get data from GPU to CPU / Request / new NativeArray");
		if (vertexCPUBuffer.IsCreated) vertexCPUBuffer.Dispose();
		vertexCPUBuffer = new NativeArray<PerVertexData>(chunkConfig.NumberOfVerticesNeededTotal, Allocator.Persistent);
		MyProfiler.EndSample();

		MyProfiler.BeginSample("Procedural Planet / Generate chunk / Mesh / Get data from GPU to CPU / Request / RequestIntoNativeArray");
		getMeshDataReadbackRequest = AsyncGPUReadback.RequestIntoNativeArray(ref vertexCPUBuffer, vertexGPUBuffer);
		MyProfiler.EndSample();
	}

	void CreateMesh()
	{
		if (vertexGPUBuffer != null && vertexGPUBuffer.IsValid())
		{
			vertexGPUBuffer.Dispose();
			vertexGPUBuffer = null;
		}

		var verticesOnEdge = chunkConfig.numberOfVerticesOnEdge;

		{
			int aIndex = 0;
			int bIndex = verticesOnEdge - 1;
			int cIndex = verticesOnEdge * verticesOnEdge - 1;
			int dIndex = cIndex - (verticesOnEdge - 1);
			rangePosToCalculateScreenSizeOn_localToPlanet.a = vertexCPUBuffer[aIndex].position;
			rangePosToCalculateScreenSizeOn_localToPlanet.b = vertexCPUBuffer[bIndex].position;
			rangePosToCalculateScreenSizeOn_localToPlanet.c = vertexCPUBuffer[cIndex].position;
			rangePosToCalculateScreenSizeOn_localToPlanet.d = vertexCPUBuffer[dIndex].position;

			if (!GenerateUsingPlanetGlobalPos)
			{
				rangePosToCalculateScreenSizeOn_localToPlanet.a += offsetFromPlanetCenter;
				rangePosToCalculateScreenSizeOn_localToPlanet.b += offsetFromPlanetCenter;
				rangePosToCalculateScreenSizeOn_localToPlanet.c += offsetFromPlanetCenter;
				rangePosToCalculateScreenSizeOn_localToPlanet.d += offsetFromPlanetCenter;
			}
		}

		if (generatingData.mesh) Mesh.Destroy(generatingData.mesh);
		// TODO: optimize: fill mesh vertices on GPU instead of CPU, remember we still need vertices on CPU for mesh collider
		generatingData.mesh = new Mesh();
		generatingData.mesh.name = this.ToString();
		generatingData.mesh.SetVertexBufferParams(
			vertexCPUBuffer.Length,
			new[]
			{
				new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
				new VertexAttributeDescriptor(VertexAttribute.Normal, VertexAttributeFormat.Float32, 3),
				new VertexAttributeDescriptor(VertexAttribute.Tangent, VertexAttributeFormat.Float32, 3),
			}
		);
		generatingData.mesh.SetVertexBufferData(vertexCPUBuffer, 0, 0, vertexCPUBuffer.Length);
		generatingData.mesh.triangles = planet.GetChunkMeshTriangles();
		generatingData.mesh.uv = planet.GetChunkMeshUVs();
		generatingData.mesh.RecalculateBounds();

		if (true || !chunkConfig.generateChunkNormapMap)
		{
			//generatedData.mesh.RecalculateNormals();
			//generatedData.mesh.RecalculateTangents();

			//generatingData.mesh.normals = RecalculateNormals(ref vertexCPUBuffer, planet.GetChunkMeshTriangles(), planet.GetChunkMeshUVs(), planet.GetChunkMeshIndiciesEdge());
			//generatingData.mesh.tangents = RecalculateTangents(ref vertexCPUBuffer, planet.GetChunkMeshTriangles(), planet.GetChunkMeshUVs(), planet.GetChunkMeshIndiciesEdge());
		}
		else
		{
			generatingData.mesh.normals = new Vector3[] { };
			generatingData.mesh.tangents = new Vector4[] { };
		}
	}

	void UploadMesh()
	{
		generatingData.mesh.UploadMeshData(false);

		if (vertexCPUBuffer.IsCreated) vertexCPUBuffer.Dispose();
	}


	void GenerateNormalMap()
	{
		var c = chunkConfig.generateChunkNormapMap;
		if (c == null) return;

		if (generatingData.chunkWorldNormalMap != null && generatingData.chunkWorldNormalMap.width != NormalMapResolution)
		{
			generatingData.chunkWorldNormalMap.Release();
			generatingData.chunkWorldNormalMap = null;
		}

		if (generatingData.chunkWorldNormalMap == null)
		{
			generatingData.chunkWorldNormalMap = new RenderTexture(NormalMapResolution, NormalMapResolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
			generatingData.chunkWorldNormalMap.wrapMode = TextureWrapMode.Clamp;
			generatingData.chunkWorldNormalMap.filterMode = FilterMode.Bilinear;
			generatingData.chunkWorldNormalMap.enableRandomWrite = true;
			generatingData.chunkWorldNormalMap.Create();
		}

		if (generatingData.chunkSlopeMap != null && generatingData.chunkSlopeMap.width != SlopeMapResolution)
		{
			generatingData.chunkSlopeMap.Release();
			generatingData.chunkSlopeMap = null;
		}

		if (generatingData.chunkSlopeMap == null)
		{
			generatingData.chunkSlopeMap = new RenderTexture(SlopeMapResolution, SlopeMapResolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
			generatingData.chunkSlopeMap.wrapMode = TextureWrapMode.Clamp;
			generatingData.chunkSlopeMap.filterMode = FilterMode.Bilinear;
			generatingData.chunkSlopeMap.enableRandomWrite = true;
			generatingData.chunkSlopeMap.Create();
		}

		SetAll(c, 0);

		var _normalLength = chunkRadius / generatingData.chunkWorldNormalMap.width / (planetConfig.radiusHeightMapMultiplier * HeightRange);
		//UnityEngine.Debug.Log(_normalLength);
		c.SetFloat("_normalLength", _normalLength);

		c.Dispatch(0, generatingData.chunkWorldNormalMap.width / 16, generatingData.chunkWorldNormalMap.height / 16, 1);
	}



	void GenerateDiffuseMap()
	{
		if (generatingData.chunkDiffuseMap != null && generatingData.chunkDiffuseMap.width != DiffuseMapResolution)
		{
			generatingData.chunkDiffuseMap.Release();
			generatingData.chunkDiffuseMap = null;
		}

		if (generatingData.chunkDiffuseMap == null)
		{
			generatingData.chunkDiffuseMap = new RenderTexture(DiffuseMapResolution, DiffuseMapResolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
			generatingData.chunkDiffuseMap.wrapMode = TextureWrapMode.Clamp;
			generatingData.chunkDiffuseMap.filterMode = FilterMode.Trilinear;
			generatingData.chunkDiffuseMap.useMipMap = true;
			generatingData.chunkDiffuseMap.autoGenerateMips = false;
			generatingData.chunkDiffuseMap.enableRandomWrite = true;
			generatingData.chunkDiffuseMap.Create();
		}

		var c = chunkConfig.generateChunkDiffuseMap;
		SetAll(c, 0);
		c.SetFloat("_mipMapLevel", Mathf.Max(1, chunkRadius / 100.0f) - 1);
		c.SetTexture(0, "_grass", chunkConfig.grass);
		c.SetTexture(0, "_clay", chunkConfig.clay);
		c.SetTexture(0, "_rock", chunkConfig.rock);
		c.SetTexture(0, "_snow", chunkConfig.snow);
		c.SetTexture(0, "_tundra", chunkConfig.tundra);
		c.SetBuffer(0, "_craterSpherePositionRadius", planet.craters.gpuBuffer);
		c.SetTexture(0, "_chunkDiffuseMap", generatingData.chunkDiffuseMap);
		c.Dispatch(0, generatingData.chunkDiffuseMap.width / 16, generatingData.chunkDiffuseMap.height / 16, 1);

		if (generatingData.chunkDiffuseMap.useMipMap) generatingData.chunkDiffuseMap.GenerateMips();
	}

	void CleanupAfterGeneration()
	{
		if (Application.platform == RuntimePlatform.WindowsEditor)
			return; // in editor we want to see and debug stuff

		if (generatingData.chunkHeightMap) generatingData.chunkHeightMap.Release();
		generatingData.chunkHeightMap = null;
	}

	private float GetSizeOnScreen(Planet.PointOfInterest data)
	{
		var myPos = rangePosToCalculateScreenSizeOn_localToPlanet.CenterPos + planet.BigPosition;
		var distanceToCamera = BigPosition.Distance(myPos, data.pos);

		// TODO: this is world space, doesnt take into consideration rotation, not good,
		// but we dont care about rotation, we want to have correct detail even if we are looking at chunk from side?
		var sphere = rangePosToCalculateScreenSizeOn_localToPlanet.ToBoundingSphere();
		var radiusWorldSpace = sphere.radius;
		var fov = data.fieldOfView;
		var cot = 1.0 / Mathf.Tan(fov / 2f * Mathf.Deg2Rad);
		var radiusCameraSpace = radiusWorldSpace * cot / distanceToCamera;

		lastDistanceToCamera = distanceToCamera;
		lastRadiusWorldSpace = radiusWorldSpace;

		return (float)radiusCameraSpace;
	}

	public double lastDistanceToCamera;
	public double lastRadiusWorldSpace;
	public float lastGenerationWeight;
	public float GetRelevanceWeight(Planet.PointOfInterest data)
	{
		var weight = GetSizeOnScreen(data);
		lastGenerationWeight = weight;
		return weight;
	}

	public override string ToString()
	{
		return typeof(Chunk) + " treeDepth:" + treeDepth + " id:#" + id;
	}

	public void Dispose()
	{
		generatingData = null;
		FullyGeneratedData = null;
		vertexGPUBuffer = null;

		if (vertexCPUBuffer.IsCreated) vertexCPUBuffer.Dispose();
	}




	public static Vector3[] RecalculateNormals(ref NativeArray<Vector3> vertices, int[] triangleIndicies, Vector2[] uvs, HashSet<int> ignoreIndicies = null)
	{
		int verticesNum = vertices.Length;
		int indiciesNum = triangleIndicies.Length;

		var outNormals = new Vector3[verticesNum];
		int[] counts = new int[verticesNum];

		for (int i = 0; i <= indiciesNum - 3; i += 3)
		{
			int ai = triangleIndicies[i];
			int bi = triangleIndicies[i + 1];
			int ci = triangleIndicies[i + 2];

			if (ai < verticesNum && bi < verticesNum && ci < verticesNum)
			{
				if (ignoreIndicies != null && (ignoreIndicies.Contains(ai) || ignoreIndicies.Contains(bi) || ignoreIndicies.Contains(ci))) continue;

				Vector3 av = vertices[ai];
				Vector3 n = Vector3.Normalize(Vector3.Cross(
					vertices[bi] - av,
					vertices[ci] - av
				));

				outNormals[ai] += n;
				outNormals[bi] += n;
				outNormals[ci] += n;

				counts[ai]++;
				counts[bi]++;
				counts[ci]++;
			}
		}

		for (int i = 0; i < verticesNum; i++)
		{
			outNormals[i] /= counts[i];
		}

		return outNormals;
	}


	public static Vector4[] RecalculateTangents(ref NativeArray<Vector3> vertices, int[] triangleIndicies, Vector2[] uvs, HashSet<int> ignoreIndicies = null)
	{
		// inspired by http://www.opengl-tutorial.org/intermediate-tutorials/tutorial-13-normal-mapping/

		int verticesNum = vertices.Length;
		int indiciesNum = triangleIndicies.Length;

		var outTangents = new Vector4[verticesNum];
		int[] counts = new int[verticesNum];

		for (int i = 0; i <= indiciesNum - 3; i += 3)
		{
			int ai = triangleIndicies[i];
			int bi = triangleIndicies[i + 1];
			int ci = triangleIndicies[i + 2];

			if (ai < verticesNum && bi < verticesNum && ci < verticesNum)
			{
				if (ignoreIndicies != null && (ignoreIndicies.Contains(ai) || ignoreIndicies.Contains(bi) || ignoreIndicies.Contains(ci))) continue;

				Vector3 av = vertices[ai];
				Vector3 deltaPos1 = vertices[bi] - av;
				Vector3 deltaPos2 = vertices[ci] - av;

				Vector2 auv = uvs[ai];
				Vector2 deltaUV1 = uvs[bi] - auv;
				Vector2 deltaUV2 = uvs[ci] - auv;

				float r = 1.0f / (deltaUV1.x * deltaUV2.y - deltaUV1.y * deltaUV2.x);
				Vector4 t = (deltaPos1 * deltaUV2.y - deltaPos2 * deltaUV1.y) * r;

				outTangents[ai] += t;
				outTangents[bi] += t;
				outTangents[ci] += t;

				counts[ai]++;
				counts[bi]++;
				counts[ci]++;
			}
		}

		for (int i = 0; i < verticesNum; i++)
		{
			outTangents[i] /= counts[i];
		}

		return outTangents;
	}

}
