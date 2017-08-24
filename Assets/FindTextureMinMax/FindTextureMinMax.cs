

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
			return Resources.Load<ComputeShader>("findTextureMinMax");
		}
	}

	static RenderTexture GetRenderTexture(int w, int h)
	{
		var t = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
		t.enableRandomWrite = true;
		t.Create();
		return t;
	}

	static void ReleaseRenderTexture(RenderTexture rt)
	{
		RenderTexture.ReleaseTemporary(rt);
	}


	const int NUM_THREADS = 1;

	public static Result Find(Texture source)
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

		kernel = s.FindKernel("firstStep");

		minOut = GetRenderTexture(w, h);
		maxOut = GetRenderTexture(w, h);

		s.SetTexture(kernel, "_initialTextureIn", source);
		s.SetTexture(kernel, "_textureMinOut", minOut);
		s.SetTexture(kernel, "_textureMaxOut", maxOut);

		s.Dispatch(kernel, w / NUM_THREADS, h / NUM_THREADS, 1);



		while (true)
		{
			w = w / 2;
			h = h / 2;

			minIn = minOut;
			maxIn = maxOut;

			kernel = s.FindKernel("otherSteps");

			minOut = GetRenderTexture(w, h);
			maxOut = GetRenderTexture(w, h);

			s.SetTexture(kernel, "_textureMinIn", minIn);
			s.SetTexture(kernel, "_textureMaxIn", maxIn);
			s.SetTexture(kernel, "_textureMinOut", minOut);
			s.SetTexture(kernel, "_textureMaxOut", maxOut);

			s.Dispatch(kernel, w / NUM_THREADS, h / NUM_THREADS, 1);

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