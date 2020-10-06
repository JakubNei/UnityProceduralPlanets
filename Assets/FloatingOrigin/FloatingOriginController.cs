using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingOriginController : MonoBehaviour
{
	public BigPosition BigPosition => SceneCenterIsAt + this.transform.position;

	public BigPosition SceneCenterIsAt { get; private set; }

	public static FloatingOriginController Instance { get; private set; }

	private List<FloatingOriginTransform> fs = new List<FloatingOriginTransform>();

	private void Awake()
	{
		Instance = this;
	}

	public void Add(FloatingOriginTransform f)
	{
		fs.Add(f);
	}

	public void Remove(FloatingOriginTransform f)
	{
		fs.Remove(f);
	}

	private void FixedUpdate()
	{
		if (this.transform.position.sqrMagnitude > 1000 * 1000)
		{
			var worldPosDelta = new BigPosition(transform.position).KeepOnlySectorPos();
			SceneCenterIsAt += worldPosDelta;
			this.transform.position -= worldPosDelta;

			foreach (var i in fs)
				i.SceneOriginChanged(SceneCenterIsAt);
		}
	}

}

