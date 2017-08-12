using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour
{

	public Planet planet;

	public Planet.PlanetConfig planetConfig { get { return planet.planetConfig; } }
	public Planet.ChunkConfig chunkConfig { get { return planet.chunkConfig; } }

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


	public static Chunk Create(Planet planet, Range range, ulong id, Chunk parent = null, ulong generation = 0, ChildPosition childPosition = ChildPosition.NoneNoParent)
	{
		var name = typeof(Chunk) + " id:#" + id + " generation:" + generation;

		var go = new GameObject(name);
		go.transform.parent = planet.transform;

		var segment = go.AddComponent<Chunk>();
		segment.planet = planet;
		segment.rangeToGenerateInto = range;
		segment.rangeToCalculateScreenSizeOn = range;
		segment.id = id;
		segment.generation = generation;
		segment.childPosition = childPosition;

		return segment;
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




	Mesh mesh;
	public void GenerateMesh()
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
		c.SetTexture(0, "_heightMap", planetConfig.planetHeightMap);

		c.Dispatch(0, chunkConfig.numberOfVerticesOnEdge, chunkConfig.numberOfVerticesOnEdge, 1);

		b.GetData(v);


		mesh = new Mesh();
		mesh.vertices = v;
		mesh.triangles = planet.GetSegmentIndicies();
		mesh.uv = planet.GetSefgmentUVs();
		mesh.RecalculateNormals();

		var go = gameObject;

		var meshFilter = go.AddComponent<MeshFilter>();
		meshFilter.mesh = mesh;

		var meshRenderer = go.AddComponent<MeshRenderer>();
		meshRenderer.material = chunkConfig.chunkMaterial;

		var meshCollider = go.AddComponent<MeshCollider>();
		meshCollider.sharedMesh = mesh;

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

	public void GenerateDiffuseMap()
	{
		var diffuse = new RenderTexture(256, 256, 1, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
		diffuse.enableRandomWrite = true;
		diffuse.Create();

		var c = chunkConfig.generateChunkDiffuseMap;
		c.SetTexture(0, "_texture", diffuse);
		rangeToGenerateInto.SetParams(c, "_range");


		c.Dispatch(0, diffuse.width, diffuse.height, 1);

		var meshRenderer = gameObject.GetComponent<MeshRenderer>();
		meshRenderer.material.mainTexture = diffuse;
	}

	public void GenerateNormalMap()
	{

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
	public void SetVisible(bool visible) // TODO: DestroyRenderer if visible == false for over CVar 60 seconds ?
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

	void DoRender(bool yes)
	{
		if (gameObject)
			gameObject.SetActive(yes);
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
