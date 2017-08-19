using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetAffectedCamera : MonoBehaviour
{

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
	public bool walkOnPlanet_clampToPlanetSurface = true;

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

		var planet = GetClosestPlanet(transform.position);
		if (planet != null)
		{
			transform.LookAt(planet.transform.position);
			transform.position = new Vector3(-planet.planetConfig.radiusStart * 2, 0, 0) + planet.Center;
		}

		Update(0.1f); // spool up
	}

	public Vector3 savedPosition1;
	public Quaternion savedRotation1;

	public Vector3 savedPosition2;
	public Quaternion savedRotation2;


	private void Update()
	{
		Update(Time.deltaTime);
	}
	void Update(float deltaTime)
	{
		if (deltaTime > 1 / 30f) deltaTime = 1 / 30f;

		var rotation = transform.rotation;
		var position = transform.position;


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


		var planet = GetClosestPlanet(position);



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
			else*/ if (!Cursor.visible)
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


		float planetSpeedModifier = 1;

		if (planet != null)
		{
			RaycastHit hit;
			if (Physics.Raycast(position, planet.Center - position, out hit))
			{
				distanceToClosestPlanet = Vector3.Distance(hit.point, position);
				if (speedBasedOnDistanceToPlanet)
				{
					var s = Mathf.Clamp(distanceToClosestPlanet, 1, 30000);
					planetSpeedModifier = (1 + (float)s / 5.0f);
				}

			}
		}

		//if (Cursor.lockState)
		{

			if (scrollWheelDelta > 0) cameraSpeedModifier *= 1.3f;
			if (scrollWheelDelta < 0) cameraSpeedModifier /= 1.3f;
			cameraSpeedModifier = Mathf.Clamp(cameraSpeedModifier, 1, 100000);
			float currentSpeed = cameraSpeedModifier * planetSpeedModifier;



			if (Input.GetKey(KeyCode.LeftShift)) currentSpeed *= 5;


			var targetForce = Vector3.zero;
			targetForce += currentSpeed * Vector3.forward * Input.GetAxis("Vertical");
			targetForce += currentSpeed * Vector3.right * Input.GetAxis("Horizontal");
			if (Input.GetKey(KeyCode.Space)) targetForce += currentSpeed * Vector3.up;
			if (Input.GetKey(KeyCode.LeftControl)) targetForce -= currentSpeed * Vector3.up;

			//var pos = Matrix4.CreateTranslation(targetVelocity);


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

				var rotDelta =
					Quaternion.AngleAxis(yawDelta, up) *
					Quaternion.AngleAxis(pitchDelta, left);


				forward = rotDelta * forward;

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


			var rb = GetComponent<Rigidbody>();


			targetForce = rotation * targetForce;


			if (rb)
			{
				rb.AddForce(targetForce, ForceMode.Force);
				rb.MoveRotation(rotation);

				// make cam on top of the planet
				if (planet != null && walkOnPlanet && walkOnPlanet_clampToPlanetSurface)
				{
					var gravityDir = (planet.Center - position).normalized;
					rb.AddForce(gravityDir * 10, ForceMode.Force);
				}

			}
			/*else // UNTESTED
			{
				currentVelocity += targetForce * velocityChangeSpeed * (float)deltaTime;
				currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, (float)deltaTime);
				position += currentVelocity * (float)deltaTime;
				transform.rotation = rotation;
				transform.position = position; // += Entity.transform.position.Towards(position).ToVector3d() * deltaTime * 10;
			}*/
		}

	}
}
