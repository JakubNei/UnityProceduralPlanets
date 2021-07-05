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

	class TimingStat
	{
		TimeSpan Min = TimeSpan.MaxValue;
		TimeSpan Max = TimeSpan.MinValue;

		TimeSpan SumValue;
		int MaxSamples = 500; 
		Queue<TimeSpan> Samples = new Queue<TimeSpan>();

		public void AddSample(TimeSpan sample)
		{
			while (Samples.Count > MaxSamples)
			{
				SumValue -= Samples.Dequeue();
			}

			Samples.Enqueue(sample);
			SumValue += sample;

			if (sample > Max) Max = sample;
			if (sample < Min) Min = sample;
		}


		static string TimeSpanToString(TimeSpan timeSpan)
		{
			return timeSpan.TotalMilliseconds + "ms";

			if (timeSpan.TotalMinutes > 1) return timeSpan.TotalMinutes.ToString("0.##") + "m";
			if (timeSpan.TotalSeconds > 1) return timeSpan.TotalSeconds.ToString("0.##") + "s";
			double ms = timeSpan.TotalMilliseconds;
			if (ms > 1) return ms.ToString("0.##") + "ms";
			ms *= 1000.0;
			if (ms > 1) return ms.ToString("0.##") + "μs";
			ms *= 1000.0;
			return ms.ToString("0.##") + "ns";
		}

		public override string ToString()
		{
			var averageTimeSpan = new TimeSpan(SumValue.Ticks / Samples.Count);
			return TimeSpanToString(averageTimeSpan) + " (min " + TimeSpanToString(Min) + ", max " + TimeSpanToString(Max) + ")";
		}
	}


	static Dictionary<string, SlidingWindowAverageLong> sampleNameToAverageNumber = new Dictionary<string, SlidingWindowAverageLong>();
	
	static Dictionary<string, TimingStat> sampleNameToAverageTiming = new Dictionary<string, TimingStat>();


	static Dictionary<string, string> sampleNameToGUIText = new Dictionary<string, string>();


	public static void AddAverageNumberSample(string name, int number)
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

		TimingStat w;
		if (!sampleNameToAverageTiming.TryGetValue(s.name, out w))
		{
			w = sampleNameToAverageTiming[s.name] = new TimingStat();
		}

		w.AddSample(s.watch.Elapsed);

		sampleNameToGUIText[s.name] = w.ToString();

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