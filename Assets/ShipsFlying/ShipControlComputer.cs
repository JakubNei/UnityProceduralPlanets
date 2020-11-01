using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

[RequireComponent(typeof(FloatingOriginTransform), typeof(Rigidbody))]
public class ShipControlComputer : MonoBehaviour
{

	public Vector3 shipRoot_centerOfMass;

	ThrusterObject[] conectedThrusters;


	public float ShipMass => rigidbody.mass;
	public Vector3 ActualThrustersForce { get; private set; }
	public Vector3 ActualThrustersAngularForce { get; private set; }
	public Vector3 ExternalForce { get; private set; }

	public Vector3 CurrentVelocity => rigidbody.transform.InverseTransformVector(rigidbody.velocity);
	public Vector3 CurrentAngularVelocity => rigidbody.transform.InverseTransformVector(rigidbody.angularVelocity);
	public Vector3 CurrentForce { get; private set; }


	public bool calculateVisuallyGoodForce = true;
	public bool calculateVisuallyGoodTorque = true;

	public bool calculateNNLSForce = true;
	public bool calculateNNLSTorque = true;

	Rigidbody rigidbody;
	FloatingOriginTransform floatingOrigin;

	Transform ShipRoot => transform;

	// Use this for initialization
	void Start()
	{
		rigidbody = GetComponent<Rigidbody>();
		floatingOrigin = GetComponent<FloatingOriginTransform>();
		previousVelocity = CurrentVelocity;

		conectedThrusters = GetComponentsInChildren<ThrusterObject>();
		shipRoot_centerOfMass = ShipRoot.InverseTransformPoint(rigidbody.worldCenterOfMass);;

		foreach (var thruster in conectedThrusters)
			thruster.Initialize(shipRoot_centerOfMass);
	}

	Vector3 previousVelocity;
	void FixedUpdate()
	{
		CurrentForce = (previousVelocity - CurrentVelocity) / Time.fixedDeltaTime;
		previousVelocity = CurrentVelocity;

		ExternalForce = Vector3.zero;
		ApplyGravity();
	}

	void ApplyGravity()
	{
		var gravity = EnvironmentSensors.main.GetGravityAt(floatingOrigin.BigPosition);
		if (gravity == Vector3.zero) return;
		ExternalForce += gravity;
		var rb = GetComponent<Rigidbody>();
		rb.AddForce(gravity * Time.fixedDeltaTime, ForceMode.VelocityChange);
	}


	public void SetTargetForces(Vector3 targetForce, Vector3 targetTorque)
	{
		// https://www.youtube.com/watch?v=Lg3P4uIlgeU
		ComputeAndSet(targetForce, targetTorque);
	}


	static float GetError(ThrusterObject[] thrusters, float[] powerPerThruster, Vector3 targetForce, Vector3 targetTorque)
	{
		var totalForce = Vector3.zero;
		var totalTorque = Vector3.zero;

		for (int i = 0; i < thrusters.Length; i++)
		{
			var thruster = thrusters[i];
			var f = powerPerThruster[i];
			totalForce += thruster.shipRoot_direction * f;
			totalTorque += thruster.shipRoot_torqueWithPowerOne * f;
		}

		var error = (totalForce - targetForce).magnitude + (totalTorque - targetTorque).magnitude;

		return error;
	}

	
	List<float> fitnessPerThruster = new List<float>();
	List<int> acceptedThruster = new List<int>();

	void ComputeAndSet(Vector3 tm, Vector3 td)
	{
		ActualThrustersForce = Vector3.zero;
		ActualThrustersAngularForce = Vector3.zero;

		var targetForce = -tm;
		var targetTorque = td;

		if (targetForce.sqrMagnitude <= 0 && targetTorque.sqrMagnitude <= 0)
		{
			for (int i = 0; i < conectedThrusters.Length; i++)
				conectedThrusters[i].SetPower(0);
			return;
		}

		var finalPowerPerThruster = new float[conectedThrusters.Length];

		int lastIterationSolutionsFound = 0;	
		int iteration = 0;
		do
		{ 
			lastIterationSolutionsFound = 0;

			// force
			{ 
				fitnessPerThruster.Clear();
				fitnessPerThruster.Capacity = Math.Max(conectedThrusters.Length, fitnessPerThruster.Capacity);
				float maxFitness = 0;

				for (int i = 0; i < conectedThrusters.Length; ++i)
				{
					var thruster = conectedThrusters[i];
					float fitness = 0;
					if (thruster.MaxPower > 0)
					{ 
						fitness = Math.Max(0, Vector3.Dot(targetForce.normalized, thruster.shipRoot_direction));
					}
					fitnessPerThruster.Add(fitness);
					maxFitness = Math.Max(fitness, maxFitness);
				}

				acceptedThruster.Clear();
				acceptedThruster.Capacity = Math.Max(conectedThrusters.Length, acceptedThruster.Capacity);
				float sumMaxForce = 0;
				for (int i = 0; i < conectedThrusters.Length; ++i)
				{
					var thruster = conectedThrusters[i];
					var fitness = fitnessPerThruster[i];
					if (fitness > maxFitness * 0.9f) 
					{ 
						acceptedThruster.Add(i);
						sumMaxForce += thruster.MaxPower;
					}
				}

				float powerRatio = Math.Min(targetForce.magnitude / sumMaxForce, 1);

				// distribute equally between all accepted
				for (int j = 0; j < acceptedThruster.Count; ++j)
				{
					int i = acceptedThruster[j];
					var thruster = conectedThrusters[i];
					float thrusterPower = powerRatio * thruster.MaxPower;
					finalPowerPerThruster[i] += thrusterPower;
					targetForce -= thruster.shipRoot_direction * thrusterPower;
					targetTorque -= thruster.shipRoot_torqueWithPowerOne * thrusterPower;
				}

				lastIterationSolutionsFound += acceptedThruster.Count;
			}

			// torque
			{ 
				fitnessPerThruster.Clear();
				fitnessPerThruster.Capacity = Math.Max(conectedThrusters.Length, fitnessPerThruster.Capacity);
				float maxFitness = 0;

				for (int i = 0; i < conectedThrusters.Length; ++i)
				{
					var thruster = conectedThrusters[i];
					float fitness = 0;
					if (thruster.MaxPower > 0)
					{ 
						fitness = Math.Max(0, Vector3.Dot(targetTorque.normalized, thruster.shipRoot_torqueWithPowerOne.normalized));
					}
					fitnessPerThruster.Add(fitness);
					maxFitness = Math.Max(fitness, maxFitness);
				}

				acceptedThruster.Clear();
				acceptedThruster.Capacity = Math.Max(conectedThrusters.Length, acceptedThruster.Capacity);
				float sumMaxTorque = 0;
				for (int i = 0; i < conectedThrusters.Length; ++i)
				{
					var thruster = conectedThrusters[i];
					var fitness = fitnessPerThruster[i];
					if (fitness > maxFitness * 0.9f) 
					{ 
						acceptedThruster.Add(i);
						Vector3 torqueWithPowerOne = thruster.shipRoot_torqueWithPowerOne;
						sumMaxTorque += torqueWithPowerOne.magnitude * thruster.MaxPower;
					}
				}

				float powerRatio = Math.Min(targetTorque.magnitude / sumMaxTorque, 1);

				// distribute equally between all accepted
				for (int j = 0; j < acceptedThruster.Count; ++j)
				{
					int i = acceptedThruster[j];
					var thruster = conectedThrusters[i];
					float thrusterPower = powerRatio * thruster.MaxPower;
					finalPowerPerThruster[i] += thrusterPower;
					targetForce -= thruster.shipRoot_direction * thrusterPower;
					targetTorque -= thruster.shipRoot_torqueWithPowerOne * thrusterPower;
				}

				lastIterationSolutionsFound += acceptedThruster.Count;
			}

		} while (++iteration < 100 && lastIterationSolutionsFound > 0);

		//Debug.Log("lastSolutionsFound "  + lastIterationSolutionsFound + ", iteration " + iteration);

		// if one thruster target power is above its limit, decrease all thruster power by same ratio
		float multipleAllBy = 1;
		for (int i = 0; i < conectedThrusters.Length; i++)
		{
			var t = conectedThrusters[i];
			var wantedPower = finalPowerPerThruster[i];

			if (wantedPower > t.MaxPower)
			{
				var newMultAllBy = t.MaxPower / wantedPower;
				multipleAllBy = Math.Min(multipleAllBy, newMultAllBy);
			}
		}

		//Debug.Log("error: " + GetError(conectedThrusters, globalThrustersPower, originalTargetForce, originalTargetTorque) + ", power multiplier:" + multipleAllBy);
		for (int i = 0; i < conectedThrusters.Length; i++)
		{
			var thruster = conectedThrusters[i];
			var power = finalPowerPerThruster[i] * multipleAllBy;
			thruster.SetPower(power);

			ActualThrustersForce += thruster.shipRoot_direction * power;
			ActualThrustersAngularForce += thruster.shipRoot_torqueWithPowerOne * power;
		}


	}

}
