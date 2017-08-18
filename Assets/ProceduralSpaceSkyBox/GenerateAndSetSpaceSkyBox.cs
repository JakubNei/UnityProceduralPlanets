using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class GenerateAndSetSpaceSkyBox : MonoBehaviour
{

	public ComputeShader shader;

	public RenderTexture[] skybox;

	const int resolution = 2048;

	string[] texNames =
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
	


	void Prepare()
	{

		if (skybox == null || skybox.Length != 6)
		{
			skybox = new RenderTexture[6];
			for (int i = 0; i < 6; i++)
			{
				var t = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGBHalf, RenderTextureReadWrite.Linear);
				t.wrapMode = TextureWrapMode.Mirror;
				t.filterMode = FilterMode.Trilinear;
				t.enableRandomWrite = true;
				//t.useMipMap = true;
				//t.autoGenerateMips = false;
				//t.antiAliasing = 8;
				t.Create();
				t.name = texNames[i];
				skybox[i] = t;
			}

		}

		for (int i = 0; i < 6; i++)
			shader.SetTexture(0, texNames[i], skybox[i]);
		//skybox.GenerateMips();

		var material = GetComponent<Skybox>().material;
		for (int i = 0; i < 6; i++)
			material.SetTexture(texNames[i], skybox[i]);
	}

	void Generate()
	{
		shader.SetFloat("_time", Time.realtimeSinceStartup);
		shader.Dispatch(0, resolution / 16, resolution / 16, 6);
	}

}
