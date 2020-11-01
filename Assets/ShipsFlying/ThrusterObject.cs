using UnityEngine;
using System.Collections;
using System.Linq;
using System;
using System.Runtime.InteropServices;
using UnityEditor;

public class ThrusterObject : MonoBehaviour
{
	[SerializeField]
	public Vector3 shipRoot_offsetFromCenterOfMass;

	[SerializeField]
	public Vector3 shipRoot_direction;

	[SerializeField]
	public Vector3 shipRoot_location;

	[SerializeField]
	public Vector3 shipRoot_torqueWithPowerOne;

	[SerializeField]
	Vector3 shipRoot_centerOfMass;

	public float health = 1;
	public float MaxPower { get { return gameObject.activeSelf ? maxPower * health: 0; } }

	[SerializeField]
	float maxPower = 5;

	[SerializeField]
	float targetPower;

	[SerializeField]
	bool createMirroredThruster = false;

	ParticleSystemControl[] particles;

	class ParticleSystemControl
	{
		public GameObject gameObject { get { return ps.gameObject; } }
		ParticleSystem ps;
		float originalRateOverTimeMultiplier;

		public ParticleSystemControl(ParticleSystem ps)
		{
			this.ps = ps;
			originalRateOverTimeMultiplier = ps.emission.rateOverTimeMultiplier;
		}
		public void Adjust(float ratio)
		{
			var emission = ps.emission;
			emission.rateOverTimeMultiplier = originalRateOverTimeMultiplier * ratio;
		}

	}

	Transform ShipRoot => transform.root;

	Rigidbody rb;
	// Use this for initialization
	public void Initialize(Vector3 shipRoot_centerOfMass)
	{
		rb = ShipRoot.GetComponent<Rigidbody>();

		shipRoot_location = ShipRoot.InverseTransformPoint(this.transform.position);
		shipRoot_direction = ShipRoot.InverseTransformDirection(this.transform.forward).normalized;
		this.shipRoot_centerOfMass = shipRoot_centerOfMass;
		shipRoot_offsetFromCenterOfMass = shipRoot_location - shipRoot_centerOfMass;
		shipRoot_torqueWithPowerOne = Vector3.Cross(shipRoot_direction, shipRoot_offsetFromCenterOfMass);

		SetParticles(0);
	}

	public void SetParticles(float a)
	{
		if (particles == null)
			particles = this.transform.GetComponentsInChildren<ParticleSystem>().Select(ps => new ParticleSystemControl(ps)).ToArray();

		foreach (var p in particles)
		{
			if (a > 0)
			{
				p.Adjust(a);
				p.gameObject.SetActive(true);
			}
			else
			{
				p.gameObject.SetActive(false);
			}
		}
	}

	public void SetPower(float power)
	{
		if (power < 0) power = 0;
		if (power > MaxPower) power = MaxPower;
		targetPower = power;
	}

	// Update is called once per frame
	void Update()
	{
		if (targetPower > 0)
			SetParticles(targetPower * 3);
		else
			SetParticles(0);
	}

	private void OnDrawGizmosSelected()
	{	
		if (!EditorApplication.isPlaying)
		{ 
			shipRoot_location = ShipRoot.InverseTransformPoint(this.transform.position);
			shipRoot_direction = ShipRoot.InverseTransformDirection(this.transform.forward).normalized;
			shipRoot_centerOfMass = ShipRoot.InverseTransformPoint(ShipRoot.GetComponent<Rigidbody>().worldCenterOfMass);
			shipRoot_offsetFromCenterOfMass = shipRoot_location - shipRoot_centerOfMass;
		}

		if (transform.parent)
		{
			Rigidbody rigidBody = ShipRoot.GetComponent<Rigidbody>();

			Gizmos.color = Color.blue;
			Gizmos.DrawLine(ShipRoot.TransformPoint(shipRoot_centerOfMass) + ShipRoot.TransformVector(shipRoot_offsetFromCenterOfMass), rigidBody.worldCenterOfMass);
			//Gizmos.DrawLine(ShipRoot.TransformPoint(shipRoot_location), ShipRoot.TransformPoint(shipRoot_location) + ShipRoot.TransformVector(Vector3.Cross(shipRoot_direction, shipRoot_offsetFromCenterOfMass)));

			float mass = rigidBody.mass;
			Func<float, float> visual = t => t / mass;
			Gizmos.matrix = Matrix4x4.TRS(
				ShipRoot.TransformPoint(shipRoot_location), 
				Quaternion.LookRotation(ShipRoot.TransformDirection(shipRoot_direction)), 
				transform.lossyScale
			);

			Gizmos.color = new Color(targetPower > 0 ? 0.3f : 0, 0, 0.5f);
			Gizmos.DrawFrustum(Vector3.zero, 20, visual(maxPower), 0, 1);
			Gizmos.color = new Color(targetPower > 0 ? 0.3f : 0, 0, 1);
			Gizmos.DrawFrustum(Vector3.zero, 20, visual(MaxPower), 0, 1);
			Gizmos.color = Color.red;
			Gizmos.DrawFrustum(Vector3.zero, 19, visual(targetPower), 0, 1);

			// Maintain mirrored thruster game object
			{
				const string mirroredGoName = "mirrored thruster";
				ThrusterObject mirrored = null;
				for (int i = 0; i < this.transform.childCount; ++i)
				{
					if (this.transform.GetChild(i).name != mirroredGoName) continue;
					mirrored = this.transform.GetChild(i).GetComponent<ThrusterObject>();
					if (mirrored) break;
				}

				if (createMirroredThruster)
				{
					if (!mirrored)
					{
						var g = new GameObject(mirroredGoName);
						g.hideFlags = HideFlags.None;
						g.transform.parent = this.transform;
						mirrored = g.AddComponent<ThrusterObject>();
					}

					if (mirrored)
					{
						mirrored.maxPower = this.maxPower;
						var p = ShipRoot.InverseTransformPoint(this.transform.position);
						mirrored.transform.position = ShipRoot.TransformPoint(new Vector3(-p.x, p.y, p.z));

						var r = ShipRoot.InverseTransformDirection(this.transform.forward);
						mirrored.transform.forward = ShipRoot.TransformDirection(new Vector3(-r.x, r.y, r.z));
					}
				}
				else if (mirrored)
				{
					Destroy(mirrored.gameObject);
				}
			}
		}
	}

	private void FixedUpdate()
	{
		if (targetPower > 0)
		{
			var worldDirection = ShipRoot.TransformDirection(shipRoot_direction);
			rb.AddForceAtPosition(
				-worldDirection * targetPower,
				transform.position,
				ForceMode.Force
			);
		}
	}
}
