using System.Xml.Schema;
using UnityEngine;

public class FloatingOriginTransform : MonoBehaviour
{
	public Quaternion Rotation
	{
		get { return transform.rotation; }
		set { transform.rotation = value; }
	}

	public Vector3 VisualPosition
	{
		get 
		{
			return lastSetVisualPosition; 
		}
		set
		{
			bigPosition = VisualSceneOrigin + value;
			lastSetVisualPosition = value;
			transform.position = value;
		}
	}

	public BigPosition BigPosition
	{
		get
		{
			return bigPosition;
		}
		set
		{
			bigPosition = value;
			lastSetVisualPosition = value - VisualSceneOrigin;
			transform.position = lastSetVisualPosition;
		}
	}


	[SerializeField]
	private BigPosition bigPosition;

	private Vector3 lastSetVisualPosition;

	private BigPosition VisualSceneOrigin => FloatingOriginCamera.Main.VisualSceneOrigin;


	private void Start()
	{
		var camera = FloatingOriginCamera.Main;
		camera.Add(this);
		transform.position = bigPosition - VisualSceneOrigin;
	}

	private void OnDrawGizmosSelected()
	{	
		bigPosition.MoveSectorIfNeeded();
		if (bigPosition != transform.position + VisualSceneOrigin)
		{
			transform.position = bigPosition - VisualSceneOrigin;
		}
	}

	private void FixedUpdate()
	{
		if (transform.position != lastSetVisualPosition) 
		{
			// Some system didnt set position thru FloatingOriginTransform, probably Unity physics, lets compensate for it
			VisualPosition = transform.position;
		}
	}

	private void OnDisable()
	{
		//FloatingOriginController.Instance.Remove(this);
	}


	public void SceneOriginChanged(BigPosition newSceneOrigin)
	{
		 transform.position = bigPosition - newSceneOrigin;
	}
}
