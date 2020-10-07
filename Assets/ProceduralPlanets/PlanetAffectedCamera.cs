using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class PlanetAffectedCamera : MonoBehaviour
{
	public int targetFrameRate = 60;

	public float velocityChangeSpeed = 10.0f;
	public float mouseSensitivty = 100f;

	public bool speedBasedOnDistanceToPlanet = true;

	public float distanceToClosestPlanet;
	public float cameraSpeedModifier = 10.0f;

	public Vector3 currentVelocity;

	public bool walkOnPlanet;
	public Vector3 walkOnPlanet_lastUp;
	public Vector3 walkOnPlanet_lastForward;
	public bool walkOnPlanet_isFirstRun;
	public bool walkOnPlanet_clampUpDownRotation = true;
	public bool walkOnPlanet_applyGravity = true;

	Camera cam { get { return GetComponent<Camera>(); } }


	Planet GetClosestPlanet(Vector3 pos)
	{
		Planet closest = null;
		float closestDist = float.MaxValue;
		foreach (var p in Planet.allPlanets)
		{
			var d = Vector3.Distance(p.Center, pos) - p.planetConfig.radiusStart;
			if (d < closestDist)
			{
				closestDist = d;
				closest = p;
			}
		}
		return closest;
	}

	void Start()
	{
		physicMaterial = new PhysicMaterial()
		{
			frictionCombine = PhysicMaterialCombine.Minimum,
			bounceCombine = PhysicMaterialCombine.Minimum,
		};
		foreach (var c in GetComponentsInChildren<Collider>())
			c.sharedMaterial = physicMaterial;

		MoveToClosestPlanetSurface();

		UpdatePosition(0.1f); // spool up
	}

	public Vector3 savedPosition1;
	public Quaternion savedRotation1;

	public Vector3 savedPosition2;
	public Quaternion savedRotation2;

	public PhysicMaterial physicMaterial;


	private void Update()
	{
		Application.targetFrameRate = targetFrameRate;
	}

	private void FixedUpdate()
	{
		UpdatePosition(Time.fixedDeltaTime);
		ApplyGravity();
	}

	void MoveToClosestPlanetSurface()
	{	
		var planet = GetClosestPlanet(transform.position);
		if (planet != null)
		{
			SetPosRot(
				new Vector3(planet.planetConfig.radiusStart * 1.1f, 0, 0) + planet.Center,
				Quaternion.LookRotation(planet.Center - transform.position)
			);

		}
	}

	void UpdatePosition(float deltaTime)
	{
		var rotation = transform.rotation;
		var position = transform.position;
		var planet = GetClosestPlanet(position);
		var rb = GetComponent<Rigidbody>();

		if (rb)
		{
			rotation = rb.rotation;
			position = rb.position;
		}

		if (Input.GetKeyDown(KeyCode.F5))
		{
			savedPosition1 = position;
			savedRotation1 = rotation;
		}
		if (Input.GetKeyDown(KeyCode.F6))
		{
			position = transform.position = savedPosition1;
			rotation = transform.rotation = savedRotation1;
		}

		if (Input.GetKeyDown(KeyCode.F7))
		{
			savedPosition2 = position;
			savedRotation2 = rotation;
		}
		if (Input.GetKeyDown(KeyCode.F8))
		{
			position = transform.position = savedPosition2;
			rotation = transform.rotation = savedRotation2;
		}


		if (Input.GetKeyDown(KeyCode.G))
		{
			walkOnPlanet = !walkOnPlanet;
		}

		if (Input.GetKeyDown(KeyCode.T))
		{
			MoveToClosestPlanetSurface();
		}


		var mouseDelta = Vector2.zero;
		if (!Cursor.visible)
			mouseDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

		var scrollWheelDelta = Input.mouseScrollDelta.y;

		if (Input.GetKeyDown(KeyCode.Escape))
		{
			/*if (Scene.Engine.WindowState == WindowState.Fullscreen)
			{
				Scene.Engine.WindowState = WindowState.Normal;
			}
			else*/
			if (!Cursor.visible)
			{
				Cursor.lockState = CursorLockMode.None;
				Cursor.visible = true;
			}
			else
			{
				Application.Quit();
			}
		}

		if (Input.GetMouseButtonDown(0))
		{
			if (Cursor.visible)
			{
				Cursor.lockState = CursorLockMode.Locked;
				Cursor.visible = false;
			}
		}

		if (Input.GetKeyDown(KeyCode.F))
		{
			RaycastHit hit;
			if (Physics.Raycast(this.transform.position + this.transform.forward * 5, this.transform.forward, out hit, 100000.0f))
			{
				BigPosition bigPosition = hit.point + FloatingOriginController.Instance.SceneCenterIsAt;
				planet.AddCrater(bigPosition, Random.Range(hit.distance * 0.3f, hit.distance * 0.5f));
			}
		}

		if (planet != null)
		{
			RaycastHit hit;
			if (Physics.Raycast(position, planet.Center - position, out hit))
			{
				distanceToClosestPlanet = Vector3.Distance(hit.point, position);
			}
		}

		//if (Cursor.lockState)
		{

			if (scrollWheelDelta > 0) cameraSpeedModifier *= 1.3f;
			if (scrollWheelDelta < 0) cameraSpeedModifier /= 1.3f;
			cameraSpeedModifier = Mathf.Clamp(cameraSpeedModifier, 1, 100000);

			float currentSpeed = cameraSpeedModifier;

			if (!walkOnPlanet && speedBasedOnDistanceToPlanet)
			{
				var s = Mathf.Clamp(distanceToClosestPlanet, 1, 30000);
				var planetSpeedModifier = (1 + (float)s / 5.0f);
				currentSpeed *= planetSpeedModifier;
			}

			if (Input.GetKey(KeyCode.LeftShift)) currentSpeed *= 5;

			var targetForce = Vector3.zero;
			targetForce += Vector3.forward * Input.GetAxis("Vertical");
			targetForce += Vector3.right * Input.GetAxis("Horizontal");
			if (Input.GetKey(KeyCode.Space)) targetForce += Vector3.up;
			if (Input.GetKey(KeyCode.LeftControl)) targetForce -= Vector3.up;

			if (walkOnPlanet && distanceToClosestPlanet > 2.5f)
				currentSpeed *= 0.1f;

			targetForce = targetForce.normalized * currentSpeed;

			float pitchDelta = 0;
			float yawDelta = 0;
			float rollDelta = 0;

			float c = mouseSensitivty * (float)deltaTime;
			yawDelta += mouseDelta.x * c;
			pitchDelta -= mouseDelta.y * c;
			if (Input.GetKey(KeyCode.Q)) rollDelta += c;
			if (Input.GetKey(KeyCode.E)) rollDelta -= c;


			if (planet != null && Input.GetKeyDown(KeyCode.C))
			{
				rotation = Quaternion.LookRotation(planet.Center - position);
			}

			if (planet != null && walkOnPlanet)
			{
				var up = (position - planet.Center).normalized;
				var forward = walkOnPlanet_lastForward;

				if (walkOnPlanet_isFirstRun)
				{
					walkOnPlanet_lastUp = up;

					var pointOnPlanet = position - planet.Center;
					forward = rotation * Vector3.forward;
				}
				else
				{
					var upDeltaAngle = Vector3.Angle(up, walkOnPlanet_lastUp);
					var upDeltaRot = Quaternion.Inverse(Quaternion.AngleAxis(upDeltaAngle, Vector3.Cross(up, walkOnPlanet_lastUp)));

					forward = upDeltaRot * forward;
				}


				var left = Vector3.Cross(up, forward);

				var rotDelta = Quaternion.identity;

				rotDelta = rotDelta * Quaternion.AngleAxis(pitchDelta, left);
				forward = Quaternion.AngleAxis(pitchDelta, left) * forward;

				if (walkOnPlanet_clampUpDownRotation)
				{
					// clamping up down rotation
					var maxUpDownAngle = 80;
					var minUp = 90 - maxUpDownAngle;
					var maxDown = 90 + maxUpDownAngle;
					var angle = Vector3.Angle(forward, up);
					if (angle < minUp)
						forward = Quaternion.AngleAxis(minUp, left) * up;
					else if (angle > maxDown)
						forward = Quaternion.AngleAxis(maxDown, left) * up;
				}

				rotDelta = rotDelta * Quaternion.AngleAxis(yawDelta, up);
				forward = Quaternion.AngleAxis(yawDelta, up) * forward;

				forward.Normalize();

				rotation = Quaternion.LookRotation(forward, up);

				walkOnPlanet_lastForward = forward;
				walkOnPlanet_lastUp = up;
				walkOnPlanet_isFirstRun = false;


			}
			else
			{
				var rotDelta =
					Quaternion.AngleAxis(pitchDelta, new Vector3(1, 0, 0)) *
					Quaternion.AngleAxis(yawDelta, new Vector3(0, 1, 0)) *
					Quaternion.AngleAxis(rollDelta, new Vector3(0, 0, 1));

				rotation = rotation * rotDelta;

				walkOnPlanet_isFirstRun = true;
			}


			targetForce = rotation * targetForce;

			if (rb)
			{
				// change fricton based on whether we want to move or not
				var targetFriction = 1;
				if (targetForce.sqrMagnitude > 0)
					targetFriction = 0;
				physicMaterial.dynamicFriction = targetFriction;
				physicMaterial.staticFriction = targetFriction;

				rb.AddForce(targetForce * deltaTime, ForceMode.VelocityChange);
				rb.MoveRotation(rotation);

				//rb.drag = Mathf.InverseLerp(100, 10, distanceToClosestPlanet); // change "air density" based on distance to planet
			}
			else // UNTESTED
			{
				currentVelocity += targetForce * velocityChangeSpeed * (float)deltaTime;
				currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, (float)deltaTime);
				position += currentVelocity * (float)deltaTime;
				SetPosRot(position, rotation);
			}



			// light toggle
			if (Input.GetKeyDown(KeyCode.L))
			{
				foreach (var l in GetComponentsInChildren<Light>())
					l.enabled = !l.enabled;
			}
		}

	}


	void SetPosRot(Vector3 pos, Quaternion rot)
	{
		var rb = GetComponent<Rigidbody>();
		transform.position = pos;
		transform.rotation = rot;
		if (rb)
		{
			rb.position = pos;
			rb.rotation = rot;
		}

	}


	void ApplyGravity()
	{
		var pos = transform.position;
		var rb = GetComponent<Rigidbody>();
		var planet = GetClosestPlanet(pos);

		// make cam on top of the planet
		if (planet != null && rb && walkOnPlanet && walkOnPlanet_applyGravity)
		{
			var gravityDir = (planet.Center - pos).normalized;
			rb.AddForce(gravityDir * 9.81f * Time.fixedDeltaTime, ForceMode.VelocityChange);
		}
	}
}
