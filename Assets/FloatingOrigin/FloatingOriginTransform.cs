using System.Xml.Schema;
using UnityEngine;

public class FloatingOriginTransform : MonoBehaviour
{
	public Quaternion Rotation
	{
		get 
		{ 
			if (rb) return rb.rotation;
			return transform.rotation; 
		}
		set 
		{
			transform.rotation = value;	
			if (rb) rb.rotation = value;
		}
	}

	public Vector3 VisualPosition
	{
		get 
		{
			if (rb) return rb.position;
			return transform.position; 
		}
		set
		{
			bigPosition = VisualSceneOrigin + value;
			transform.position = value;
			if (rb) rb.position = value;
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
			lastRbPosition = value - VisualSceneOrigin;
			transform.position = lastRbPosition;
		}
	}


	[SerializeField]
	private BigPosition bigPosition;


	private BigPosition VisualSceneOrigin => FloatingOriginCamera.main.VisualSceneOrigin;

	Rigidbody rb;
	private void Start()
	{
		var camera = FloatingOriginCamera.main;
		camera.Add(this);
		transform.position = bigPosition - VisualSceneOrigin;

		rb = GetComponent<Rigidbody>();
	}

	private void OnDrawGizmosSelected()
	{	
		//bigPosition.MoveSectorIfNeeded();
		//if (bigPosition != transform.position + VisualSceneOrigin)
		//{
		//	transform.position = bigPosition - VisualSceneOrigin;
		//}
	}

	private Vector3 lastRbPosition;
	private void FixedUpdate()
	{
		if (rb && rb.position!= lastRbPosition) 
		{
			// Some system didnt set position thru FloatingOriginTransform, probably Unity physics, lets compensate for it
			bigPosition = VisualSceneOrigin + rb.position;
			lastRbPosition = rb.position;
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
