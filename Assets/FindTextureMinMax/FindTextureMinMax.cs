

using UnityEngine;

public class FindTextureMinMax
{

	[System.Serializable]
	public struct Result
	{
		public Vector4 min;
		public Vector4 max;
	}


	static ComputeShader Shader
	{
		get
		{
			return Resources.Load<ComputeShader>("FindTextureMinMax");
		}
	}

	static RenderTexture GetRenderTexture(int w, int h, RenderTextureFormat format)
	{
		var t = RenderTexture.GetTemporary(w, h, 0, format, RenderTextureReadWrite.Linear);
		t.enableRandomWrite = true;
		t.Create();
		return t;
	}

	static void ReleaseRenderTexture(RenderTexture rt)
	{
		RenderTexture.ReleaseTemporary(rt);
	}


	const int NUM_THREADS = 16;

	public static Result Find(Texture source, RenderTextureFormat format = RenderTextureFormat.ARGB32)
	{
		var s = Shader;

		var w = source.width;
		var h = source.height;

		if (Mathf.IsPowerOfTwo(w)) w = w / 2;
		else w = Mathf.NextPowerOfTwo(w) / 2;

		if (Mathf.IsPowerOfTwo(h)) h = h / 2;
		else h = Mathf.NextPowerOfTwo(h) / 2;


		int kernel = 0;
		RenderTexture minIn, maxIn, minOut, maxOut;
		int tgy, tgx;

		kernel = s.FindKernel("firstStep");

		minOut = GetRenderTexture(w, h, format);
		maxOut = GetRenderTexture(w, h, format);

		s.SetTexture(kernel, "_initialTextureIn", source);
		s.SetTexture(kernel, "_textureMinOut", minOut);
		s.SetTexture(kernel, "_textureMaxOut", maxOut);

		tgx = Mathf.Max(1, Mathf.CeilToInt(w / NUM_THREADS));
		tgy = Mathf.Max(1, Mathf.CeilToInt(h / NUM_THREADS));
		s.Dispatch(kernel, tgx, tgy, 1);



		kernel = s.FindKernel("otherSteps");

		while (true)
		{
			w = w / 2;
			h = h / 2;

			minIn = minOut;
			maxIn = maxOut;

			minOut = GetRenderTexture(w, h, format);
			maxOut = GetRenderTexture(w, h, format);

			s.SetTexture(kernel, "_textureMinIn", minIn);
			s.SetTexture(kernel, "_textureMaxIn", maxIn);
			s.SetTexture(kernel, "_textureMinOut", minOut);
			s.SetTexture(kernel, "_textureMaxOut", maxOut);

			w = Mathf.Max(1, Mathf.CeilToInt(w / NUM_THREADS));
			tgy = Mathf.Max(1, Mathf.CeilToInt(h / NUM_THREADS));
			s.Dispatch(kernel, tgx, tgy, 1);

			ReleaseRenderTexture(minIn);
			ReleaseRenderTexture(maxIn);

			if (w == 2 || h == 2) break;
		}


		minIn = minOut;
		maxIn = maxOut;

		kernel = s.FindKernel("finalStep");

		var finalDataOut = new ComputeBuffer(1, sizeof(float) * 4 * 2);

		s.SetTexture(kernel, "_textureMinIn", minIn);
		s.SetTexture(kernel, "_textureMaxIn", maxIn);
		s.SetBuffer(kernel, "_finalDataOut", finalDataOut);
		s.Dispatch(kernel, 1, 1, 1);

		ReleaseRenderTexture(minIn);
		ReleaseRenderTexture(maxIn);

		var result = new Result[1];
		finalDataOut.GetData(result);
		finalDataOut.Release();
		return result[0];
	}



}