using UnityEngine;
using System.Collections;

public class ShipPlayerController : MonoBehaviour
{

	public ShipControlComputer shipComputer;

	public bool coupledMode = false;

	public float multiplier = 1;

	void FixedUpdate()
	{
		if (!shipComputer) return;
	}

	public Vector3 targetVel;
	public Vector3 targetAngularVel;

	public Vector3 targetPositionError;
	public Vector3 targetRotationError;

	// Update is called once per frame
	void Update()
	{
		if (!shipComputer) return;


		Camera.main.transform.position = shipComputer.transform.position + shipComputer.transform.rotation * new Vector3(0, 10, -20);
		Camera.main.transform.rotation = shipComputer.transform.rotation;


		if (Input.mouseScrollDelta.y > 0) multiplier++;
		else if (Input.mouseScrollDelta.y < 0) multiplier--;
		multiplier = Mathf.Clamp(multiplier, 0, 100);


		var targetDir = new Vector3();

		if (Input.GetKey(KeyCode.RightArrow)) targetDir.y += multiplier;
		if (Input.GetKey(KeyCode.LeftArrow)) targetDir.y -= multiplier;

		if (Input.GetKey(KeyCode.UpArrow)) targetDir.x += multiplier;
		if (Input.GetKey(KeyCode.DownArrow)) targetDir.x -= multiplier;

		if (Input.GetKey(KeyCode.Q)) targetDir.z += multiplier;
		if (Input.GetKey(KeyCode.E)) targetDir.z -= multiplier;


		var targetMove = new Vector3();

		if (Input.GetKey(KeyCode.A)) targetMove.x -= multiplier;
		if (Input.GetKey(KeyCode.D)) targetMove.x += multiplier;

		if (Input.GetKey(KeyCode.W)) targetMove.z += multiplier * 5;
		if (Input.GetKey(KeyCode.S)) targetMove.z -= multiplier * 5;

		if (Input.GetKey(KeyCode.LeftControl)) targetMove.y -= multiplier;
		if (Input.GetKey(KeyCode.Space)) targetMove.y += multiplier;

		if (Input.GetKeyDown(KeyCode.C))
			coupledMode = !coupledMode;



		if (coupledMode)
		{
			targetPositionError -= shipComputer.CurrentVelocity * Time.deltaTime;
			targetRotationError -= shipComputer.CurrentAngularVelocity * Time.deltaTime;

			// var timeToRequiredToStop = currentSpeed / accelerationSpeed;
			// var distanceRequiredToStop = currentSpeed * timeToRequiredToStop - accelerationSpeed * 1 / 2 * timeToRequiredToStop * timeToRequiredToStop;

			var accelerationSpeed = 5;
			var timeToRequiredToStop = shipComputer.CurrentVelocity / accelerationSpeed;
			//var distanceRequiredToStop = controllerComputer.CurrentVelocity * timeToRequiredToStop - accelerationSpeed * 1 / 2 * timeToRequiredToStop * timeToRequiredToStop;		

			const float C = 0.1f;
			//targetMove = (targetPositionError + acceleration) * C;
			//targetDir = (targetRotationError + angularAcceleration) * C;
		}

		if (Input.GetKey(KeyCode.F)) targetMove = -shipComputer.CurrentVelocity;
		if (Input.GetKey(KeyCode.R)) targetDir = -shipComputer.CurrentAngularVelocity;


		float mass = shipComputer.ShipMass;
		targetMove *= mass;
		targetDir *= mass;

		shipComputer.SetTarget(targetMove, targetDir);
	}

	void OnGUI()
	{
		GUILayout.Label("Ship status");
		GUILayout.Label("Velocity linear: " + shipComputer.CurrentVelocity.magnitude);
		GUILayout.Label("Velocity angular: " + shipComputer.CurrentAngularVelocity.magnitude);
		GUILayout.Label("G Force: " + shipComputer.CurrentForce.magnitude);
		GUILayout.Label("");
		GUILayout.Label("Input");
		GUILayout.Label("Input multiplier: " + multiplier);
		GUILayout.Label("Coupled Mode: " + (coupledMode ? "on" : "off"));
	}
}
