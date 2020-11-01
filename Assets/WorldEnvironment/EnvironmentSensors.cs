using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentSensors : MonoBehaviour
{
	public static EnvironmentSensors main { get; private set; }

	private void Awake()
	{
		main = this;
	}

	public Vector3 GetGravityAt(BigPosition position)
	{
		var p = ProceduralPlanets.main.GetClosestPlanet(position);
		if (p == null) return Vector3.zero;

		var d = BigPosition.Distance(position, p.BigPosition);
		var v = (p.BigPosition - position).normalized;
		return p.GetGravityAtDistanceFromCenter(d) * v.ToVector3();
	}

	// mass in g of air/atmosphere per m^3 at position, can be used to calculate drag
	public float GetAirDensityAt(BigPosition position)
	{
		var p = ProceduralPlanets.main.GetClosestPlanet(position);
		if (p == null) return 0;

		var d = BigPosition.Distance(position, p.BigPosition);
		return p.GetGravityAtDistanceFromCenter(d);
	}

}
