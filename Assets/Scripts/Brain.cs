using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using AForge.Fuzzy;

public class Brain : MonoBehaviour {
	public List<FuzzySetGroupCharts> Charts;
	private InferenceSystem _inferenceSystem;
	private Rigidbody _rb;

	protected void Start() {
		_rb = GetComponent<Rigidbody>();
		InitFuzz();
	}

	protected void Update() {
		float right = GetWallDistance(transform.right);
		float left = GetWallDistance(-transform.right);
		float front = GetWallDistance(transform.forward);

		_inferenceSystem.SetInput("RightDistance", right);
		_inferenceSystem.SetInput("LeftDistance", left);
		_inferenceSystem.SetInput("FrontDistance", front);

		// get the fuzzy angle and rotate towards it
		try {
			float angle = _inferenceSystem.Evaluate("Heading");
			Quaternion delta = Quaternion.Euler(0, angle, 0);
			transform.rotation = Quaternion.RotateTowards(transform.rotation, transform.rotation * delta, 180 * Time.deltaTime);
		} catch (Exception) {
			// logging to troubleshoot holes in the fuzzy ruleset
			Debug.LogError(String.Format("Failed to get heading for L:{0} F:{1} R:{2}", left, front, right));
		}
		
		// force velocity
		_rb.velocity = transform.forward * 2;
	}

	private void InitFuzz() {
		this.Charts = new List<FuzzySetGroupCharts>();
		Database db = new Database();

		// fuzzy set to determine perceived distance
		FuzzySetGroupDesc distanceGroup = new FuzzySetGroupDesc("Distance");
		distanceGroup.Add(new FuzzySetDescLeft("Near", 0.75f, 2.5f));
		distanceGroup.Add(new FuzzySetDescMid("Medium", 0.75f, 2.5f, 3, 10));
		distanceGroup.Add(new FuzzySetDescRight("Far", 3, 10));

		this.Charts.Add(new FuzzySetGroupCharts(distanceGroup));
		FuzzySet[] distanceSets = FuzzUtil.CreateFuzzySets(distanceGroup);

		db.AddVariable(FuzzUtil.CreateVariable("RightDistance", 0, 10, distanceSets));
		db.AddVariable(FuzzUtil.CreateVariable("LeftDistance", 0, 10, distanceSets));
		db.AddVariable(FuzzUtil.CreateVariable("FrontDistance", 0, 10, distanceSets));

		// fuzzy set to determine heading directions
		FuzzySetGroupDesc headingGroup = new FuzzySetGroupDesc("Heading");
		headingGroup.Add(new FuzzySetDescLeft("HardLeft", -35, -30));
		headingGroup.Add(new FuzzySetDescMid("Left", -35, -30, -20, -15));
		headingGroup.Add(new FuzzySetDescMid("SlightLeft", -20, -15, -5, 0));
		headingGroup.Add(new FuzzySetDescMid("Straight", -5, -1, 1, 5));
		headingGroup.Add(new FuzzySetDescMid("SlightRight", 0, 5, 15, 20));
		headingGroup.Add(new FuzzySetDescMid("Right", 15, 20, 30, 35));
		headingGroup.Add(new FuzzySetDescRight("HardRight", 30, 35));

		this.Charts.Add(new FuzzySetGroupCharts(headingGroup));
		FuzzySet[] headingSets = FuzzUtil.CreateFuzzySets(headingGroup);

		db.AddVariable(FuzzUtil.CreateVariable("Heading", -50, 50, headingSets));

		_inferenceSystem = new InferenceSystem(db, new CentroidDefuzzifier(50));
		
		// if all is clear ahead go for it
		_inferenceSystem.NewRule("1", "IF FrontDistance IS Far THEN Heading IS Straight");

		// should handle left/right turns
		_inferenceSystem.NewRule("2", "IF FrontDistance IS Near AND RightDistance IS Near THEN Heading IS Left");
		_inferenceSystem.NewRule("3", "IF FrontDistance IS Near AND LeftDistance IS Near THEN Heading IS Right");

		// edge away from walls
		_inferenceSystem.NewRule("4", "IF LeftDistance IS Near AND (RightDistance IS Medium OR RightDistance IS Far) THEN Heading IS SlightRight");
		_inferenceSystem.NewRule("5", "IF RightDistance IS Near AND (LeftDistance IS Medium OR LeftDistance IS Far) THEN Heading IS SlightLeft");

		// attempt to give priority to left turns if both are available?
		_inferenceSystem.NewRule("6", "IF FrontDistance IS Near THEN Heading IS Left");
	}

	private float GetWallDistance(Vector3 direction) {
		RaycastHit[] hits = Physics.RaycastAll(transform.position, direction, float.MaxValue, ~LayerMask.NameToLayer("Obstacle"), QueryTriggerInteraction.UseGlobal);
		if (hits.Length == 0) return float.MaxValue;

		// linq bad, but so good
		RaycastHit closest = hits.OrderBy(x => x.distance).FirstOrDefault();
		return closest.distance;
	}

	// public void OnDrawGizmos() {
	// 	float distance = GetWallDistance(transform.forward);
	// 	Gizmos.DrawLine(transform.position, transform.position + transform.forward * distance);
	// }
}