

using UnityEngine;

public class RenderNormalsToTexture
{


	static Shader renderNormalsToTexture;
	static Camera renderToTextureCamera;
	public static void Render(GameObject toRender, RenderTexture target)
	{
		int layer = 20;
		int cullingMask = 1 << layer;
		if (renderToTextureCamera == null)
		{
			var cameraHolder = new GameObject("render to texture camera holder");
			renderToTextureCamera = cameraHolder.AddComponent<Camera>();
			renderToTextureCamera.enabled = false;
			renderToTextureCamera.renderingPath = RenderingPath.Forward;
			renderToTextureCamera.cullingMask = cullingMask;
			renderToTextureCamera.useOcclusionCulling = false;
			renderToTextureCamera.clearFlags = CameraClearFlags.Color;
			renderToTextureCamera.depthTextureMode = DepthTextureMode.None;
			renderToTextureCamera.backgroundColor = new Color(0.5f, 0.5f, 1);

			cameraHolder.transform.position = new Vector3(0, 0, -3001);
			renderToTextureCamera.farClipPlane = 10000f;
		}
		var originalLayer = toRender.layer;
		toRender.layer = layer;

		renderToTextureCamera.pixelRect = new Rect(0, 0, target.width, target.height);
		renderToTextureCamera.targetTexture = target;
		renderToTextureCamera.transform.LookAt(toRender.transform);
		renderToTextureCamera.RenderWithShader(renderNormalsToTexture, string.Empty);

		toRender.layer = originalLayer;
	}


	void CreateNormalMapFromMesh()
	{
		const int resolution = 256;

		var texture = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.filterMode = FilterMode.Trilinear;
		texture.enableRandomWrite = true;
		texture.Create();
	}
}