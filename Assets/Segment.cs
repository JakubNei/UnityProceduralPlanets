using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Segment : MonoBehaviour
{

	public Planet planet;
	public Range range;
	public ulong id;
	public Segment parent;
	public ulong generation;

	public Range RangeToGenerateInto { get { return range; } }
	public Range RangeToCalculateScreenSizeOn { get { return range; } }

	public ChildPosition childPosition;

	public enum ChildPosition
	{
		NoneNoParent = 0,
		Top = 1,
		Left = 2,
		Middle = 3,
		Right = 4,
	}


	public bool generationBegan;
	public bool isGenerationDone;
	public List<Segment> children = new List<Segment>(4);


	public static Segment Create(Planet planet, Range range, ulong id, Segment parent = null, ulong generation = 0, ChildPosition childPosition = ChildPosition.NoneNoParent)
	{
		var name = typeof(Segment) + " id:#" + id + " generation:" + generation;

		var go = new GameObject(name);
		go.transform.parent = planet.transform;

		var segment = go.AddComponent<Segment>();
		segment.planet = planet;
		segment.range = range;
		segment.id = id;
		segment.generation = generation;
		segment.childPosition = childPosition;

		return segment;
	}

	private void AddChild(Vector3 a, Vector3 b, Vector3 c, ChildPosition cp, ushort index)
	{
		var range = new Range()
		{
			a = a,
			b = b,
			c = c
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
			var a = range.a;
			var b = range.b;
			var c = range.c;
			var ab = Vector3.Normalize((a + b) / 2.0f);
			var ac = Vector3.Normalize((a + c) / 2.0f);
			var bc = Vector3.Normalize((b + c) / 2.0f);

			ab *= planet.radiusMin;
			ac *= planet.radiusMin;
			bc *= planet.radiusMin;

			AddChild(a, ab, ac, ChildPosition.Top, 0);
			AddChild(ab, b, bc, ChildPosition.Left, 1);
			AddChild(ac, bc, c, ChildPosition.Right, 2);
			AddChild(ab, bc, ac, ChildPosition.Middle, 3);
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

		var c = planet.segmentHeights;
		c.SetBuffer(0, "vertices", b);
		c.SetVector("rangeA", RangeToGenerateInto.a);
		c.SetVector("rangeB", RangeToGenerateInto.b);
		c.SetVector("rangeC", RangeToGenerateInto.c);
		planet.SetComputeBuffer(c);

		c.Dispatch(0, b.count, 1, 1);

		b.GetData(v);


		mesh = new Mesh();
		mesh.vertices = v;
		mesh.triangles = planet.GetSegmentIndicies();
		mesh.RecalculateNormals();

		var go = gameObject;

		var meshFilter = go.AddComponent<MeshFilter>();
		meshFilter.mesh = mesh;

		var meshRenderer = go.AddComponent<MeshRenderer>();
		meshRenderer.material = planet.segmentMaterial;

		var meshCollider = go.AddComponent<MeshCollider>();
		meshCollider.sharedMesh = mesh;

		isGenerationDone = true;
	}


	private void OnDrawGizmos()
	{
		if (gameObject && gameObject.activeSelf)
		{
			Gizmos.color = Color.cyan;
			Gizmos.DrawLine(range.a, range.b);
			Gizmos.DrawLine(range.b, range.c);
			Gizmos.DrawLine(range.c, range.a);
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
		var myPos = RangeToCalculateScreenSizeOn.CenterPos + planet.transform.position;
		var distanceToCamera = Vector3.Distance(myPos, data.pos);

		// this is world space, doesnt take into consideration rotation, not good
		var sphere = RangeToCalculateScreenSizeOn.ToBoundingSphere();
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
