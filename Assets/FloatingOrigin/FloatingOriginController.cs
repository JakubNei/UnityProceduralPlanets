using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloatingOriginController : MonoBehaviour
{
	public WorldPos sceneCenterIsAt;

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

	private void Update()
	{
		if (this.transform.position.sqrMagnitude > 1000 * 1000)
		{
			var worldPosDelta = new WorldPos(transform.position).KeepOnlySectorPos();
			sceneCenterIsAt += worldPosDelta;
			this.transform.position -= worldPosDelta;

			foreach (var i in fs)
				i.SceneOriginChanged(sceneCenterIsAt);
		}
	}

}

