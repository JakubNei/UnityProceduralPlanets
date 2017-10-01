using UnityEngine;

public class FloatingOriginTransform : MonoBehaviour
{

	public Quaternion Rotation
	{
		get { return transform.rotation; }
		set { transform.rotation = value; }
	}
	public Vector3 UnityPos
	{
		get { return transform.position; }
		set { transform.position = value; }
	}
	public WorldPos WorldPos
	{
		get;
		set;
	}

	private void Start()
	{
		FloatingOriginController.Instance.Add(this);
	}

	public void SceneOriginChanged(WorldPos sceneOrigin)
	{
		UnityPos = WorldPos - sceneOrigin;
	}
}
