using UnityEngine;
using System.Collections;

public class CameraRender : MonoBehaviour 
{
	void OnRenderObject()
	{
		if (TestParticle.list != null)
			foreach(TestParticle particle in TestParticle.list)
				particle.Render();
	}
}
