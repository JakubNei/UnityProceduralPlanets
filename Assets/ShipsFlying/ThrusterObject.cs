using UnityEngine;
using System.Collections;
using System.Linq;
using System;
using System.Runtime.InteropServices;

public class ThrusterObject : MonoBehaviour
{

	public Vector3 localOffsetFromCenterOfMass { get; private set; }

	public Vector3 mountedDirection;
	public float MaxPower { get { return maxPower; } }

	[SerializeField]
	float maxPower = 5;

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

	Rigidbody rb;
	// Use this for initialization
	public void Initialize(Vector3 centerOfMass)
	{
		rb = this.transform.root.GetComponentInChildren<Rigidbody>();

		localOffsetFromCenterOfMass = this.transform.localPosition - centerOfMass;
		mountedDirection = (this.transform.localRotation * Vector3.forward).normalized;

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

	public void SetThrust(double targetForce)
	{
		if (targetForce < 0) targetForce = 0;
		if (targetForce > MaxPower) targetForce = MaxPower;
		this.targetPower = (float)targetForce;
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
		mountedDirection = (transform.localRotation * Vector3.forward).normalized;
		if (transform.parent)
		{
			const float c = 0.3f;
			Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
			Gizmos.color = Color.blue;
			Gizmos.DrawFrustum(Vector3.zero, 20, maxPower * c, 0, 1);
			Gizmos.color = Color.red;
			Gizmos.DrawFrustum(Vector3.zero, 19, targetPower * c, 0, 1);

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
						var p = this.transform.root.InverseTransformPoint(this.transform.position);
						mirrored.transform.position = this.transform.root.TransformPoint(new Vector3(-p.x, p.y, p.z));

						var r = this.transform.root.InverseTransformDirection(this.transform.forward);
						mirrored.transform.forward =  this.transform.root.TransformDirection(new Vector3(-r.x, r.y, r.z));
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
			var worldDir = this.transform.root.TransformDirection(mountedDirection);
			if (worldDir.sqrMagnitude > 0) this.transform.rotation = Quaternion.LookRotation(worldDir);

			rb.AddForceAtPosition(
				-worldDir * targetPower,
				transform.position,
				ForceMode.Force
			);
		}
	}
}
