using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class GenerateAndSetSpaceSkyBox : MonoBehaviour
{

	public ComputeShader shader;

	public RenderTexture[] skybox;

	public KeyCode refreshKey = KeyCode.None;

	public int resolution = 2048;

	string[] textureNames =
	{
		"_FrontTex",
		"_BackTex",
		"_LeftTex",
		"_RightTex",
		"_UpTex",
		"_DownTex"
	};

	void Start()
	{
		Prepare();
		Generate();
	}
	

	private void Update()
	{
		if (Input.GetKeyDown(refreshKey))
			Generate();
	}

	void Prepare()
	{
		if (skybox == null || skybox.Length != 6)
		{
			skybox = new RenderTexture[6];
			for (int i = 0; i < 6; i++)
			{
				var t = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
				t.wrapMode = TextureWrapMode.Mirror;
				t.filterMode = FilterMode.Trilinear;
				t.enableRandomWrite = true;
				t.Create();
				t.name = textureNames[i];
				skybox[i] = t;
			}

		}

		var material = GetComponent<Skybox>().material;
		for (int i = 0; i < 6; i++)
			material.SetTexture(textureNames[i], skybox[i]);
	}

	void Generate()
	{
		shader.SetFloat("_time", Time.realtimeSinceStartup);

		for (int i = 0; i < 6; i++)
			shader.SetTexture(0, textureNames[i], skybox[i]);
		shader.Dispatch(0, resolution / 16, resolution / 16, 6);
	}

}
