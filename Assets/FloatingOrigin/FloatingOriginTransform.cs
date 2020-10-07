using UnityEngine;

public class FloatingOriginTransform : MonoBehaviour
{

	public Quaternion UnityRotation
	{
		get { return transform.rotation; }
		set { transform.rotation = value; }
	}
	public Vector3 UnityPosition
	{
		get { return transform.position; }
		set { transform.position = value; }
	}
	public BigPosition BigPosition
	{
		get
		{
			return _bigPosition;
		}
		set
		{
			_bigPosition = value;
			UpdateUnityPos();
		}
	}

	public BigPosition _bigPosition;

	private void Start()
	{
		FloatingOriginController.Instance.Add(this);
		UpdateUnityPos();
	}

	private void OnDisable()
	{
		//FloatingOriginController.Instance.Remove(this);
	}
	
	private void UpdateUnityPos()
	{
		SceneOriginChanged(FloatingOriginController.Instance.SceneCenterIsAt);
	}

	public void SceneOriginChanged(BigPosition sceneOrigin)
	{
		UnityPosition = BigPosition - sceneOrigin;
	}
}
