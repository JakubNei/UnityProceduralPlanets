using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

[Serializable]
public class Chunk
{

	public Planet planet;

	public ulong id;
	public Chunk parent;
	public int treeDepth;

	public Range rangeUnitCubePosRealSubdivided;
	public Range rangeUnitCubePosToGenerateInto;
	public Range rangePosToCalculateScreenSizeOn;


	public Vector3 offsetFromPlanetCenter;

	public float chunkRangeMaxAngleDeg;


	// float slope = (abs(sx0 - s0) + abs(sy0 - s0)) / _chunkRelativeSize * (w / 256.0);
	//public float ChunkRelativeSize { get { return chunkRadius / planetConfig.radiusStart; } }

	//public bool GenerateUsingPlanetGlobalPos { get { return chunkRangeMaxAngleDeg > 2; } }
	public bool GenerateUsingPlanetGlobalPos { get { return true; } }
	//public int SlopeModifier { get { return (int)Mathf.Pow(2, generation); } }
	//public float SlopeModifier { get { return (float)((planetConfig.radiusStart / chunkRadius / 4 * (heightMapResolution / 1024.0)) / 44.0 * HeightRange); } }
	public float SlopeModifier { get { return (float)(Mathf.Pow(2, treeDepth) * (HeightMapResolution / 1024.0) * HeightRange); } }
	public float HeightRange { get { return heightMax - heightMin; } }

	public Planet.PlanetConfig planetConfig { get { return planet.planetConfig; } }
	public Planet.ChunkConfig chunkConfig { get { return planet.chunkConfig; } }

	[Serializable]
	public struct ChunkGeneratedData
	{
		public RenderTexture chunkHeightMap;
		public RenderTexture chunkNormalMap;
		public RenderTexture chunkDiffuseMap;
		public RenderTexture chunkSlopeAndCurvatureMap;
		public RenderTexture chunkBiomesMap;
		public Mesh mesh;
	};

	public ChunkGeneratedData generatedData;

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

	[SerializeField]
	bool _generationBegan;
	public bool generationBegan
	{
		get { return _generationBegan; }
		set
		{
			if (value != _generationBegan)
			{
				_generationBegan = value;

				// debug
				if (value) planet.chunksGenerationBegan++;
				else planet.chunksGenerationBegan--;
			}
		}
	}

	[SerializeField]
	bool _isGenerationDone;
	public bool isGenerationDone
	{
		get { return _isGenerationDone; }
		set
		{
			if (value != _isGenerationDone)
			{
				_isGenerationDone = value;

				// debug
				if (value) planet.chunksGenerationDone++;
				else planet.chunksGenerationDone--;
			}
		}
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


		chunk.rangePosToCalculateScreenSizeOn = new Range
		{
			a = chunk.rangeUnitCubePosToGenerateInto.a.normalized * planet.planetConfig.radiusStart,
			b = chunk.rangeUnitCubePosToGenerateInto.b.normalized * planet.planetConfig.radiusStart,
			c = chunk.rangeUnitCubePosToGenerateInto.c.normalized * planet.planetConfig.radiusStart,
			d = chunk.rangeUnitCubePosToGenerateInto.d.normalized * planet.planetConfig.radiusStart,
		};

		chunk.chunkRadius = chunk.rangePosToCalculateScreenSizeOn.ToBoundingSphere().radius;


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


	static float lastGetMeshDataTookSeconds;

	public IEnumerator StartGenerateCoroutine()
	{
		if (generationBegan) yield break;
		generationBegan = true;

		MyProfiler.BeginSample("Procedural Planet / Generate chunk / Height map");
		GenerateHeightMap();
		MyProfiler.EndSample();
		yield return null;

		MyProfiler.BeginSample("Procedural Planet / Generate chunk / Mesh / Generate on GPU");
		GenerateMesh();
		MyProfiler.EndSample();

		MyProfiler.BeginSample("Procedural Planet / Generate chunk / Normal map");
		GenerateNormalMap();
		MyProfiler.EndSample();
		yield return null;

		MyProfiler.BeginSample("Procedural Planet / Generate chunk / Diffuse map");
		GenerateDiffuseMap();
		MyProfiler.EndSample();
		yield return null;

		//yield return new WaitForSecondsRealtime(lastGetMeshDataTookSeconds);
		MyProfiler.BeginSample("Procedural Planet / Generate chunk / Mesh / Get data from GPU to CPU");
		var getMeshDataSW = Stopwatch.StartNew();
		GetMeshData();
		lastGetMeshDataTookSeconds = getMeshDataSW.ElapsedMilliseconds / 1000f;
		MyProfiler.EndSample();
		yield return null;

		MyProfiler.BeginSample("Procedural Planet / Generate chunk / Mesh / Create on CPU");
		CreateMesh();
		MyProfiler.EndSample();
		yield return null;

		MyProfiler.BeginSample("Procedural Planet / Generate chunk / Mesh / Move skirts on CPU");
		MoveSkirtVertices();
		MyProfiler.EndSample();
		yield return null;

		MyProfiler.BeginSample("Procedural Planet / Generate chunk / Mesh / Upload to GPU");
		UploadMesh();
		MyProfiler.EndSample();
		yield return null;

		CleanupAfterGeneration();


		isGenerationDone = true;
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
		if (generatedData.chunkHeightMap != null) c.SetTexture(kernelIndex, "_chunkHeightMap", generatedData.chunkHeightMap);
		if (generatedData.chunkNormalMap != null) c.SetTexture(kernelIndex, "_chunkNormalMap", generatedData.chunkNormalMap);
		if (generatedData.chunkSlopeAndCurvatureMap != null) c.SetTexture(kernelIndex, "_chunkSlopeAndCurvatureMap", generatedData.chunkSlopeAndCurvatureMap);
		if (generatedData.chunkBiomesMap != null) c.SetTexture(kernelIndex, "_chunkBiomesMap", generatedData.chunkBiomesMap);

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
		// pass 0
		if (chunkConfig.rescaleToMinMax)
		{
			var heightRough = new RenderTexture(HeightMapResolution/2, HeightMapResolution/2, 0, RenderTextureFormat.RInt, RenderTextureReadWrite.Linear);
			heightRough.wrapMode = TextureWrapMode.Clamp;
			heightRough.filterMode = FilterMode.Bilinear;
			heightRough.enableRandomWrite = true;
			heightRough.Create();

			var c = chunkConfig.generateChunkHeightMapPass1;
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

		// pass 1
		RenderTexture height1;
		{
			height1 = new RenderTexture(HeightMapResolution, HeightMapResolution, 0, RenderTextureFormat.RInt, RenderTextureReadWrite.Linear);
			height1.wrapMode = TextureWrapMode.Clamp;
			height1.filterMode = FilterMode.Bilinear;
			height1.enableRandomWrite = true;
			height1.Create();

			var c = chunkConfig.generateChunkHeightMapPass1;
			c.SetTexture(0, "_planetHeightMap", planetConfig.planetHeightMap);
			rangeUnitCubePosToGenerateInto.SetParams(c, "_rangeUnitCubePos");
			c.SetFloat("_heightMin", heightMin);
			c.SetFloat("_heightMax", heightMax);

			c.SetTexture(0, "_chunkHeightMap", height1);
			c.Dispatch(0, height1.width / 16, height1.height / 16, 1);

			GenerateSlopeAndCurvatureMap(height1);
		}

		// pass 2
		{
			if (generatedData.chunkHeightMap == null)
			{
				generatedData.chunkHeightMap = new RenderTexture(HeightMapResolution, HeightMapResolution, 0, RenderTextureFormat.RInt, RenderTextureReadWrite.Linear);
				generatedData.chunkHeightMap.wrapMode = TextureWrapMode.Clamp;
				generatedData.chunkHeightMap.filterMode = FilterMode.Bilinear;
				generatedData.chunkHeightMap.enableRandomWrite = true;
				generatedData.chunkHeightMap.Create();
			}

			var c = chunkConfig.generateChunkHeightMapPass2;
			SetAll(c, 0);
			c.SetTexture(0, "_chunkHeightMap", height1);

			c.SetTexture(0, "_chunkHeightMapNew", generatedData.chunkHeightMap);
			c.Dispatch(0, generatedData.chunkHeightMap.width / 16, generatedData.chunkHeightMap.height / 16, 1);

			if (generatedData.chunkHeightMap.useMipMap) generatedData.chunkHeightMap.GenerateMips();

			//GenerateSlopeAndCurvatureMap(chunkHeightMap);
		}

		height1.Release();


	}


	void GenerateSlopeAndCurvatureMap(RenderTexture heightMap)
	{
		if (generatedData.chunkSlopeAndCurvatureMap == null)
		{
			generatedData.chunkSlopeAndCurvatureMap = new RenderTexture(SlopeMapResolution, SlopeMapResolution, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
			generatedData.chunkSlopeAndCurvatureMap.wrapMode = TextureWrapMode.Clamp;
			generatedData.chunkSlopeAndCurvatureMap.filterMode = FilterMode.Bilinear;
			generatedData.chunkSlopeAndCurvatureMap.enableRandomWrite = true;
			generatedData.chunkSlopeAndCurvatureMap.Create();
		}


		var c = chunkConfig.generateSlopeAndCurvatureMap;

		var kernelIndex = 0;
		if (parent != null) kernelIndex = c.FindKernel("parentExists");
		else kernelIndex = c.FindKernel("parentDoesNotExist");

		SetAll(c, kernelIndex);
		if (parent != null)
			c.SetTexture(kernelIndex, "_parentChunkSlopeAndCurvatureMap", parent.generatedData.chunkSlopeAndCurvatureMap);
		c.SetTexture(kernelIndex, "_chunkHeightMap", heightMap);

		c.SetTexture(kernelIndex, "_chunkSlopeAndCurvatureMap", generatedData.chunkSlopeAndCurvatureMap);
		c.Dispatch(kernelIndex, generatedData.chunkSlopeAndCurvatureMap.width / 16, generatedData.chunkSlopeAndCurvatureMap.height / 16, 1);
	}


	void GenerateChunkBiomesMap()
	{

		if (generatedData.chunkBiomesMap == null)
		{
			generatedData.chunkBiomesMap = new RenderTexture(BiomesMapResolution, BiomesMapResolution, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
			generatedData.chunkBiomesMap.wrapMode = TextureWrapMode.Clamp;
			generatedData.chunkBiomesMap.filterMode = FilterMode.Bilinear;
			generatedData.chunkBiomesMap.enableRandomWrite = true;
			generatedData.chunkBiomesMap.Create();
		}

		var c = chunkConfig.generateChunkBiomesMap;
		SetAll(c, 0);

		c.SetTexture(0, "_chunkBiomesMap", generatedData.chunkBiomesMap);
		c.Dispatch(0, generatedData.chunkBiomesMap.width / 16, generatedData.chunkBiomesMap.height / 16, 1);
	}


	//ComputeBuffer vertexGPUBuffer { get { return planet.chunkVertexGPUBuffer; } }
	ComputeBuffer vertexGPUBuffer;
	Vector3[] vertexCPUBuffer { get { return planet.chunkVertexCPUBuffer; } }

	void GenerateMesh()
	{
		vertexGPUBuffer = new ComputeBuffer(chunkConfig.NumberOfVerticesNeededTotal, 3 * sizeof(float));
		var c = chunkConfig.generateChunkVertices;
		var verticesOnEdge = chunkConfig.numberOfVerticesOnEdge;

		var kernelIndex = 0;
		if (GenerateUsingPlanetGlobalPos) kernelIndex = c.FindKernel("generateUsingPlanetGlobalPos");
		else kernelIndex = c.FindKernel("generateUsingChunkLocalPos");

		rangeUnitCubePosToGenerateInto.SetParams(c, "_rangeUnitCubePos");
		c.SetInt("_numberOfVerticesOnEdge", verticesOnEdge);
		c.SetFloat("_planetRadiusStart", planetConfig.radiusStart);
		c.SetFloat("_planetRadiusHeightMapMultiplier", planetConfig.radiusHeightMapMultiplier);
		c.SetTexture(kernelIndex, "_chunkHeightMap", generatedData.chunkHeightMap);
		c.SetFloat("_heightMin", heightMin);
		c.SetFloat("_heightMax", heightMax);

		c.SetBuffer(kernelIndex, "_vertices", vertexGPUBuffer);
		c.Dispatch(kernelIndex, verticesOnEdge, verticesOnEdge, 1);

	}

	void GetMeshData()
	{
		vertexGPUBuffer.GetData(vertexCPUBuffer);
	}

	void CreateMesh()
	{
		var verticesOnEdge = chunkConfig.numberOfVerticesOnEdge;

		{
			int aIndex = 0;
			int bIndex = verticesOnEdge - 1;
			int cIndex = verticesOnEdge * verticesOnEdge - 1;
			int dIndex = cIndex - (verticesOnEdge - 1);
			rangePosToCalculateScreenSizeOn.a = vertexCPUBuffer[aIndex];
			rangePosToCalculateScreenSizeOn.b = vertexCPUBuffer[bIndex];
			rangePosToCalculateScreenSizeOn.c = vertexCPUBuffer[cIndex];
			rangePosToCalculateScreenSizeOn.d = vertexCPUBuffer[dIndex];

			if (!GenerateUsingPlanetGlobalPos)
			{
				rangePosToCalculateScreenSizeOn.a += offsetFromPlanetCenter;
				rangePosToCalculateScreenSizeOn.b += offsetFromPlanetCenter;
				rangePosToCalculateScreenSizeOn.c += offsetFromPlanetCenter;
				rangePosToCalculateScreenSizeOn.d += offsetFromPlanetCenter;
			}
		}

		if (generatedData.mesh) Mesh.Destroy(generatedData.mesh);
		// TODO: optimize: fill mesh vertices on GPU instead of CPU, remember we still need vertices on CPU for mesh collider
		generatedData.mesh = new Mesh();
		generatedData.mesh.name = this.ToString();
		generatedData.mesh.vertices = vertexCPUBuffer;
		generatedData.mesh.triangles = planet.GetSegmentIndicies();
		generatedData.mesh.uv = planet.GetChunkUVs();
		generatedData.mesh.tangents = new Vector4[] { };
		generatedData.mesh.normals = new Vector3[] { };
	}

	void MoveSkirtVertices()
	{
		if (chunkConfig.useSkirts)
		{
			var v = vertexCPUBuffer;
			var verticesOnEdge = chunkConfig.numberOfVerticesOnEdge;

			var decreaseSkirtsBy = -offsetFromPlanetCenter.normalized * chunkRadius / 2.0f;
			for (int i = 0; i < verticesOnEdge; i++)
			{
				v[i] += decreaseSkirtsBy; // top line
				v[verticesOnEdge * (verticesOnEdge - 1) + i] += decreaseSkirtsBy; // bottom line
			}
			for (int i = 1; i < verticesOnEdge - 1; i++)
			{
				v[verticesOnEdge * i] += decreaseSkirtsBy; // left line
				v[verticesOnEdge * i + verticesOnEdge - 1] += decreaseSkirtsBy; // right line
			}
			generatedData.mesh.vertices = vertexCPUBuffer;
		}
	}

	void UploadMesh()
	{
		generatedData.mesh.UploadMeshData(false);
	}


	void GenerateNormalMap()
	{
		var c = chunkConfig.generateChunkNormapMap;
		if (c == null) return;

		if (generatedData.chunkNormalMap != null && generatedData.chunkNormalMap.width != NormalMapResolution)
		{
			generatedData.chunkNormalMap.Release();
			generatedData.chunkNormalMap = null;
		}

		if (generatedData.chunkNormalMap == null)
		{
			generatedData.chunkNormalMap = new RenderTexture(NormalMapResolution, NormalMapResolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
			generatedData.chunkNormalMap.wrapMode = TextureWrapMode.Clamp;
			generatedData.chunkNormalMap.filterMode = FilterMode.Bilinear;
			generatedData.chunkNormalMap.enableRandomWrite = true;
			generatedData.chunkNormalMap.Create();
		}

		SetAll(c, 0);

		var _normalLength = chunkRadius / generatedData.chunkNormalMap.width / (planetConfig.radiusHeightMapMultiplier * HeightRange);
		//UnityEngine.Debug.Log(_normalLength);
		c.SetFloat("_normalLength", _normalLength);

		c.SetTexture(0, "_chunkNormalMap", generatedData.chunkNormalMap);
		c.Dispatch(0, generatedData.chunkNormalMap.width / 16, generatedData.chunkNormalMap.height / 16, 1);
	}



	void GenerateDiffuseMap()
	{
		if (generatedData.chunkDiffuseMap != null && generatedData.chunkDiffuseMap.width != DiffuseMapResolution)
		{
			generatedData.chunkDiffuseMap.Release();
			generatedData.chunkDiffuseMap = null;
		}

		if (generatedData.chunkDiffuseMap == null)
		{
			generatedData.chunkDiffuseMap = new RenderTexture(DiffuseMapResolution, DiffuseMapResolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
			generatedData.chunkDiffuseMap.wrapMode = TextureWrapMode.Clamp;
			generatedData.chunkDiffuseMap.filterMode = FilterMode.Trilinear;
			generatedData.chunkDiffuseMap.enableRandomWrite = true;
			generatedData.chunkDiffuseMap.Create();
		}

		var c = chunkConfig.generateChunkDiffuseMap;
		SetAll(c, 0);
		c.SetFloat("_mipMapLevel", Mathf.Max(1, chunkRadius / 10.0f) - 1);
		c.SetTexture(0, "_grass", chunkConfig.grass);
		c.SetTexture(0, "_clay", chunkConfig.clay);
		c.SetTexture(0, "_rock", chunkConfig.rock);

		c.SetTexture(0, "_chunkDiffuseMap", generatedData.chunkDiffuseMap);
		c.Dispatch(0, generatedData.chunkDiffuseMap.width / 16, generatedData.chunkDiffuseMap.height / 16, 1);

		if (generatedData.chunkDiffuseMap.useMipMap) generatedData.chunkDiffuseMap.GenerateMips();
	}

	void CleanupAfterGeneration()
	{
		if (Application.platform == RuntimePlatform.WindowsEditor)
			return; // in editor we want to see and debug stuff

		if (generatedData.chunkHeightMap) generatedData.chunkHeightMap.Release();
		generatedData.chunkHeightMap = null;
	}


	public void MarkForRegeneration()
	{
		generationBegan = false;
		isGenerationDone = false;
	}


	public void Destroy()
	{
		MarkForRegeneration();

		if (generatedData.mesh) Mesh.Destroy(generatedData.mesh);
		generatedData.mesh = null;

		if (generatedData.chunkHeightMap) generatedData.chunkHeightMap.Release();
		generatedData.chunkHeightMap = null;
		if (generatedData.chunkNormalMap) generatedData.chunkNormalMap.Release();
		generatedData.chunkNormalMap = null;
		if (generatedData.chunkDiffuseMap) generatedData.chunkDiffuseMap.Release();
		generatedData.chunkDiffuseMap = null;
		if (generatedData.chunkSlopeAndCurvatureMap) generatedData.chunkSlopeAndCurvatureMap.Release();
		generatedData.chunkSlopeAndCurvatureMap = null;
	}

	private float GetSizeOnScreen(Planet.PointOfInterest data)
	{
		var myPos = rangePosToCalculateScreenSizeOn.CenterPos + planet.transform.position;
		var distanceToCamera = Vector3.Distance(myPos, data.pos);

		// TODO: this is world space, doesnt take into consideration rotation, not good,
		// but we dont care about rotation, we want to have correct detail even if we are looking at chunk from side?
		var sphere = rangePosToCalculateScreenSizeOn.ToBoundingSphere();
		var radiusWorldSpace = sphere.radius;
		var fov = data.fieldOfView;
		var cot = 1.0f / Mathf.Tan(fov / 2f * Mathf.Deg2Rad);
		var radiusCameraSpace = radiusWorldSpace * cot / distanceToCamera;

		return radiusCameraSpace;
	}

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


}
