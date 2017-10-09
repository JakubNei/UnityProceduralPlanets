using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using UnityEngine.Profiling;

public static class MyProfiler
{



	class Sample
	{
		public string name;
		public Stopwatch watch;
		public Sample(string name)
		{
			this.name = name;
			this.watch = Stopwatch.StartNew();
		}
	}

	static Stack<Sample> samplesStack = new Stack<Sample>();


	class NameData
	{
		public string name;
		public ulong timesExecuted;
		public TimeSpan timeTaken;
	}
	static Dictionary<string, NameData> nameToNameData = new Dictionary<string, NameData>();


	public static void BeginSample(string name)
	{
		//Profiler.BeginSample(name);
		samplesStack.Push(new Sample(name));
	}

	public static void EndSample()
	{
		var s = samplesStack.Pop();

		NameData d;
		if (!nameToNameData.TryGetValue(s.name, out d))
		{
			d = new NameData();
			d.name = s.name;
			nameToNameData[d.name] = d;
		}

		d.timesExecuted++;
		d.timeTaken += s.watch.Elapsed;

		//Profiler.EndSample();

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
			foreach (var d in nameToNameData.Values)
			{
				if (d.timesExecuted > 0)
					GUILayout.Label(d.name + " " + d.timeTaken.TotalMilliseconds / d.timesExecuted + "ms");
			}
		}
	}
}