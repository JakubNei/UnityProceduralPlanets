using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public partial class Planet : MonoBehaviour
{
	public float WeightNeededToSubdivide = 0.3f;
	public float radiusMin = 100;
	public float radiusVariation = 10;
	public float stopSegmentRecursionAtWorldSize = 5;

	public Material segmentMaterial;
	public ComputeShader segmentHeights;

	public ulong id;

	public List<Segment> rootSegments;


	public bool useSkirts = false;
	public int chunkNumberOfVerticesOnEdge = 10;

	public static HashSet<Planet> allPlanets = new HashSet<Planet>();

	public Vector3 Center { get { return transform.position; } }

	public void SetComputeBuffer(ComputeShader c)
	{
		c.SetInt("numberOfVerticesOnEdge", chunkNumberOfVerticesOnEdge);
		c.SetFloat("radiusMin", radiusMin);
		c.SetFloat("radiusVariation", radiusVariation);
	}



	// Use this for initialization
	void Start()
	{
		allPlanets.Add(this);
		InitializeRootSegments();
	}


	// Update is called once per frame
	void LateUpdate()
	{
		TrySubdivideOver(new SubdivisionData()
		{
			pos = Camera.main.transform.position,
			fieldOfView = Camera.main.fieldOfView,
		});

		var sw = Stopwatch.StartNew();
		foreach (var s in toGenerate.GetWeighted())
		{
			s.GenerateMesh();
			//if (sw.Elapsed.TotalMilliseconds > 5f)
				break;
		}
	}

	private void OnGUI()
	{
		GUILayout.Button("segments to generate: " + toGenerate.Count);
	}




	void AddRootChunk(ulong id, List<Vector3> vertices, int A, int B, int C)
	{
		var range = new Range()
		{
			a = vertices[A],
			b = vertices[B],
			c = vertices[C]
		};

		var child = Segment.Create(
			planet: this,
			parent: null,
			generation: 0,
			range: range,
			id: id
		);

		rootSegments.Add(child);
	}

	private void InitializeRootSegments()
	{
		if (rootSegments != null && rootSegments.Count > 0) return;
		if (rootSegments == null)
			rootSegments = new List<Segment>(20);
		//detailLevel = (int)ceil(planetInfo.rootChunks[0].range.ToBoundingSphere().radius / 100);

		var vertices = new List<Vector3>();
		var indicies = new List<uint>();

		var r = radiusMin / 2.0f;

		var t = (1 + Mathf.Sqrt(5.0f)) / 2.0f * r;
		var d = r;

		vertices.Add(new Vector3(-d, t, 0));
		vertices.Add(new Vector3(d, t, 0));
		vertices.Add(new Vector3(-d, -t, 0));
		vertices.Add(new Vector3(d, -t, 0));

		vertices.Add(new Vector3(0, -d, t));
		vertices.Add(new Vector3(0, d, t));
		vertices.Add(new Vector3(0, -d, -t));
		vertices.Add(new Vector3(0, d, -t));

		vertices.Add(new Vector3(t, 0, -d));
		vertices.Add(new Vector3(t, 0, d));
		vertices.Add(new Vector3(-t, 0, -d));
		vertices.Add(new Vector3(-t, 0, d));

		// 5 faces around point 0
		AddRootChunk(0, vertices, 0, 11, 5);
		AddRootChunk(1, vertices, 0, 5, 1);
		AddRootChunk(2, vertices, 0, 1, 7);
		AddRootChunk(3, vertices, 0, 7, 10);
		AddRootChunk(4, vertices, 0, 10, 11);

		// 5 adjacent faces
		AddRootChunk(5, vertices, 1, 5, 9);
		AddRootChunk(6, vertices, 5, 11, 4);
		AddRootChunk(7, vertices, 11, 10, 2);
		AddRootChunk(8, vertices, 10, 7, 6);
		AddRootChunk(9, vertices, 7, 1, 8);

		// 5 faces around point 3
		AddRootChunk(10, vertices, 3, 9, 4);
		AddRootChunk(11, vertices, 3, 4, 2);
		AddRootChunk(12, vertices, 3, 2, 6);
		AddRootChunk(13, vertices, 3, 6, 8);
		AddRootChunk(14, vertices, 3, 8, 9);

		// 5 adjacent faces
		AddRootChunk(15, vertices, 4, 9, 5);
		AddRootChunk(16, vertices, 2, 4, 11);
		AddRootChunk(17, vertices, 6, 2, 10);
		AddRootChunk(18, vertices, 8, 6, 7);
		AddRootChunk(19, vertices, 9, 8, 1);

	}


	private void OnDrawGizmos()
	{
		if (rootSegments == null || rootSegments.Count == 0)
		{
			Gizmos.color = Color.blue;
			Gizmos.DrawSphere(this.transform.position, this.radiusMin);
		}
	}
}
