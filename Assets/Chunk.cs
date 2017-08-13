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

	public Range rangeToGenerateInto;
	public Range rangeToCalculateScreenSizeOn;

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


	public class Behavior : MonoBehaviour
	{
		public Chunk chunk;
		private void OnDrawGizmos()
		{
			if (chunk != null)
				chunk.OnDrawGizmos();
		}
	}

	public static Chunk Create(Planet planet, Range range, ulong id, Chunk parent = null, ulong generation = 0, ChildPosition childPosition = ChildPosition.NoneNoParent)
	{
		var chunk = new Chunk();
		chunk.planet = planet;
		chunk.rangeToGenerateInto = range;
		chunk.rangeToCalculateScreenSizeOn = range;
		chunk.id = id;
		chunk.generation = generation;
		chunk.childPosition = childPosition;

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

			var a = rangeToGenerateInto.a;
			var b = rangeToGenerateInto.b;
			var c = rangeToGenerateInto.c;
			var d = rangeToGenerateInto.d;
			var ab = Vector3.Normalize((a + b) / 2.0f);
			var ad = Vector3.Normalize((a + d) / 2.0f);
			var bc = Vector3.Normalize((b + c) / 2.0f);
			var dc = Vector3.Normalize((d + c) / 2.0f);
			var mid = Vector3.Normalize((ab + ad + dc + bc) / 4.0f);

			ab *= planetConfig.radiusMin;
			ad *= planetConfig.radiusMin;
			bc *= planetConfig.radiusMin;
			dc *= planetConfig.radiusMin;
			mid *= planetConfig.radiusMin;

			AddChild(a, ab, mid, ad, ChildPosition.TopLeft, 0);
			AddChild(ab, b, bc, mid, ChildPosition.TopRight, 1);
			AddChild(ad, mid, dc, d, ChildPosition.BottomLeft, 2);
			AddChild(mid, bc, c, dc, ChildPosition.BottomRight, 3);
		}
	}



	public void Generate()
	{
		GenerateHeightMap();
		GenerateMesh();
		CreateNormalMapFromMesh();
		//GenerateNormalMap();
		GenerateDiffuseMap();
	}


	void GenerateHeightMap()
	{
		var height = chunkHeightMap = new RenderTexture(64 * 16, 64 * 16, 1, RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
		height.depth = 0;
		height.enableRandomWrite = true;
		height.Create();

		var c = chunkConfig.generateChunkHeightMap;
		c.SetTexture(0, "_planetHeightMap", planetConfig.planetHeightMap);
		c.SetTexture(0, "_chunkHeightMap", height);
		rangeToGenerateInto.SetParams(c, "_range");

		c.Dispatch(0, height.width / 16, height.height / 16, 1);
	}



	Mesh mesh;
	void GenerateMesh()
	{
		if (generationBegan) return;
		generationBegan = true;

		var v = planet.GetSegmentVertices();

		var b = new ComputeBuffer(v.Length, 3 * sizeof(float));
		b.SetData(v);

		var c = chunkConfig.generateChunkVertices;
		c.SetBuffer(0, "_vertices", b);
		rangeToGenerateInto.SetParams(c, "_range");
		c.SetInt("_numberOfVerticesOnEdge", chunkConfig.numberOfVerticesOnEdge);
		c.SetFloat("_radiusBase", planetConfig.radiusMin);
		c.SetFloat("_radiusHeightMap", planetConfig.radiusVariation);
		c.SetTexture(0, "_chunkHeightMap", chunkHeightMap);

		c.Dispatch(0, chunkConfig.numberOfVerticesOnEdge, chunkConfig.numberOfVerticesOnEdge, 1);

		b.GetData(v);


		mesh = new Mesh();
		mesh.vertices = v;
		mesh.triangles = planet.GetSegmentIndicies();
		mesh.uv = planet.GetSefgmentUVs();
		mesh.RecalculateNormals();
		mesh.RecalculateTangents();

		{
			int aIndex = 0;
			int bIndex = chunkConfig.numberOfVerticesOnEdge - 1;
			int cIndex = chunkConfig.numberOfVerticesOnEdge * chunkConfig.numberOfVerticesOnEdge - 1;
			int dIndex = cIndex - (chunkConfig.numberOfVerticesOnEdge - 1);
			rangeToCalculateScreenSizeOn.a = v[aIndex];
			rangeToCalculateScreenSizeOn.b = v[bIndex];
			rangeToCalculateScreenSizeOn.c = v[cIndex];
			rangeToCalculateScreenSizeOn.d = v[dIndex];
		}

		isGenerationDone = true;
	}

	void CreateNormalMapFromMesh()
	{
		const int resolution = 512;
		var normal = chunkNormalMap = new RenderTexture(resolution, resolution, 1, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
		normal.depth = 0;
		normal.enableRandomWrite = true;
		normal.Create();

		DoRender(true);
		planet.RenderNormalsToTexture(this.gameObject, normal);
	}

	void GenerateNormalMap()
	{
		const int resolution = 512;
		var normal = chunkNormalMap = new RenderTexture(resolution, resolution, 1, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
		normal.depth = 0;
		normal.enableRandomWrite = true;
		normal.Create();

		var c = chunkConfig.generateChunkNormapMap;
		c.SetTexture(0, "_chunkHeightMap", chunkHeightMap);
		c.SetTexture(0, "_chunkNormalMap", chunkNormalMap);
		rangeToGenerateInto.SetParams(c, "_range");

		c.Dispatch(0, normal.width / 16, normal.height / 16, 1);

		//if (material) material.SetTexture("_BumpMap", chunkNormalMap);
	}



	void GenerateDiffuseMap()
	{
		const int resolution = 512;
		var diffuse = chunkDiffuseMap = new RenderTexture(resolution, resolution, 1, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
		diffuse.depth = 0;
		diffuse.enableRandomWrite = true;
		diffuse.Create();

		var c = chunkConfig.generateChunkDiffuseMap;
		c.SetTexture(0, "_chunkNormalMap", chunkNormalMap);
		c.SetTexture(0, "_chunkDiffuseMap", diffuse);
		rangeToGenerateInto.SetParams(c, "_range");
		c.SetFloat("_chunkRelativeSize", this.rangeToGenerateInto.ToBoundingSphere().radius / planetConfig.radiusMin);

		c.Dispatch(0, diffuse.width / 16, diffuse.height / 16, 1);

		if (material) material.mainTexture = chunkDiffuseMap;
	}

	private void OnDrawGizmos()
	{
		if (gameObject && gameObject.activeSelf)
		{
			Gizmos.color = Color.cyan;
			Gizmos.DrawLine(rangeToGenerateInto.a, rangeToGenerateInto.b);
			Gizmos.DrawLine(rangeToGenerateInto.b, rangeToGenerateInto.c);
			Gizmos.DrawLine(rangeToGenerateInto.c, rangeToGenerateInto.d);
			Gizmos.DrawLine(rangeToGenerateInto.d, rangeToGenerateInto.a);

			Gizmos.color = Color.red;
			Gizmos.DrawLine(rangeToCalculateScreenSizeOn.a, rangeToCalculateScreenSizeOn.b);
			Gizmos.DrawLine(rangeToCalculateScreenSizeOn.b, rangeToCalculateScreenSizeOn.c);
			Gizmos.DrawLine(rangeToCalculateScreenSizeOn.c, rangeToCalculateScreenSizeOn.d);
			Gizmos.DrawLine(rangeToCalculateScreenSizeOn.d, rangeToCalculateScreenSizeOn.a);
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

	DateTime notRenderedTimeStamp;
	GameObject gameObject;
	Material material;
	void DoRender(bool doRender)
	{
		if (doRender)
		{
			if (gameObject == null)
			{
				var name = typeof(Chunk) + " id:#" + id + " generation:" + generation;

				var go = gameObject = new GameObject(name);
				go.transform.parent = planet.transform;

				var behavior = go.AddComponent<Behavior>();
				behavior.chunk = this;

				var meshFilter = go.AddComponent<MeshFilter>();
				meshFilter.mesh = mesh;

				var meshRenderer = go.AddComponent<MeshRenderer>();
				meshRenderer.sharedMaterial = chunkConfig.chunkMaterial;
				material = meshRenderer.material;

				var meshCollider = go.AddComponent<MeshCollider>();
				meshCollider.sharedMesh = mesh;

				if (chunkDiffuseMap) material.mainTexture = chunkDiffuseMap;

				// if (chunkNormalMap) material.SetTexture("_BumpMap", chunkNormalMap);
			}

			if (!gameObject.activeSelf)
				gameObject.SetActive(true);
		}
		else
		{
			if (gameObject)
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
		var myPos = rangeToCalculateScreenSizeOn.CenterPos + planet.transform.position;
		var distanceToCamera = Vector3.Distance(myPos, data.pos);

		// TODO: this is world space, doesnt take into consideration rotation, not good, but we dont care about rotation ?, we want to have correct detail even if looking from side
		var sphere = rangeToCalculateScreenSizeOn.ToBoundingSphere();
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


}
