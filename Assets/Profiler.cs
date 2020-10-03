using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using UnityEngine.Profiling;
using System.Collections;

public static class MyProfiler
{



	class TimingSample
	{
		public string name;
		public Stopwatch watch;
		public TimingSample(string name)
		{
			this.name = name;
			this.watch = Stopwatch.StartNew();
		}
	}

	static Stack<TimingSample> timingSamplesStack = new Stack<TimingSample>();


	class SlidingWindowAverageLong
	{
		public double AverageValue => SumValue / (double)Samples.Count;
		long SumValue = 0;
		int MaxSamples = 500;
		Queue<long> Samples = new Queue<long>();
		public void AddSample(long sample)
		{
			while (Samples.Count > MaxSamples)
			{
				SumValue -= Samples.Dequeue();
			}

			Samples.Enqueue(sample);
			SumValue += sample;
		}
	}

	class SlidingWindowAverageDouble
	{
		public double AverageValue => SumValue / (double)Samples.Count;
		double SumValue = 0;
		int MaxSamples = 500;
		Queue<double> Samples = new Queue<double>();
		public void AddSample(double sample)
		{
			while (Samples.Count > MaxSamples)
			{
				SumValue -= Samples.Dequeue();
			}

			Samples.Enqueue(sample);
			SumValue += sample;
		}
	}


	static Dictionary<string, SlidingWindowAverageLong> sampleNameToAverageNumber = new Dictionary<string, SlidingWindowAverageLong>();
	
	static Dictionary<string, SlidingWindowAverageDouble> sampleNameToAverageTiming = new Dictionary<string, SlidingWindowAverageDouble>();


	static Dictionary<string, string> sampleNameToGUIText = new Dictionary<string, string>();


	public static void AddAvergaNumberSample(string name, int number)
	{
		SlidingWindowAverageLong w;
		if (!sampleNameToAverageNumber.TryGetValue(name, out w))
		{
			w = sampleNameToAverageNumber[name] = new SlidingWindowAverageLong();
		}

		w.AddSample(number);
	
		sampleNameToGUIText[name] = w.AverageValue.ToString();

		Behavior.MakeSureExists();
	}

	public static void BeginSample(string name)
	{
		Profiler.BeginSample(name);

		timingSamplesStack.Push(new TimingSample(name));
	}

	public static void EndSample()
	{
		var s = timingSamplesStack.Pop();

		SlidingWindowAverageDouble w;
		if (!sampleNameToAverageTiming.TryGetValue(s.name, out w))
		{
			w = sampleNameToAverageTiming[s.name] = new SlidingWindowAverageDouble();
		}

		w.AddSample(s.watch.Elapsed.TotalMilliseconds);

		sampleNameToGUIText[s.name] = w.AverageValue + "ms";

		Profiler.EndSample();

		Behavior.MakeSureExists();
	}


	public class Behavior : MonoBehaviour
	{
		bool show;
		static Behavior instance;
		public static void MakeSureExists()
		{
			if (instance == null)
			{
				var go = new GameObject(typeof(Behavior) + " holder");
				go.hideFlags = HideFlags.HideAndDontSave;
				instance = go.AddComponent<Behavior>();
			}
		}
		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.F3))
				show = !show;
		}

		private void OnGUI()
		{
			if (!show)
				return;

			foreach (var pair in sampleNameToGUIText.OrderBy((pair) => pair.Key))
			{
				GUILayout.Label(pair.Key + " " + pair.Value);
			}
		}
	}
}