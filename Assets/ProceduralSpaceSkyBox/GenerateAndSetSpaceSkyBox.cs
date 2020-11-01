using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class GenerateAndSetSpaceSkyBox : MonoBehaviour
{
	public bool markedForRegeneration;

	public ComputeShader shader;

	public RenderTexture[] skyboxTextures;

	public KeyCode refreshKey = KeyCode.None;

	public int resolution = 2048;

	public Skybox[] targetComponents;

	public Transform closestSun;
	public Vector3 closestSunDirection;

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

	int delyedResolutionChangeRegenerate = 0;
	private void Update()
	{
		if (resolution != GetIdealResolution())
		{ 
			delyedResolutionChangeRegenerate++;
		}

		if (delyedResolutionChangeRegenerate > 120)
		{
			delyedResolutionChangeRegenerate = 0;
			Prepare();
			Generate();
		}

		if (markedForRegeneration)
		{
			markedForRegeneration = false;
			Generate();
		}
	}

	int GetIdealResolution()
	{
		int r;
		if (Screen.width > Screen.height) r = Screen.width;
		else r = Screen.height;

		var power = Mathf.Ceil(Mathf.Log(r) / Mathf.Log(2));
		r = (int)Mathf.Pow(2.0f, power);

		return r;
	}

	void Prepare()
	{
		if (skyboxTextures != null)
		{
			foreach (var t in skyboxTextures)
			{
				t.Release();
				RenderTexture.Destroy(t);
			}
			skyboxTextures = null;
		}

		resolution = GetIdealResolution();
		skyboxTextures = new RenderTexture[6];
		for (int i = 0; i < 6; i++)
		{
			var t = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
			t.wrapMode = TextureWrapMode.Mirror;
			t.filterMode = FilterMode.Trilinear;
			t.enableRandomWrite = true;
			t.autoGenerateMips = false;
			t.useMipMap = true;
			t.Create();
			t.name = textureNames[i];
			skyboxTextures[i] = t;
		}

		if (targetComponents == null || targetComponents.Length == 0)
			targetComponents = GetComponentsInChildren<Skybox>();

		foreach (var target in targetComponents)
		{
			var material = target.material;
			for (int i = 0; i < 6; i++)
				material.SetTexture(textureNames[i], skyboxTextures[i]);
		}

	}

	void Generate()
	{
		if (closestSun != null)
			closestSunDirection = -closestSun.forward;

		shader.SetFloat("_time", Time.realtimeSinceStartup);
		shader.SetVector("_sunDir", closestSunDirection);

		for (int i = 0; i < 6; i++)
			shader.SetTexture(0, textureNames[i], skyboxTextures[i]);
		shader.Dispatch(0, resolution / 16, resolution / 16, 6);
		for (int i = 0; i < 6; i++)
			if (skyboxTextures[i].useMipMap && !skyboxTextures[i].autoGenerateMips)
				skyboxTextures[i].GenerateMips();
	}

}
