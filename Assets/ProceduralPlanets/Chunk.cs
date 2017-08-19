using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Chunk
{

	public Planet planet;

	public Planet.PlanetConfig planetConfig { get { return planet.planetConfig; } }
	public Planet.ChunkConfig chunkConfig { get { return planet.chunkConfig; } }

	public RenderTexture chunkHeightMap;
	public RenderTexture chunkNormalMap;
	public RenderTexture chunkDiffuseMap;

	public ulong id;
	public Chunk parent;
	public ulong generation;

	public Range rangePosRealSubdivided;

	public Range rangePosToCalculateScreenSizeOn;

	public Range rangePosToGenerateInto;
	public Range rangeDirToGenerateInto;
	public Range rangeLocalPosToGenerateInto;

	public Vector3 offsetFromPlanetCenter;

	public float chunkRangeMaxAngleDeg;
	public bool GenerateUsingPlanetGlobalPos { get { return chunkRangeMaxAngleDeg > 1; } }
	public float ChunkRelativeSize { get { return chunkRadius / planetConfig.radiusStart; } }

	public ChildPosition childPosition;

	public enum ChildPosition
	{
		NoneNoParent = 0,
		TopLeft = 1,
		TopRight = 2,
		BottomLeft = 3,
		BottomRight = 4,
	}


	public bool generationBegan;
	public bool isGenerationDone;
	public List<Chunk> children = new List<Chunk>(4);
	public float chunkRadius;

	const int resolution = 256;
	const int diffuseMapResolution = resolution;
	const int heightMapResolution = resolution;
	const int normalMapResolution = heightMapResolution; // must be the same

	public class Behavior : MonoBehaviour
	{
		public Chunk chunk;
		private void OnDrawGizmosSelected()
		{
			if (chunk != null)
				chunk.OnDrawGizmosSelected();
		}
	}

	public static Chunk Create(Planet planet, Range range, ulong id, Chunk parent = null, ulong generation = 0, ChildPosition childPosition = ChildPosition.NoneNoParent)
	{
		MyProfiler.BeginSample("Procedural Planet / Create chunk");

		var chunk = new Chunk();

		chunk.planet = planet;
		chunk.rangePosRealSubdivided = range;
		chunk.rangePosToCalculateScreenSizeOn = range;
		chunk.id = id;
		chunk.generation = generation;
		chunk.childPosition = childPosition;
		chunk.chunkRadius = range.ToBoundingSphere().radius;

		if (chunk.chunkConfig.useSkirts)
		{
			var ratio = ((chunk.chunkConfig.numberOfVerticesOnEdge - 1) / 2.0f) / ((chunk.chunkConfig.numberOfVerticesOnEdge - 1 - 2) / 2.0f);
			var center = range.CenterPos;
			var a = range.a - center;
			var b = range.b - center;
			var c = range.c - center;
			var d = range.d - center;
			range.a = a * ratio + center;
			range.b = b * ratio + center;
			range.c = c * ratio + center;
			range.d = d * ratio + center;
			chunk.rangePosToGenerateInto = range;
		}
		else
		{
			chunk.rangePosToGenerateInto = range;
		}

		chunk.rangeDirToGenerateInto = new Range
		{
			a = chunk.rangePosToGenerateInto.a.normalized,
			b = chunk.rangePosToGenerateInto.b.normalized,
			c = chunk.rangePosToGenerateInto.c.normalized,
			d = chunk.rangePosToGenerateInto.d.normalized,
		};

		chunk.offsetFromPlanetCenter = chunk.rangePosRealSubdivided.CenterPos;

		chunk.rangeLocalPosToGenerateInto = new Range
		{
			a = chunk.rangePosToGenerateInto.a - chunk.offsetFromPlanetCenter,
			b = chunk.rangePosToGenerateInto.b - chunk.offsetFromPlanetCenter,
			c = chunk.rangePosToGenerateInto.c - chunk.offsetFromPlanetCenter,
			d = chunk.rangePosToGenerateInto.d - chunk.offsetFromPlanetCenter,
		};

		chunk.chunkRangeMaxAngleDeg = Mathf.Max(
			Vector3.Angle(chunk.rangeDirToGenerateInto.a, chunk.rangeDirToGenerateInto.b),
			Vector3.Angle(chunk.rangeDirToGenerateInto.b, chunk.rangeDirToGenerateInto.c),
			Vector3.Angle(chunk.rangeDirToGenerateInto.c, chunk.rangeDirToGenerateInto.d),
			Vector3.Angle(chunk.rangeDirToGenerateInto.d, chunk.rangeDirToGenerateInto.a)
		);

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
			generation: generation + 1,
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

			var a = rangePosRealSubdivided.a;
			var b = rangePosRealSubdivided.b;
			var c = rangePosRealSubdivided.c;
			var d = rangePosRealSubdivided.d;
			var ab = Vector3.Normalize((a + b) / 2.0f);
			var ad = Vector3.Normalize((a + d) / 2.0f);
			var bc = Vector3.Normalize((b + c) / 2.0f);
			var dc = Vector3.Normalize((d + c) / 2.0f);
			var mid = Vector3.Normalize((ab + ad + dc + bc) / 4.0f);

			ab *= planetConfig.radiusStart;
			ad *= planetConfig.radiusStart;
			bc *= planetConfig.radiusStart;
			dc *= planetConfig.radiusStart;
			mid *= planetConfig.radiusStart;

			AddChild(a, ab, mid, ad, ChildPosition.TopLeft, 0);
			AddChild(ab, b, bc, mid, ChildPosition.TopRight, 1);
			AddChild(ad, mid, dc, d, ChildPosition.BottomLeft, 2);
			AddChild(mid, bc, c, dc, ChildPosition.BottomRight, 3);
		}
	}



	public void Generate()
	{
		if (generationBegan) return;
		generationBegan = true;
		
		MyProfiler.BeginSample("Procedural Planet / Generate chunk");

		if (gameObject) GameObject.Destroy(gameObject);

		MyProfiler.BeginSample("Procedural Planet / Generate chunk / Height map");
		GenerateHeightMap();
		MyProfiler.EndSample();
		
		MyProfiler.BeginSample("Procedural Planet / Generate chunk / Mesh");
		MyProfiler.BeginSample("Procedural Planet / Generate chunk / Mesh / Generate");
		GenerateMesh();
		MyProfiler.EndSample();
		MyProfiler.BeginSample("Procedural Planet / Generate chunk / Mesh / Prepare");
		PrepareMesh();
		MyProfiler.EndSample();
		MyProfiler.BeginSample("Procedural Planet / Generate chunk / Mesh / Skirts");
		MoveSkirtVertices();
		MyProfiler.EndSample();
		MyProfiler.BeginSample("Procedural Planet / Generate chunk / Mesh / Upload");
		UploadMesh();
		MyProfiler.EndSample();
		MyProfiler.EndSample();
		
		MyProfiler.BeginSample("Procedural Planet / Generate chunk / Normal map");
		GenerateNormalMap();
		MyProfiler.EndSample();
		
		MyProfiler.BeginSample("Procedural Planet / Generate chunk / Diffuse map");
		GenerateDiffuseMap();
		MyProfiler.EndSample();

		MyProfiler.EndSample();

		isGenerationDone = true;
	}




	void GenerateHeightMap()
	{
		if (chunkHeightMap) chunkHeightMap.Release();

		// pass 1
		var height1 = planet.chunkHeightFirstPassTemp;
		{

			if (height1 == null)
			{
				height1 = planet.chunkHeightFirstPassTemp = new RenderTexture(heightMapResolution, heightMapResolution, 0, RenderTextureFormat.RInt, RenderTextureReadWrite.Linear);
				height1.wrapMode = TextureWrapMode.Clamp;
				height1.filterMode = FilterMode.Trilinear;
				height1.enableRandomWrite = true;
				height1.Create();
			}

			var c = chunkConfig.generateChunkHeightMapPass1;
			c.SetTexture(0, "_planetHeightMap", planetConfig.planetHeightMap);
			rangeDirToGenerateInto.SetParams(c, "_range");
			c.SetTexture(0, "_chunkHeightMap", height1);

			c.Dispatch(0, height1.width / 16, height1.height / 16, 1);
		}

		// pass 2
		{
			var height2 = chunkHeightMap = new RenderTexture(heightMapResolution, heightMapResolution, 0, RenderTextureFormat.RInt, RenderTextureReadWrite.Linear);
			height2.wrapMode = TextureWrapMode.Clamp;
			height2.filterMode = FilterMode.Trilinear;
			height2.enableRandomWrite = true;
			height2.Create();

			var c = chunkConfig.generateChunkHeightMapPass2;
			c.SetTexture(0, "_chunkHeightMap", height1);
			c.SetFloat("_chunkRelativeSize", ChunkRelativeSize);
			rangeDirToGenerateInto.SetParams(c, "_rangeDir");
			c.SetTexture(0, "_chunkHeightMapNew", height2);

			c.Dispatch(0, height2.width / 16, height2.height / 16, 1);
		}

	}

	ComputeBuffer vertexGPUBuffer { get { return planet.chunkVertexGPUBuffer; } }
	Vector3[] vertexCPUBuffer { get { return planet.chunkVertexCPUBuffer; } }

	Mesh mesh;
	void GenerateMesh()
	{

		var c = chunkConfig.generateChunkVertices;
		var v = vertexCPUBuffer;
		var verticesOnEdge = chunkConfig.numberOfVerticesOnEdge;

		var kernelIndex = 0;
		if (GenerateUsingPlanetGlobalPos) kernelIndex = c.FindKernel("generateUsingPlanetGlobalPos");
		else kernelIndex = c.FindKernel("generateUsingChunkLocalPos");

		c.SetBuffer(kernelIndex, "_vertices", vertexGPUBuffer);
		rangeDirToGenerateInto.SetParams(c, "_rangeDir");
		rangePosToGenerateInto.SetParams(c, "_rangePos");
		rangeLocalPosToGenerateInto.SetParams(c, "_rangeLocalPos");
		c.SetInt("_numberOfVerticesOnEdge", verticesOnEdge);
		c.SetFloat("_planetRadiusStart", planetConfig.radiusStart);
		c.SetFloat("_planetRadiusHeightMapMultiplier", planetConfig.radiusHeightMapMultiplier);
		c.SetTexture(kernelIndex, "_chunkHeightMap", chunkHeightMap);


		c.Dispatch(kernelIndex, verticesOnEdge, verticesOnEdge, 1);

		MyProfiler.BeginSample("Procedural Planet / Generate chunk / Mesh / Generate / Download data");
		vertexGPUBuffer.GetData(v);
		MyProfiler.EndSample();

		{
			int aIndex = 0;
			int bIndex = verticesOnEdge - 1;
			int cIndex = verticesOnEdge * verticesOnEdge - 1;
			int dIndex = cIndex - (verticesOnEdge - 1);
			rangePosToCalculateScreenSizeOn.a = v[aIndex];
			rangePosToCalculateScreenSizeOn.b = v[bIndex];
			rangePosToCalculateScreenSizeOn.c = v[cIndex];
			rangePosToCalculateScreenSizeOn.d = v[dIndex];

			if (!GenerateUsingPlanetGlobalPos)
			{
				rangePosToCalculateScreenSizeOn.a += offsetFromPlanetCenter;
				rangePosToCalculateScreenSizeOn.b += offsetFromPlanetCenter;
				rangePosToCalculateScreenSizeOn.c += offsetFromPlanetCenter;
				rangePosToCalculateScreenSizeOn.d += offsetFromPlanetCenter;
			}
		}


	}

	void PrepareMesh()
	{
		if (mesh) Mesh.Destroy(mesh);
		// TODO: optimize: fill mesh vertices on GPU instead of CPU, calculate UVs, normals and tangents on GPU instead of CPU, remember we still need vertices on CPU for mesh collider
		mesh = new Mesh();
		mesh.name = this.ToString();
		mesh.vertices = vertexCPUBuffer;
		mesh.triangles = planet.GetSegmentIndicies();
		mesh.uv = planet.GetSefgmentUVs();
		mesh.RecalculateNormals();
		mesh.RecalculateTangents();
	}

	void MoveSkirtVertices()
	{
		if (chunkConfig.useSkirts)
		{
			var v = vertexCPUBuffer;
			var verticesOnEdge = chunkConfig.numberOfVerticesOnEdge;

			var decreaseSkirtsBy = -offsetFromPlanetCenter.normalized * (chunkRadius / 10.0f);
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
			mesh.vertices = vertexCPUBuffer;
		}
	}

	void UploadMesh()
	{
		mesh.UploadMeshData(false);
	}

	void CreateNormalMapFromMesh()
	{
		const int resolution = 256;

		if (chunkNormalMap != null && chunkNormalMap.width != resolution)
		{
			chunkNormalMap.Release();
			chunkNormalMap = null;
		}

		if (chunkNormalMap == null)
		{
			chunkNormalMap = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
			chunkNormalMap.wrapMode = TextureWrapMode.Clamp;
			chunkNormalMap.filterMode = FilterMode.Trilinear;
			chunkNormalMap.enableRandomWrite = true;
			chunkNormalMap.Create();
		}

		DoRender(true);
		planet.RenderNormalsToTexture(this.gameObject, chunkNormalMap);
	}

	void GenerateNormalMap()
	{
		if (chunkNormalMap != null && chunkNormalMap.width != normalMapResolution)
		{
			chunkNormalMap.Release();
			chunkNormalMap = null;
		}

		if (chunkNormalMap == null)
		{
			chunkNormalMap = new RenderTexture(normalMapResolution, normalMapResolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
			chunkNormalMap.enableRandomWrite = true;
			chunkNormalMap.Create();
		}

		var c = chunkConfig.generateChunkNormapMap;
		c.SetTexture(0, "_chunkHeightMap", chunkHeightMap);
		c.SetFloat("_chunkRelativeSize", ChunkRelativeSize);
		rangeDirToGenerateInto.SetParams(c, "_rangeDir");
		c.SetTexture(0, "_chunkNormalMap", chunkNormalMap);

		c.Dispatch(0, chunkNormalMap.width / 16, chunkNormalMap.height / 16, 1);

		if (material) material.SetTexture("_BumpMap", chunkNormalMap);
	}



	void GenerateDiffuseMap()
	{
		if (chunkDiffuseMap != null && chunkDiffuseMap.width != diffuseMapResolution)
		{
			chunkDiffuseMap.Release();
			chunkDiffuseMap = null;
		}

		if (chunkDiffuseMap == null)
		{
			chunkDiffuseMap = new RenderTexture(diffuseMapResolution, diffuseMapResolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
			chunkDiffuseMap.wrapMode = TextureWrapMode.Mirror;
			chunkDiffuseMap.filterMode = FilterMode.Trilinear;
			chunkDiffuseMap.enableRandomWrite = true;
			chunkDiffuseMap.useMipMap = true;
			chunkDiffuseMap.autoGenerateMips = false;
			chunkDiffuseMap.antiAliasing = 8;
			chunkDiffuseMap.Create();
		}

		var c = chunkConfig.generateChunkDiffuseMap;
		c.SetTexture(0, "_chunkHeightMap", chunkHeightMap);
		c.SetTexture(0, "_chunkDiffuseMap", chunkDiffuseMap);
		rangeDirToGenerateInto.SetParams(c, "_rangeDir");
		c.SetFloat("_chunkRelativeSize", ChunkRelativeSize);
		c.SetFloat("_textureSamplingLOD", Mathf.Max(0, 10 - generation));

		c.SetTexture(0, "_grass", chunkConfig.grass);
		c.SetTexture(0, "_clay", chunkConfig.clay);
		c.SetTexture(0, "_rock", chunkConfig.rock);

		c.Dispatch(0, chunkDiffuseMap.width / 16, chunkDiffuseMap.height / 16, 1);

		chunkDiffuseMap.GenerateMips();
		if (material) material.mainTexture = chunkDiffuseMap;
	}

	private void OnDrawGizmosSelected()
	{
		if (gameObject && gameObject.activeSelf)
		{
			Gizmos.color = Color.cyan;
			//rangePosRealSubdivided.DrawGizmos();
			rangePosToCalculateScreenSizeOn.DrawGizmos();
		}
	}


	bool lastVisible = false;
	public void SetVisible(bool visible)
	{
		if (this.generationBegan && this.isGenerationDone)
		{
			if (visible == lastVisible) return;
			lastVisible = visible;
		}

		if (visible)
		{
			if (this.generationBegan == false)
			{
				//Log.Warn("trying to show segment " + this + " that did not begin generation");
			}
			else if (this.isGenerationDone == false)
			{
				//Log.Warn("trying to show segment " + this + " that did not finish generation");
			}
			else DoRender(true);
		}
		else
		{
			DoRender(false);
		}
	}

	public void HideAllChildren()
	{
		foreach (var child in children)
		{
			child.SetVisible(false);
			child.HideAllChildren();
		}
	}

	public void MarkForRegeneration()
	{
		generationBegan = false;
		isGenerationDone = false;
		if (gameObject) GameObject.Destroy(gameObject);
		lastVisible = false;
	}

	void DestroyData()
	{
		if (gameObject) GameObject.Destroy(gameObject);
		if (mesh) Mesh.Destroy(mesh);
		if (chunkNormalMap) chunkNormalMap.Release();
		if (chunkDiffuseMap) chunkDiffuseMap.Release();
		if (chunkHeightMap) chunkHeightMap.Release();
	}

	void TryCreateGameObject()
	{
		if (gameObject) return;

		MyProfiler.BeginSample("Procedural Planet / Create GameObject");

		var name = ToString();

		var go = gameObject = new GameObject(name);
		go.transform.parent = planet.transform;
		if (!GenerateUsingPlanetGlobalPos)
			go.transform.localPosition = offsetFromPlanetCenter;

		var behavior = go.AddComponent<Behavior>();
		behavior.chunk = this;

		var meshFilter = go.AddComponent<MeshFilter>();
		meshFilter.mesh = mesh;

		var meshRenderer = go.AddComponent<MeshRenderer>();
		meshRenderer.sharedMaterial = chunkConfig.chunkMaterial;
		material = meshRenderer.material;

		MyProfiler.BeginSample("Procedural Planet / Create GameObject / Collider");
		var meshCollider = go.AddComponent<MeshCollider>();
		meshCollider.sharedMesh = mesh;
		MyProfiler.EndSample();

		if (chunkDiffuseMap) material.mainTexture = chunkDiffuseMap;
		if (chunkNormalMap) material.SetTexture("_BumpMap", chunkNormalMap);

		MyProfiler.EndSample();
	}

	DateTime notRenderedTimeStamp;
	GameObject gameObject;
	Material material;
	void DoRender(bool doRender)
	{
		if (doRender)
		{
			if (isGenerationDone)
				TryCreateGameObject();

			if (gameObject != null && !gameObject.activeSelf)
				gameObject.SetActive(true);
		}
		else
		{
			if (gameObject != null)
			{
				if (gameObject.activeSelf)
				{
					notRenderedTimeStamp = DateTime.UtcNow;
					gameObject.SetActive(false);

					// TODO: schedule CleanUpChance(); for execution in chunkConfig.destroyGameObjectIfNotVisibleForSeconds
				}
				else
				{
					CleanUpChance();
				}
			}
		}
	}

	void CleanUpChance()
	{
		if (!gameObject.activeSelf)
		{
			if ((DateTime.UtcNow - notRenderedTimeStamp).TotalSeconds > chunkConfig.destroyGameObjectIfNotVisibleForSeconds)
			{
				GameObject.Destroy(gameObject);
				gameObject = null;
			}
		}
	}


	private float GetSizeOnScreen(Planet.SubdivisionData data)
	{
		var myPos = rangePosToCalculateScreenSizeOn.CenterPos + planet.transform.position;
		var distanceToCamera = Vector3.Distance(myPos, data.pos);

		// TODO: this is world space, doesnt take into consideration rotation, not good, but we dont care about rotation ?, we want to have correct detail even if looking from side
		var sphere = rangePosToCalculateScreenSizeOn.ToBoundingSphere();
		var radiusWorldSpace = sphere.radius;
		var fov = data.fieldOfView;
		var cot = 1.0f / Mathf.Tan(fov / 2f * Mathf.Deg2Rad);
		var radiusCameraSpace = radiusWorldSpace * cot / distanceToCamera;

		return radiusCameraSpace;
	}

	public float lastGenerationWeight;
	public float GetGenerationWeight(Planet.SubdivisionData data)
	{
		var weight = GetSizeOnScreen(data);
		lastGenerationWeight = weight;
		return weight;
	}

	public override string ToString()
	{
		return typeof(Chunk) + " id:#" + id + " generation:" + generation;
	}


}
