using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UIElements;

public class ShipPlayerController : MonoBehaviour
{

	public ShipControlComputer shipComputer;

	public bool coupledMode = false;

	public float multiplier = 80;

	void FixedUpdate()
	{
		if (!shipComputer) return;
	}



	Vector3 cameraRotation;
	float cameraRotationResetIn;

	// Update is called once per frame
	void Update()
	{
		if (!shipComputer) return;

		if (Input.mouseScrollDelta.y > 0) multiplier++;
		else if (Input.mouseScrollDelta.y < 0) multiplier--;
		multiplier = Mathf.Clamp(multiplier, 0, 100);

		var targetDir = new Vector3();		
		var targetMove = new Vector3();
		
		
		if (coupledMode)
		{
			targetMove -= shipComputer.CurrentVelocity * multiplier;;
			targetDir -= shipComputer.CurrentAngularVelocity * multiplier;;

			//targetMove -= shipComputer.CurrentVelocity.normalized * (targetMove.magnitude > 0 ? targetMove.magnitude : multiplier) *
			//	Math.Max(0, 1 - Vector3.Dot(targetMove.normalized, shipComputer.CurrentVelocity.normalized));
				
			//targetDir -= shipComputer.CurrentAngularVelocity.normalized * (targetDir.magnitude > 0 ? targetDir.magnitude : multiplier) *
			//	Math.Max(0, 1 - Vector3.Dot(targetDir.normalized, shipComputer.CurrentAngularVelocity.normalized));
		}

		if (Input.GetKey(KeyCode.F)) targetMove -= shipComputer.CurrentVelocity * multiplier;
		if (Input.GetKey(KeyCode.R)) targetDir -= shipComputer.CurrentAngularVelocity * multiplier;



		// User input should override automated adjustments

		float dirMultiplier = multiplier;

		if (Input.GetKey(KeyCode.RightArrow)) targetDir.y = +1 * dirMultiplier;
		if (Input.GetKey(KeyCode.LeftArrow)) targetDir.y = -1 * dirMultiplier;

		if (Input.GetKey(KeyCode.UpArrow)) targetDir.x = +1 * dirMultiplier;
		if (Input.GetKey(KeyCode.DownArrow)) targetDir.x = -1 * dirMultiplier;

		if (Input.GetKey(KeyCode.Q)) targetDir.z = +1 * dirMultiplier;
		if (Input.GetKey(KeyCode.E)) targetDir.z = -1 * dirMultiplier;


		if (Input.GetKey(KeyCode.A)) targetMove.x = -multiplier * 0.3f;
		if (Input.GetKey(KeyCode.D)) targetMove.x = +multiplier * 0.3f;

		if (Input.GetKey(KeyCode.W)) targetMove.z = +multiplier;
		if (Input.GetKey(KeyCode.S)) targetMove.z = -multiplier;

		if (Input.GetKey(KeyCode.LeftControl)) targetMove.y = -multiplier * 0.3f;
		if (Input.GetKey(KeyCode.Space)) targetMove.y = +multiplier * 0.3f;

		if (Input.GetKeyDown(KeyCode.C)) coupledMode = !coupledMode;

		float mass = shipComputer.ShipMass;
		targetMove *= mass;
		targetDir *= mass;

		shipComputer.SetTargetForces(targetMove, targetDir);


		
		

		var mouseX = Input.GetAxis("Mouse X");
		var mouseY = Input.GetAxis("Mouse Y");

		if (mouseX == 0 && mouseY == 0)
		{
			cameraRotationResetIn -= Time.deltaTime;
		}
		else
		{
			cameraRotationResetIn = 2;
			var r = 100 * Time.deltaTime;
			cameraRotation.y += mouseX * r;
			cameraRotation.x += -mouseY * r;
		}

		if (Input.GetKey(KeyCode.LeftAlt))
		{
			cameraRotationResetIn = 0;
		}

		if (cameraRotationResetIn <= 0) 
		{
			cameraRotation = Vector3.Slerp(cameraRotation, Vector3.zero, 10 * Time.deltaTime);
		}

		var camera = FloatingOriginCamera.main;
		camera.Rotation = shipComputer.transform.rotation * Quaternion.Euler(cameraRotation);
		
		cameraDistance = Mathf.Lerp(cameraDistance, shipComputer.CurrentForce.magnitude / 50, Time.deltaTime * 0.1f);
		cameraDistance = Mathf.Clamp(cameraDistance, 1, 2);
		camera.VisualPosition = shipComputer.transform.position + camera.Rotation * (Vector3.up * 5 + Vector3.back * 20) * cameraDistance;
	}

	float cameraDistance = 1;

	void OnGUI()
	{
		GUILayout.Label("Ship status");
		GUILayout.Label("Velocity linear: " + shipComputer.CurrentVelocity.magnitude);
		GUILayout.Label("Velocity angular: " + shipComputer.CurrentAngularVelocity.magnitude);
		GUILayout.Label("Actual thrust: " + shipComputer.ActualThrustersForce.magnitude);
		GUILayout.Label("Actual thrust / ship mass: " + shipComputer.ActualThrustersForce.magnitude / shipComputer.ShipMass);
		GUILayout.Label("Gravity (external forces): " + shipComputer.ExternalForce.magnitude);
		GUILayout.Label("G: " + shipComputer.CurrentForce.magnitude.ToString("0.0"));

		GUILayout.Label("");
		GUILayout.Label("Input");
		GUILayout.Label("Input multiplier: " + multiplier);
		GUILayout.Label("Coupled Mode: " + (coupledMode ? "on" : "off"));
	}
}
