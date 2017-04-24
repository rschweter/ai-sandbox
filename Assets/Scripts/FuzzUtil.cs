using System;
using System.Collections.Generic;
using UnityEngine;

using AForge;
using AForge.Fuzzy;

public class FuzzUtil  {
	public static LinguisticVariable CreateVariable(string name, float start, float end, params FuzzySet[] labels) {
		LinguisticVariable lv = new LinguisticVariable(name, start, end);
		
		for (int i = 0; i < labels.Length; i++) {
			lv.AddLabel(labels[i]);
		}

		return lv;
	}

	public static FuzzySet[] CreateFuzzySets(FuzzySetGroupDesc group) {
		FuzzySet[] sets = new FuzzySet[group.Sets.Count];

		for (int i = 0; i < group.Sets.Count; i++) {
			FuzzySetDesc desc = group.Sets[i];
			sets[i] = new FuzzySet(desc.Name, new PiecewiseLinearFunction(desc.Points));
		}

		return sets;
	}
}

[Serializable]
public class FuzzySetGroupDesc {
	public string Name;
	public List<FuzzySetDesc> Sets;
	public float ActualMin;
	public float ActualMax;

	public FuzzySetGroupDesc(string name) {
		this.Name = name;
		this.Sets = new List<FuzzySetDesc>();
		this.ActualMin = float.MaxValue;
		this.ActualMax = float.MinValue;
	}

	// currently requires the points to be pre-sorted
	public FuzzySetDesc Add(FuzzySetDesc desc) {
		this.Sets.Add(desc);

		float min = desc.Points[0].X;
		float max = desc.Points[desc.Points.Length - 1].X;

		if (min < this.ActualMin) {
			this.ActualMin = min;
		}

		if (max > this.ActualMax) {
			this.ActualMax = max;
		}

		return desc;
	}

	public float GetChartMin() {
		return this.ActualMin - (this.ActualMax - this.ActualMin) * 0.1f;
	}

	public float GetChartMax() {
		return this.ActualMax + (this.ActualMax - this.ActualMin) * 0.1f;
	}
}

[Serializable]
public abstract class FuzzySetDesc {
	public string Name;
	public Point[] Points;

	public FuzzySetDesc(string name) {
		this.Name = name;
	}
}

public class FuzzySetDescLeft : FuzzySetDesc {
	public FuzzySetDescLeft(string name, float start, float end) 
		: base(name) {

		this.Points = new Point[] { 
			new Point(start, 1), 
			new Point(end, 0) 
		};
	}
}

public class FuzzySetDescMid : FuzzySetDesc {
	public FuzzySetDescMid(string name, float start, float peakLeft, float peakRight, float end) 
		: base(name) {

		this.Points = new Point[] { 
			new Point(start, 0),
			new Point(peakLeft, 1),
			new Point(peakRight, 1),
			new Point(end, 0)
		};
	}
}

public class FuzzySetDescRight : FuzzySetDesc {
	public FuzzySetDescRight(string name, float start, float end) 
		: base(name) {

		this.Points = new Point[] { 
			new Point(start, 0), 
			new Point(end, 1) 
		};
	}
}

[Serializable]
public class FuzzySetGroupCharts {
	public string Name;
	public List<FuzzySetChart> Charts;

	public FuzzySetGroupCharts(FuzzySetGroupDesc group) {
		this.Name = group.Name;

		float chartMin = group.GetChartMin();
		float chartMax = group.GetChartMax();

		this.Charts = new List<FuzzySetChart>();
		for (int i = 0; i < group.Sets.Count; i++) {
			this.Charts.Add(new FuzzySetChart(group.Sets[i], chartMin, chartMax));
		}
	}
}

[Serializable]
public class FuzzySetChart {
	public string Name;
	public AnimationCurve Curve;

	public FuzzySetChart(FuzzySetDesc set, float chartMin, float chartMax) {
		this.Name = set.Name;

		foreach (Point p in set.Points) {
			Debug.Log(set.Name + " " + p.X + " " + p.Y);
		}

		List<Keyframe> frames = new List<Keyframe>(set.Points.Length + 2);
		Keyframe? last = null;

		if (!Mathf.Approximately(set.Points[0].X, chartMin)) {
			last = new Keyframe(chartMin, set.Points[0].Y, 0, 0);
			frames.Add(last.Value);
		}

		for (int i = 0; i < set.Points.Length; i++) {
			Point p = set.Points[i];

			float outTangent = 0;
			if (last.HasValue) {
				outTangent = (p.Y - last.Value.value) / (p.X - last.Value.time);
				Keyframe kf = last.Value;
				kf.outTangent = outTangent;
				last = kf;
				frames[frames.Count - 1] = kf;
			}

			Keyframe next = new Keyframe(p.X, p.Y, outTangent, 0);
			frames.Add(next);
			last = next;
		}

		if (last.HasValue && !Mathf.Approximately(last.Value.time, 1f)) {
			Keyframe next = new Keyframe(chartMax, last.Value.value, 0, 0);
			frames.Add(next);
		}

		this.Curve = new AnimationCurve(frames.ToArray());
	}
}