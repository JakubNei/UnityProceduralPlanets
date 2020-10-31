using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

public class ShipControlComputer : MonoBehaviour
{

	public Vector3 centerOfMass;

	ThrusterObject[] conectedThrusters;


	public Vector3 CurrentVelocity => rigidbody.transform.InverseTransformDirection(rigidbody.velocity);
	public Vector3 CurrentAngularVelocity => rigidbody.transform.InverseTransformDirection(rigidbody.angularVelocity);


	public bool calculateVisuallyGoodForce = true;
	public bool calculateVisuallyGoodTorque = true;

	public bool calculateNNLSForce = true;
	public bool calculateNNLSTorque = true;
	
	Rigidbody rigidbody;
	Rigidbody rb { get { return this.transform.root.GetComponentInChildren<Rigidbody>(); } }

	// Use this for initialization
	void Start()
	{
		rigidbody = GetComponent<Rigidbody>();
		centerOfMass = rigidbody.centerOfMass;
		conectedThrusters = GetComponentsInChildren<ThrusterObject>();

		foreach (var thruster in conectedThrusters)
			thruster.Initialize(centerOfMass);
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
			force += t.mountedDirection * f;
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
			torque += Cross(t.mountedDirection * f, t.localOffsetFromCenterOfMass);
		}
		return torque;
	}

	static Vector3 GetForce(ThrusterObject thruster, float power)
	{
		return thruster.mountedDirection * power;
	}

	static Vector3 GetTorque(ThrusterObject thruster, float power)
	{
		return Cross(thruster.mountedDirection, thruster.localOffsetFromCenterOfMass) * power;
	}
	static Vector3 GetMaxTorque(ThrusterObject thruster)
	{
		return GetTorque(thruster, thruster.MaxPower);
	}


	void ComputeAndSet(Vector3 tm, Vector3 td)
	{
		var targetForce = -tm;
		var targetTorque = td;

		var originalTargetForce = targetForce;
		var originalTargetTorque = targetTorque;

		if (targetForce.sqrMagnitude <= 0 && targetTorque.sqrMagnitude <= 0)
		{
			for (int i = 0; i < conectedThrusters.Length; i++)
				conectedThrusters[i].SetThrust(0);
			return;
		}

		var globalThrustersPower = new float[conectedThrusters.Length];
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
					if (Vector3.Distance(t.mountedDirection, targetForce) < minDistance)
						thrusterIndexes.Add(i);
				}
				if (thrusterIndexes.Count > 0)
				{
					var allSumForce = GetSumForce(conectedThrusters, localThrustersPower);
					var allSumForceRatioOnTarget = Vector3.Dot(allSumForce, targetForceNormalized);

					var newSumForce = thrusterIndexes.Select(ti => conectedThrusters[ti]).Select(t => t.mountedDirection * t.MaxPower).Aggregate((a, b) => a + b);
					var newSumForceRatioOnTarget = Vector3.Dot(newSumForce, targetForceNormalized);

					var newForceRatioNeeded = 1f - allSumForceRatioOnTarget;

					var newForceMultiplier = 1f;
					if (newSumForceRatioOnTarget > newForceRatioNeeded)
						newForceMultiplier = newForceRatioNeeded / newSumForceRatioOnTarget;

					// set the force
					foreach (var i in thrusterIndexes)
					{
						var f = conectedThrusters[i].MaxPower;
						localThrustersPower[i] = f;
						globalThrustersPower[i] += f;
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
						var f = conectedThrusters[i].MaxPower * newTorqueMultiplier;
						localThrustersPower[i] = f;
						globalThrustersPower[i] += f;
					}

					if (allSumTorqueRatioOnTarget + newSumTorqueRatioOnTarget >= 1) break;
				}
				minDistance *= 2;
			} while (minDistance < 90);

			// remove our forces from target, so NNLS can calculate the remaining solution
			targetForce -= GetSumForce(conectedThrusters, localThrustersPower);
			targetTorque -= GetSumTorque(conectedThrusters, localThrustersPower);
		}


		// if one thruster target force is above its limit, decrease all thruster force solution by same amount
		double multipleAllBy = 1;
		for (int i = 0; i < conectedThrusters.Length; i++)
		{
			var t = conectedThrusters[i];
			var f = globalThrustersPower[i];

			if (t.MaxPower < f)
			{
				var newMultAllBy = t.MaxPower / f;
				if (newMultAllBy < multipleAllBy) multipleAllBy = newMultAllBy;
			}
		}



		//Debug.Log("error: " + GetError(conectedThrusters, globalThrustersPower, originalTargetForce, originalTargetTorque) + ", power multiplier:" + multipleAllBy);





		for (int i = 0; i < conectedThrusters.Length; i++)
		{
			var t = conectedThrusters[i];
			var f = globalThrustersPower[i];
			t.SetThrust(f * multipleAllBy);
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
			return Vector3.Angle(t.mountedDirection * t.MaxPower, targetForce);
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
