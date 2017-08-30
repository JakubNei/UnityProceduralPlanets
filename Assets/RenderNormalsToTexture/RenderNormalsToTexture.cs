using UnityEngine;

public class RenderNormalsToTexture
{
	public const int layer = 20;
	public const int cullingMask = 1 << layer;



	static Camera renderToTextureCamera;
	static Camera GetRenderToTextureCamera()
	{
		if (renderToTextureCamera == null)
		{
			var go = new GameObject("render mesh normals to texture: camera");
			//go.hideFlags = HideFlags.HideAndDontSave;

			renderToTextureCamera = go.AddComponent<Camera>();
			renderToTextureCamera.enabled = false;
			renderToTextureCamera.renderingPath = RenderingPath.Forward;
			renderToTextureCamera.cullingMask = cullingMask;
			renderToTextureCamera.useOcclusionCulling = false;
			renderToTextureCamera.clearFlags = CameraClearFlags.Color;
			renderToTextureCamera.depthTextureMode = DepthTextureMode.None;
			renderToTextureCamera.backgroundColor = new Color(0.5f, 0.5f, 1);

			go.transform.position = new Vector3(0, 0, -3001);
			renderToTextureCamera.farClipPlane = 10000f;
		}
		return renderToTextureCamera;
	}

	static Shader renderNormalsToTextureShader;
	static Shader GetRenderNormalsToTextureShader()
	{
		if (renderNormalsToTextureShader == null)
		{
			renderNormalsToTextureShader = Resources.Load<Shader>("RenderNormalsToTexture");
		}
		return renderNormalsToTextureShader;
	}

	public static void Render(GameObject toRender, RenderTexture target)
	{
		var cam = GetRenderToTextureCamera();

		var originalLayer = toRender.layer;
		toRender.layer = layer;

		cam.pixelRect = new Rect(0, 0, target.width, target.height);
		cam.targetTexture = target;
		cam.transform.LookAt(toRender.transform);
		cam.RenderWithShader(GetRenderNormalsToTextureShader(), string.Empty);

		toRender.layer = originalLayer;
	}

	
	static MeshFilter meshFilter;
	public static void Render(Mesh mesh, RenderTexture target)
	{
		if(meshFilter == null)
		{
			var go = new GameObject("render mesh normals to texture: mesh holder");
			//go.hideFlags = HideFlags.HideAndDontSave;
			
			meshFilter = go.AddComponent<MeshFilter>();
			go.AddComponent<MeshRenderer>();
		}

		meshFilter.sharedMesh = mesh;
		
		Render(meshFilter.gameObject, target);

		meshFilter.sharedMesh = null;;
	}


	static void ASDF(Mesh toRender)
	{
		const int resolution = 256;

		var texture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.filterMode = FilterMode.Bilinear;
		texture.enableRandomWrite = true;
		texture.Create();
	}
}