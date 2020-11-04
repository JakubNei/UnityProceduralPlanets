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
	public Vector3 shipRoot_torqueWithPowerOne_normalized;

	[SerializeField]
	Vector3 shipRoot_centerOfMass;

	public float health = 1;
	public float MaxPower { get { return gameObject.activeSelf ? maxPower * health: 0; } }

	[SerializeField]
	float maxPower = 5;

	[SerializeField]
	float targetPower;

	[SerializeField]
	float currentPower;

	[SerializeField]
	bool createMirroredThruster = false;

	ParticleSystemControl[] particles;

	class ParticleSystemControl
	{
		public GameObject gameObject { get { return ps.gameObject; } }
		ParticleSystem ps;
		Color originalColor;

		public ParticleSystemControl(ParticleSystem ps)
		{
			this.ps = ps;
			originalColor = ps.main.startColor.color;
		}
		public void SetRatio(float ratio)
		{
			var main = ps.main;
			main.startColor = new Color(originalColor.r, originalColor.g, originalColor.b, originalColor.a * ratio);
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
		shipRoot_torqueWithPowerOne_normalized = shipRoot_torqueWithPowerOne.normalized;

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
				p.SetRatio(a);
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
		if (currentPower > 0)
			SetParticles(currentPower / MaxPower);
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
			Gizmos.color = new Color(0.5f, 0, 0);
			Gizmos.DrawFrustum(Vector3.zero, 19, visual(targetPower), 0, 1);
			Gizmos.color = new Color(1, 0, 0);
			Gizmos.DrawFrustum(Vector3.zero, 19, visual(currentPower), 0, 1);

			// Maintain mirrored thruster game object
			{
				string mirroredGoName = "mirrored thruster for: " + this.gameObject.name;
				ThrusterObject mirrored = null;
				for (int i = 0; i < this.transform.parent.childCount; ++i)
				{
					if (this.transform.parent.GetChild(i).name != mirroredGoName) continue;
					mirrored = this.transform.parent.GetChild(i).GetComponent<ThrusterObject>();
					if (mirrored) break;
				}

				if (createMirroredThruster)
				{
					if (!mirrored)
					{						
						var g = GameObject.Instantiate(this.gameObject);
						g.name = mirroredGoName;
						g.hideFlags = HideFlags.None;
						g.transform.parent = this.transform.parent;
						mirrored = g.GetComponent<ThrusterObject>();
						mirrored.createMirroredThruster = false;
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
					DestroyImmediate(mirrored.gameObject);
				}
			}
		}
	}

	private void FixedUpdate()
	{
		//currentPower = Mathf.Lerp(currentPower, targetPower, Time.fixedDeltaTime * 30.0f);
		currentPower = targetPower;

		if (targetPower > 0)
		{
			var worldDirection = ShipRoot.TransformDirection(shipRoot_direction);
			rb.AddForceAtPosition(
				-worldDirection * currentPower,
				transform.position,
				ForceMode.Force
			);
		}
	}
}
