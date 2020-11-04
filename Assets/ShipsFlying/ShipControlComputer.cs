using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using TMPro;
using System.Runtime.InteropServices.ComTypes;

[RequireComponent(typeof(FloatingOriginTransform), typeof(Rigidbody))]
public class ShipControlComputer : MonoBehaviour
{

	public Vector3 shipRoot_centerOfMass;

	ThrusterObject[] connectedThrusters;


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

		connectedThrusters = GetComponentsInChildren<ThrusterObject>();
		shipRoot_centerOfMass = ShipRoot.InverseTransformPoint(rigidbody.worldCenterOfMass);;

		foreach (var thruster in connectedThrusters)
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


	float GetError(float[] powerPerThruster, Vector3 targetForce, Vector3 targetTorque)
	{
		var totalForce = Vector3.zero;
		var totalTorque = Vector3.zero;

		for (int i = 0; i < connectedThrusters.Length; i++)
		{
			var thruster = connectedThrusters[i];
			var f = powerPerThruster[i];
			totalForce += thruster.shipRoot_direction * f;
			totalTorque += thruster.shipRoot_torqueWithPowerOne * f;
		}

		var error = (targetForce - totalForce).magnitude + (targetTorque - totalTorque).magnitude;

		return error;
	}

	
	List<float> fitnessPerThruster = new List<float>();
	List<int> acceptedThruster = new List<int>();


	void CalculateSolution_Attempt1(Vector3 targetForce, Vector3 targetTorque, float[] powerPerThruster)
	{
		int lastIterationSolutionsFound = 0;
		int iteration = 0;
		float minFitness = 0.1f;
		do
		{ 
			//minFitness = Mathf.Lerp(0.5f, 0.9f, iteration / 100.0f);
			lastIterationSolutionsFound = 0;

			// force
			{ 
				fitnessPerThruster.Clear();
				fitnessPerThruster.Capacity = Math.Max(connectedThrusters.Length, fitnessPerThruster.Capacity);
				float maxFitness = minFitness;

				for (int i = 0; i < connectedThrusters.Length; ++i)
				{
					var thruster = connectedThrusters[i];
					float fitness = 0;
					if (thruster.MaxPower > 0)
					{ 
						fitness = Math.Max(0, Vector3.Dot(targetForce.normalized, thruster.shipRoot_direction));
					}
					fitnessPerThruster.Add(fitness);
					maxFitness = Math.Max(fitness, maxFitness);
				}

				acceptedThruster.Clear();
				acceptedThruster.Capacity = Math.Max(connectedThrusters.Length, acceptedThruster.Capacity);
				float sumMaxForce = 0;
				for (int i = 0; i < connectedThrusters.Length; ++i)
				{
					var thruster = connectedThrusters[i];
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
					var thruster = connectedThrusters[i];
					float thrusterPower = powerRatio * thruster.MaxPower;
					powerPerThruster[i] += thrusterPower;
					targetForce -= thruster.shipRoot_direction * thrusterPower;
					targetTorque -= thruster.shipRoot_torqueWithPowerOne * thrusterPower;
				}

				lastIterationSolutionsFound += acceptedThruster.Count;
			}

			// torque
			{ 
				fitnessPerThruster.Clear();
				fitnessPerThruster.Capacity = Math.Max(connectedThrusters.Length, fitnessPerThruster.Capacity);
				float maxFitness = minFitness;

				for (int i = 0; i < connectedThrusters.Length; ++i)
				{
					var thruster = connectedThrusters[i];
					float fitness = 0;
					if (thruster.MaxPower > 0)
					{ 
						fitness = Math.Max(0, Vector3.Dot(targetTorque.normalized, thruster.shipRoot_torqueWithPowerOne.normalized));
					}
					fitnessPerThruster.Add(fitness);
					maxFitness = Math.Max(fitness, maxFitness);
				}

				acceptedThruster.Clear();
				acceptedThruster.Capacity = Math.Max(connectedThrusters.Length, acceptedThruster.Capacity);
				float sumMaxTorque = 0;
				for (int i = 0; i < connectedThrusters.Length; ++i)
				{
					var thruster = connectedThrusters[i];
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
					var thruster = connectedThrusters[i];
					float thrusterPower = powerRatio * thruster.MaxPower;
					powerPerThruster[i] += thrusterPower;
					targetForce -= thruster.shipRoot_direction * thrusterPower;
					targetTorque -= thruster.shipRoot_torqueWithPowerOne * thrusterPower;
				}

				lastIterationSolutionsFound += acceptedThruster.Count;
			}

		} while (++iteration < 100 && lastIterationSolutionsFound > 0);

		
		// if one thruster target power is above its limit, decrease all thruster power by same ratio
		{ 
			float multipleAllBy = 1;
			for (int i = 0; i < connectedThrusters.Length; i++)
			{
				var t = connectedThrusters[i];
				var wantedPower = powerPerThruster[i];

				if (wantedPower > t.MaxPower)
				{
					var newMultAllBy = t.MaxPower / wantedPower;
					multipleAllBy = Math.Min(multipleAllBy, newMultAllBy);
				}
			}

			if (multipleAllBy != 1)
			{
				for (int i = 0; i < connectedThrusters.Length; i++)
				{
					powerPerThruster[i] *= multipleAllBy;
				}
			}
		}
	}

	void CalculateSolution_Attempt2(Vector3 targetForce, Vector3 targetTorque, float[] powerPerThruster)
	{
		int lastIterationSolutionsFound = 0;	
		int iteration = 0;
		float minFitness = 0.5f;
		do
		{ 
			fitnessPerThruster.Clear();
			fitnessPerThruster.Capacity = Math.Max(connectedThrusters.Length, fitnessPerThruster.Capacity);
			float maxFitness = minFitness;
			Vector3 targetForceNormalized = targetForce.normalized;
			Vector3 targetTorqueNormalized = targetTorque.normalized;
			for (int i = 0; i < connectedThrusters.Length; ++i)
			{
				var thruster = connectedThrusters[i];
				float fitness = 0;
				if (thruster.MaxPower > 0)
				{ 
					fitness = Vector3.Dot(targetForceNormalized, thruster.shipRoot_direction)
							+ Vector3.Dot(targetTorqueNormalized, thruster.shipRoot_torqueWithPowerOne_normalized);
				}
				fitnessPerThruster.Add(fitness);
				maxFitness = Math.Max(fitness, maxFitness);
			}

			acceptedThruster.Clear();
			acceptedThruster.Capacity = Math.Max(connectedThrusters.Length, acceptedThruster.Capacity);
			float sumMaxForce = 0;
			float sumMaxTorque = 0;
			float acceptableFitness = maxFitness * 0.9f;
			for (int i = 0; i < connectedThrusters.Length; ++i)
			{
				var thruster = connectedThrusters[i];
				var fitness = fitnessPerThruster[i];
				if (fitness > acceptableFitness) 
				{ 
					acceptedThruster.Add(i);
					sumMaxForce += thruster.MaxPower 
						* Math.Max(0, Vector3.Dot(targetForceNormalized, thruster.shipRoot_direction));
					sumMaxTorque += thruster.shipRoot_torqueWithPowerOne.magnitude * thruster.MaxPower
						* Math.Max(0, Vector3.Dot(targetTorqueNormalized, thruster.shipRoot_torqueWithPowerOne_normalized));
				}
			}

			float forceRatio = sumMaxForce <= 0 ? 0.001f : Math.Min(targetForce.magnitude / sumMaxForce, 1);
			float torqueRatio = sumMaxTorque <= 0 ? 0.001f : Math.Min(targetTorque.magnitude / sumMaxTorque, 1);
			float powerRatio = (forceRatio + torqueRatio) / 2.0f;
			//float powerRatio = Math.Max(forceRatio, torqueRatio);

			// distribute equally between all accepted
			for (int j = 0; j < acceptedThruster.Count; ++j)
			{
				int i = acceptedThruster[j];
				var thruster = connectedThrusters[i];
				float r = (
					Math.Max(0, Vector3.Dot(targetForceNormalized, thruster.shipRoot_direction)) +
					Math.Max(0, Vector3.Dot(targetTorqueNormalized, thruster.shipRoot_torqueWithPowerOne_normalized))
				) / 2.0f;
				float thrusterPower = powerRatio * thruster.MaxPower * r;
				powerPerThruster[i] += thrusterPower;
				targetForce -= thruster.shipRoot_direction * thrusterPower;
				targetTorque -= thruster.shipRoot_torqueWithPowerOne * thrusterPower;
			}

			// found perfect solution
			if (targetForce.magnitude <= 0.01f && targetTorque.magnitude <= 0.01f) break;

			// didn't find perfect solution and didnt adjust any thruster, decrease fitness critera
			if (acceptedThruster.Count == 0) minFitness *= 0.5f;

		} while (++iteration < 100);

		// if one thruster target power is above its limit, decrease all thruster power by same ratio
		{ 
			float multipleAllBy = 1;
			for (int i = 0; i < connectedThrusters.Length; i++)
			{
				var t = connectedThrusters[i];
				var wantedPower = powerPerThruster[i];

				if (wantedPower > t.MaxPower)
				{
					var newMultAllBy = t.MaxPower / wantedPower;
					multipleAllBy = Math.Min(multipleAllBy, newMultAllBy);
				}
			}

			if (multipleAllBy != 1)
			{
				for (int i = 0; i < connectedThrusters.Length; i++)
				{
					powerPerThruster[i] *= multipleAllBy;
				}
			}
		}
	}

	void CalculateSolution_Attempt3(Vector3 targetForce, Vector3 targetTorque, float[] powerPerThruster)
	{	
		string debug = Time.frameCount + "\n\n";

		int lastIterationSolutionsFound = 0;	
		
		float previousTargetForceMagnitude = targetForce.magnitude;
		float previousTargetTorqueMagnitude = targetTorque.magnitude;
		int iteration = 0;
		float minFitness = 0.5f;
		do
		{ 
			debug += "\n#" + iteration;
			debug += " t " + targetForce.magnitude.ToString("0") + "," + targetTorque.magnitude.ToString("0");

			{ 
				fitnessPerThruster.Clear();
				fitnessPerThruster.Capacity = Math.Max(connectedThrusters.Length, fitnessPerThruster.Capacity);
				float maxFitness = minFitness;
				Vector3 targetForceNormalized = targetForce.normalized;
				Vector3 targetTorqueNormalized = targetTorque.normalized;
				for (int i = 0; i < connectedThrusters.Length; ++i)
				{
					var thruster = connectedThrusters[i];
					float fitness = 0;
					if (thruster.MaxPower > 0)
					{ 
						//float maxPower = thruster.MaxPower - powerPerThruster[i];

						var f1 = targetForce;
						var f2 = targetForce - thruster.shipRoot_direction.normalized * targetForce.magnitude;
						fitness += 
							Vector3.Distance(f1, f2) *
							(f1.magnitude > f2.magnitude ? 1 : -1);

						var t1 = targetTorque;
						var t2 = targetTorque - thruster.shipRoot_torqueWithPowerOne;
						fitness += 
							Vector3.Distance(t1, t2) *
							(t1.magnitude > t2.magnitude ? 1 : -1);
					}

					fitnessPerThruster.Add(fitness);
					maxFitness = Math.Max(fitness, maxFitness);
				}

				acceptedThruster.Clear();
				acceptedThruster.Capacity = Math.Max(connectedThrusters.Length, acceptedThruster.Capacity);
				float sumMaxForce = 0;
				float sumMaxTorque = 0;
				float acceptableFitness = maxFitness * 0.9f;
				for (int i = 0; i < connectedThrusters.Length; ++i)
				{
					var thruster = connectedThrusters[i];
					var fitness = fitnessPerThruster[i];
					if (fitness > acceptableFitness) 
					{ 
						acceptedThruster.Add(i);
						float remainingPower = thruster.MaxPower - powerPerThruster[i];
						sumMaxForce += remainingPower 
							* Math.Max(0, Vector3.Dot(targetForceNormalized, thruster.shipRoot_direction));
						sumMaxTorque += thruster.shipRoot_torqueWithPowerOne.magnitude * remainingPower
							* Math.Max(0, Vector3.Dot(targetTorqueNormalized, thruster.shipRoot_torqueWithPowerOne_normalized));
					}
				}

				float forceRatio = sumMaxForce <= 0 ? 0.001f : Math.Min(targetForce.magnitude / sumMaxForce, 1);
				float torqueRatio = sumMaxTorque <= 0 ? 0.001f : Math.Min(targetTorque.magnitude / sumMaxTorque, 1);
				float powerRatio = (forceRatio + torqueRatio) / 2.0f;
				//float powerRatio = Math.Max(forceRatio, torqueRatio);

				// distribute equally between all accepted
				for (int j = 0; j < acceptedThruster.Count; ++j)
				{
					int i = acceptedThruster[j];
					var thruster = connectedThrusters[i];
					float remainingPower = thruster.MaxPower - powerPerThruster[i];
					float r = (
						Math.Max(0, Vector3.Dot(targetForceNormalized, thruster.shipRoot_direction)) +
						Math.Max(0, Vector3.Dot(targetTorqueNormalized, thruster.shipRoot_torqueWithPowerOne_normalized))
					) / 2.0f;
					float thrusterPower = powerRatio * remainingPower * r;
					powerPerThruster[i] += thrusterPower;
					targetForce -= thruster.shipRoot_direction * thrusterPower;
					targetTorque -= thruster.shipRoot_torqueWithPowerOne * thrusterPower;
				}

				debug +=
					", +++ " + acceptedThruster.Count + 
					", fmax " + maxFitness +  
					", fmin " + minFitness +
					", t " + targetForce.magnitude.ToString("0") + "," + targetTorque.magnitude.ToString("0");
			}

			// do the same as above, but opposite, find thruster which to decrease power for in order to improve solution
			if (false)
			{
				fitnessPerThruster.Clear();
				fitnessPerThruster.Capacity = Math.Max(connectedThrusters.Length, fitnessPerThruster.Capacity);
				float maxFitness = minFitness;
				Vector3 targetForceNormalized = targetForce.normalized;
				Vector3 targetTorqueNormalized = targetTorque.normalized;
				for (int i = 0; i < connectedThrusters.Length; ++i)
				{
					var thruster = connectedThrusters[i];
					float fitness = 0;
					if (thruster.MaxPower > 0)
					{ 
						fitness = Vector3.Dot(targetForceNormalized, -thruster.shipRoot_direction)
								+ Vector3.Dot(targetTorqueNormalized, -thruster.shipRoot_torqueWithPowerOne_normalized);
						fitness *= powerPerThruster[i] / thruster.MaxPower;
					}
					fitnessPerThruster.Add(fitness);
					maxFitness = Math.Max(fitness, maxFitness);
				}

				acceptedThruster.Clear();
				acceptedThruster.Capacity = Math.Max(connectedThrusters.Length, acceptedThruster.Capacity);
				float sumMaxForce = 0;
				float sumMaxTorque = 0;
				float acceptableFitness = maxFitness * 0.9f;
				for (int i = 0; i < connectedThrusters.Length; ++i)
				{
					var thruster = connectedThrusters[i];
					var fitness = fitnessPerThruster[i];
					if (fitness > acceptableFitness) 
					{ 
						acceptedThruster.Add(i);
						sumMaxForce += powerPerThruster[i] 
							* Math.Max(0, Vector3.Dot(targetForceNormalized, -thruster.shipRoot_direction));
						sumMaxTorque += thruster.shipRoot_torqueWithPowerOne.magnitude * powerPerThruster[i]
							* Math.Max(0, Vector3.Dot(targetTorqueNormalized, -thruster.shipRoot_torqueWithPowerOne_normalized));
					}
				}

				float forceRatio = sumMaxForce <= 0 ? 0.001f : Math.Min(targetForce.magnitude / sumMaxForce, 1);
				float torqueRatio = sumMaxTorque <= 0 ? 0.001f : Math.Min(targetTorque.magnitude / sumMaxTorque, 1);
				float powerRatio = (forceRatio + torqueRatio) / 2.0f;
				//float powerRatio = Math.Max(forceRatio, torqueRatio);

				// distribute equally between all accepted
				for (int j = 0; j < acceptedThruster.Count; ++j)
				{
					int i = acceptedThruster[j];
					var thruster = connectedThrusters[i];
					float r = (
						Math.Max(0, Vector3.Dot(targetForceNormalized, -thruster.shipRoot_direction)) +
						Math.Max(0, Vector3.Dot(targetTorqueNormalized, -thruster.shipRoot_torqueWithPowerOne_normalized))
					) / 2.0f;
					float thrusterPower = powerRatio * powerPerThruster[i] * r;
					powerPerThruster[i] += thrusterPower;
					targetForce -= -thruster.shipRoot_direction * thrusterPower;
					targetTorque -= -thruster.shipRoot_torqueWithPowerOne * thrusterPower;
				}

				debug +=
					", --- " + acceptedThruster.Count + 
					", fmax " + maxFitness +  
					", fmin " + minFitness +
					", t " + targetForce.magnitude.ToString("0") + "," + targetTorque.magnitude.ToString("0");
			}


			// found perfect solution
			if (targetForce.magnitude <= 0.01f && targetTorque.magnitude <= 0.01f) break;

			// unable to find good solution, decrease requirements
			{ 
				if (targetForce.magnitude > previousTargetForceMagnitude)
				{
					targetForce *= 0.9f;
					debug += 
						", decreasing force" +
						", t " + targetForce.magnitude.ToString("0") + "," + targetTorque.magnitude.ToString("0");
				}

				if (targetTorque.magnitude > previousTargetTorqueMagnitude)
				{
					targetTorque *= 0.9f;
					debug += 
						", decreasing torque" +
						", t " + targetForce.magnitude.ToString("0") + "," + targetTorque.magnitude.ToString("0");
				}
			}

			// didn't find perfect solution and didnt adjust any thruster, decrease fitness critera
			//if (acceptedThruster.Count == 0) minFitness *= 0.5f;

			previousTargetForceMagnitude = targetForce.magnitude;
			previousTargetTorqueMagnitude = targetTorque.magnitude;

		} while (++iteration < 100);

		Debug.Log(debug);
	}

	void ComputeAndSet(Vector3 tm, Vector3 td)
	{
		var targetForce = -tm;
		var targetTorque = td;

		if (targetForce.sqrMagnitude <= 0 && targetTorque.sqrMagnitude <= 0)
		{
			for (int i = 0; i < connectedThrusters.Length; i++)
				connectedThrusters[i].SetPower(0);
			return;
		}

		//string debug = "errors: ";

		//{ 
		//	var powerPerThruster = new float[connectedThrusters.Length];
		//	CalculateSolution_Attempt1(targetForce, targetTorque, powerPerThruster);
		//	debug += "s1:" + GetError(powerPerThruster, targetForce, targetTorque) + ", ";
		//}

		//{ 
		//	var powerPerThruster = new float[connectedThrusters.Length];
		//	CalculateSolution_Attempt2(targetForce, targetTorque, powerPerThruster);
		//	debug += "s2:" + GetError(powerPerThruster, targetForce, targetTorque) + ", ";
		//}

		//{ 
		//	var powerPerThruster = new float[connectedThrusters.Length];
		//	CalculateSolution_Attempt3(targetForce, targetTorque, powerPerThruster);
		//	debug += "s3:" + GetError(powerPerThruster, targetForce, targetTorque);
		//}
		
		//Debug.Log(debug);


		var finalPowerPerThruster = new float[connectedThrusters.Length];
		CalculateSolution_Attempt2(targetForce, targetTorque, finalPowerPerThruster);


		ActualThrustersForce = Vector3.zero;
		ActualThrustersAngularForce = Vector3.zero;

		for (int i = 0; i < connectedThrusters.Length; i++)
		{
			var thruster = connectedThrusters[i];
			var power = finalPowerPerThruster[i];
			thruster.SetPower(power);

			ActualThrustersForce += thruster.shipRoot_direction * power;
			ActualThrustersAngularForce += thruster.shipRoot_torqueWithPowerOne * power;
		}

	}

}
