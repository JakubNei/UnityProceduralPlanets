using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class ShipControlComputer : MonoBehaviour
{

	public Vector3 shipRoot_centerOfMass;

	ThrusterObject[] conectedThrusters;


	public float ShipMass => rigidbody.mass;
	public Vector3 CurrentVelocity => rigidbody.transform.InverseTransformDirection(rigidbody.velocity);
	public Vector3 CurrentAngularVelocity => rigidbody.transform.InverseTransformDirection(rigidbody.angularVelocity);


	public bool calculateVisuallyGoodForce = true;
	public bool calculateVisuallyGoodTorque = true;

	public bool calculateNNLSForce = true;
	public bool calculateNNLSTorque = true;

	Rigidbody rigidbody;

	Transform ShipRoot => transform;

	// Use this for initialization
	void Start()
	{
		rigidbody = GetComponent<Rigidbody>();
		conectedThrusters = GetComponentsInChildren<ThrusterObject>();
		shipRoot_centerOfMass = ShipRoot.InverseTransformPoint(rigidbody.worldCenterOfMass);;

		foreach (var thruster in conectedThrusters)
			thruster.Initialize(shipRoot_centerOfMass);
	}




	public void SetTarget(Vector3 targetForce, Vector3 targetTorque)
	{
		//var force = targetLocalMoveDirection.magnitude > 0 ? 1 : 0;
		//foreach (var thruster in thrusters)
		//{
		//    thruster.SetThrust(targetLocalMoveDirection, force);
		//}

		// https://www.youtube.com/watch?v=Lg3P4uIlgeU
		ComputeAndSet(targetForce, targetTorque);
	}


	static float GetError(ThrusterObject[] thrusters, float[] candidateSolution, Vector3 targetForce, Vector3 targetTorque)
	{
		var totalForce = Vector3.zero;
		var totalTorque = Vector3.zero;

		for (int i = 0; i < thrusters.Length; i++)
		{
			var t = thrusters[i];
			var f = (float)candidateSolution[i];
			totalForce += GetForce(t, f);
			totalTorque += GetTorque(t, f);
		}

		var error = (totalForce - targetForce).magnitude + (totalTorque - targetTorque).magnitude;

		return error;
	}

	static Vector3 GetSumForce(ThrusterObject[] thrusters, float[] power)
	{
		var force = Vector3.zero;
		for (int i = 0; i < thrusters.Length; i++)
		{
			var t = thrusters[i];
			var f = power[i];
			force += t.shipRoot_direction * f;
		}
		return force;
	}

	/// <summary>
	/// Right Hand Side cross product.
	/// (Unity uses Left Hand Side cross product)
	/// </summary>
	/// <param name="a"></param>
	/// <param name="b"></param>
	/// <returns></returns>
	static Vector3 Cross(Vector3 a, Vector3 b)
	{
		return new Vector3(
			a.y * b.z - a.z * b.y,
			a.z * b.x - a.x * b.z,
			a.x * b.y - a.y * b.x
		);
	}

	static Vector3 GetSumTorque(ThrusterObject[] thrusters, float[] power)
	{
		var torque = Vector3.zero;
		for (int i = 0; i < thrusters.Length; i++)
		{
			var t = thrusters[i];
			var f = power[i];
			torque += Cross(t.shipRoot_direction * f, t.shipRoot_offsetFromCenterOfMass);
		}
		return torque;
	}

	static Vector3 GetForce(ThrusterObject thruster, float power)
	{
		return thruster.shipRoot_direction * power;
	}

	static Vector3 GetTorque(ThrusterObject thruster, float power)
	{
		return Cross(thruster.shipRoot_direction, thruster.shipRoot_offsetFromCenterOfMass) * power;
	}
	static Vector3 GetMaxTorque(ThrusterObject thruster)
	{
		return GetTorque(thruster, thruster.MaxPower);
	}


	void ComputeAndSet(Vector3 tm, Vector3 td)
	{
		var targetForce = -tm;
		var targetTorque = td;


		if (targetForce.sqrMagnitude <= 0 && targetTorque.sqrMagnitude <= 0)
		{
			for (int i = 0; i < conectedThrusters.Length; i++)
				conectedThrusters[i].SetThrust(0);
			return;
		}

		var finalPowerPerThruster = new float[conectedThrusters.Length];

		/*
		for (int i = 0; i < conectedThrusters.Length; ++i)
		{
			var thruster = conectedThrusters[i];
			var error = float.MaxValue;

			float canProvideForce = thruster.MaxPower * Vector3.Dot(targetForce.normalized, thruster.shipRoot_direction);

			Vector3 torqueWithPowerOne = Cross(thruster.shipRoot_direction, thruster.shipRoot_offsetFromCenterOfMass);
			Vector3 torqueAtMaxPower = torqueWithPowerOne * thruster.MaxPower;
			float canProvideTorque = torqueAtMaxPower.magnitude * Math.Max(0, Vector3.Dot(targetTorque.normalized, torqueWithPowerOne.normalized));			

			if (canProvideForce > 0 || canProvideTorque > 0)
			{
				float thrusterPower = canProvideForce;
				if (thrusterPower > targetForce.magnitude) thrusterPower = targetForce.magnitude;

				targetForce -= thruster.shipRoot_direction * thrusterPower;
				targetTorque -= Cross(thruster.shipRoot_direction, thruster.shipRoot_offsetFromCenterOfMass) * thrusterPower;

				finalPowerPerThruster[i] = thrusterPower;
			}
		}
		*/

		/*
		float minError = float.MaxValue;
		int iteration = 0;	
		do
		{ 
			var errorPerThruster = new float[conectedThrusters.Length];
		
			minError = float.MaxValue;

			for (int i = 0; i < conectedThrusters.Length; ++i)
			{
				var thruster = conectedThrusters[i];
				var error = float.MaxValue;

				Vector3 torqueWithPowerOne = Cross(thruster.shipRoot_direction, thruster.shipRoot_offsetFromCenterOfMass);

				error = 
					Vector3.Angle(targetForce.normalized, thruster.shipRoot_direction) +
					Vector3.Angle(targetTorque.normalized, torqueWithPowerOne.normalized);

				errorPerThruster[i] = error;

				minError = Math.Min(error, minError);
			}

			List<int> acceptableError = new List<int>();
			float sumMaxForce = 0;
			for (int i = 0; i < conectedThrusters.Length; ++i)
			{
				var thruster = conectedThrusters[i];
				var error = errorPerThruster[i];
				if (error * 0.9f > minError) continue;
				acceptableError.Add(i);
				sumMaxForce += thruster.MaxPower;
			}


			float powerRatio = targetForce.magnitude / sumMaxForce;
			if (powerRatio > 1) powerRatio = 1;

			// distribute equally between all acceptable
			for (int j = 0; j < acceptableError.Count; ++j)
			{
				int i = acceptableError[j];
				var thruster = conectedThrusters[i];
				finalPowerPerThruster[i] += powerRatio * thruster.MaxPower;
			}

		} while (false && minError > 0 && ++iteration < 10);
		*/



	
		int iteration = 0;	
		do
		{ 
			// force
			if (true)
			{ 
				var fitnessPerThruster = new float[conectedThrusters.Length];	
				float maxFitness = 0;

				for (int i = 0; i < conectedThrusters.Length; ++i)
				{
					var thruster = conectedThrusters[i];
					var fitness = Math.Max(0, Vector3.Dot(targetForce.normalized, thruster.shipRoot_direction));
					fitnessPerThruster[i] = fitness;
					maxFitness = Math.Max(fitness, maxFitness);
				}

				List<int> acceptableThrusters = new List<int>();
				float sumMaxForce = 0;
				for (int i = 0; i < conectedThrusters.Length; ++i)
				{
					var thruster = conectedThrusters[i];
					var fitness = fitnessPerThruster[i];
					if (fitness > maxFitness * 0.9f) 
					{ 
						acceptableThrusters.Add(i);
						sumMaxForce += thruster.MaxPower;
					}
				}

				float powerRatio = targetForce.magnitude / sumMaxForce;
				if (powerRatio > 1) powerRatio = 1;

				// distribute equally between all acceptable
				for (int j = 0; j < acceptableThrusters.Count; ++j)
				{
					int i = acceptableThrusters[j];
					var thruster = conectedThrusters[i];
					float thrusterPower = powerRatio * thruster.MaxPower;
					finalPowerPerThruster[i] += thrusterPower;
					targetForce -= thruster.shipRoot_direction * thrusterPower;
					targetTorque -= Cross(thruster.shipRoot_direction, thruster.shipRoot_offsetFromCenterOfMass) * thrusterPower;
				}
			}

			// torque
			if (true)
			{ 
				var fitnessPerThruster = new float[conectedThrusters.Length];	
				float maxFitness = 0;

				for (int i = 0; i < conectedThrusters.Length; ++i)
				{
					var thruster = conectedThrusters[i];
					Vector3 torqueWithPowerOne = Cross(thruster.shipRoot_direction, thruster.shipRoot_offsetFromCenterOfMass);
					var fitnes = Math.Max(0, Vector3.Dot(targetTorque.normalized, torqueWithPowerOne.normalized));
					fitnessPerThruster[i] = fitnes;
					maxFitness = Math.Max(fitnes, maxFitness);
				}

				List<int> acceptableThrusters = new List<int>();
				float sumMaxTorque = 0;
				for (int i = 0; i < conectedThrusters.Length; ++i)
				{
					var thruster = conectedThrusters[i];
					var fitness = fitnessPerThruster[i];
					if (fitness > maxFitness * 0.9f) 
					{ 
						acceptableThrusters.Add(i);
						Vector3 torqueWithPowerOne = Cross(thruster.shipRoot_direction, thruster.shipRoot_offsetFromCenterOfMass);
						sumMaxTorque += torqueWithPowerOne.magnitude * thruster.MaxPower;
					}
				}

				float powerRatio = targetTorque.magnitude / sumMaxTorque;
				if (powerRatio > 1) powerRatio = 1;

				// distribute equally between all acceptable
				for (int j = 0; j < acceptableThrusters.Count; ++j)
				{
					int i = acceptableThrusters[j];
					var thruster = conectedThrusters[i];
					float thrusterPower = powerRatio * thruster.MaxPower;
					finalPowerPerThruster[i] += thrusterPower;
					targetForce -= thruster.shipRoot_direction * thrusterPower;
					targetTorque -= Cross(thruster.shipRoot_direction, thruster.shipRoot_offsetFromCenterOfMass) * thrusterPower;
				}
			}

		} while (++iteration < 10);






		/*
		var originalTargetForce = targetForce;
		var originalTargetTorque = targetTorque;


		var localThrustersPower = new float[conectedThrusters.Length];		


		if (calculateVisuallyGoodForce && targetForce.sqrMagnitude > 0)
		{
			var thrusterIndexes = new List<int>(conectedThrusters.Length);
			var targetForceNormalized = targetForce.normalized;

			// find ideally looking force for thrusters
			float minDistance = 1;
			do
			{
				thrusterIndexes.Clear();

				for (int i = 0; i < conectedThrusters.Length; i++)
				{
					var t = conectedThrusters[i];
					if (Vector3.Distance(t.shipRoot_direction, targetForce) < minDistance)
						thrusterIndexes.Add(i);
				}
				if (thrusterIndexes.Count > 0)
				{
					var allSumForce = GetSumForce(conectedThrusters, localThrustersPower);
					var allSumForceRatioOnTarget = Vector3.Dot(allSumForce, targetForceNormalized);

					var newSumForce = thrusterIndexes.Select(ti => conectedThrusters[ti]).Select(t => t.shipRoot_direction * t.MaxThrust).Aggregate((a, b) => a + b);
					var newSumForceRatioOnTarget = Vector3.Dot(newSumForce, targetForceNormalized);

					var newForceRatioNeeded = 1f - allSumForceRatioOnTarget;

					var newForceMultiplier = 1f;
					if (newSumForceRatioOnTarget > newForceRatioNeeded)
						newForceMultiplier = newForceRatioNeeded / newSumForceRatioOnTarget;

					// set the force
					foreach (var i in thrusterIndexes)
					{
						var f = conectedThrusters[i].MaxThrust;
						localThrustersPower[i] = f;
						powerPerThruster[i] += f;
					}

					if (allSumForceRatioOnTarget + newSumForceRatioOnTarget >= 1) break;
				}
				minDistance *= 2;
			} while (minDistance < 90);

			// remove our forces from target, so NNLS can calculate the remaining solution
			targetForce -= GetSumForce(conectedThrusters, localThrustersPower);
			targetTorque -= GetSumTorque(conectedThrusters, localThrustersPower);
		}


		if (calculateVisuallyGoodTorque && targetTorque.sqrMagnitude > 0)
		{
			var thrusterIndexes = new List<int>(conectedThrusters.Length);
			var targetTorqueNormalized = targetTorque.normalized;

			// find ideally looking force for thrusters
			float minDistance = 1;
			do
			{
				thrusterIndexes.Clear();
				for (int i = 0; i < conectedThrusters.Length; i++)
				{
					var t = conectedThrusters[i];
					if (Vector3.Distance(GetMaxTorque(t), targetTorque) < minDistance)
						thrusterIndexes.Add(i);
				}
				if (thrusterIndexes.Count > 0)
				{
					var allSumTorque = GetSumTorque(conectedThrusters, localThrustersPower);
					var allSumTorqueRatioOnTarget = Vector3.Dot(allSumTorque, targetTorqueNormalized);

					var newSumTorque = thrusterIndexes.Select(ti => conectedThrusters[ti]).Select(t => GetMaxTorque(t)).Aggregate((a, b) => a + b);
					var newSumTorqueRatioOnTarget = Vector3.Dot(newSumTorque, targetTorqueNormalized);

					var newTorqueRatioNeeded = 1f - allSumTorqueRatioOnTarget;

					var newTorqueMultiplier = 1f;
					if (newSumTorqueRatioOnTarget > newTorqueRatioNeeded)
						newTorqueMultiplier = newTorqueRatioNeeded / newSumTorqueRatioOnTarget;

					// set the force
					foreach (var i in thrusterIndexes)
					{
						var f = conectedThrusters[i].MaxThrust * newTorqueMultiplier;
						localThrustersPower[i] = f;
						powerPerThruster[i] += f;
					}

					if (allSumTorqueRatioOnTarget + newSumTorqueRatioOnTarget >= 1) break;
				}
				minDistance *= 2;
			} while (minDistance < 90);

			// remove our forces from target, so NNLS can calculate the remaining solution
			targetForce -= GetSumForce(conectedThrusters, localThrustersPower);
			targetTorque -= GetSumTorque(conectedThrusters, localThrustersPower);
		}
		*/

		// if one thruster target force is above its limit, decrease all thruster force solution by same amount
		double multipleAllBy = 1;
		for (int i = 0; i < conectedThrusters.Length; i++)
		{
			var t = conectedThrusters[i];
			var wantedPower = finalPowerPerThruster[i];

			if (wantedPower > t.MaxPower)
			{
				var newMultAllBy = t.MaxPower / wantedPower;
				if (newMultAllBy < multipleAllBy) multipleAllBy = newMultAllBy;
			}
		}
		

		//Debug.Log("error: " + GetError(conectedThrusters, globalThrustersPower, originalTargetForce, originalTargetTorque) + ", power multiplier:" + multipleAllBy);

		for (int i = 0; i < conectedThrusters.Length; i++)
		{
			var t = conectedThrusters[i];
			var f = finalPowerPerThruster[i] * multipleAllBy;
			t.SetThrust(f);
		}


	}

	class ForceIndexedComparer : IComparer<int>
	{
		ThrusterObject[] thrusters;
		Vector3 targetForce;
		public ForceIndexedComparer(ThrusterObject[] thrusters, Vector3 targetForce)
		{
			this.thrusters = thrusters;
			this.targetForce = targetForce;
		}
		float GetWeight(ThrusterObject t)
		{
			return Vector3.Angle(t.shipRoot_direction * t.MaxPower, targetForce);
		}
		public int Compare(int x, int y)
		{
			var tx = thrusters[x];
			var ty = thrusters[y];
			var wx = GetWeight(tx);
			var wy = GetWeight(ty);
			if (wx == wy)
				return -tx.MaxPower.CompareTo(ty.MaxPower); // bigger will be first
			return wx.CompareTo(wy); // smaller will be first
		}
	}

	class TorqueIndexedComparer : IComparer<int>
	{
		ThrusterObject[] thrusters;
		Vector3 targetTorque;
		public TorqueIndexedComparer(ThrusterObject[] thrusters, Vector3 targetTorque)
		{
			this.thrusters = thrusters;
			this.targetTorque = targetTorque;
		}
		float GetWeight(ThrusterObject t)
		{
			return Vector3.Angle(GetMaxTorque(t), targetTorque);
		}
		public int Compare(int x, int y)
		{
			var tx = thrusters[x];
			var ty = thrusters[y];
			var wx = GetWeight(tx);
			var wy = GetWeight(ty);
			if (wx == wy)
				return -tx.MaxPower.CompareTo(ty.MaxPower); // bigger will be first
			return wx.CompareTo(wy); // smaller will be first
		}
	}

}
