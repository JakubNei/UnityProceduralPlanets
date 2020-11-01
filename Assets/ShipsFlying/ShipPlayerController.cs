using UnityEngine;
using System.Collections;

public class ShipPlayerController : MonoBehaviour
{

	public ShipControlComputer shipComputer;


	public float C = 0.1f;
	public bool coupledMode = false;

	public float mouseSpeedMultiplier = 4;
	public float moveSpeedMultiplier = 2;
	public float multiplier = 1;

	public Vector3 lastValocity;
	public Vector3 acceleration;

	public Vector3 lastAngularVelocity;
	public Vector3 angularAcceleration;

	void FixedUpdate()
	{
		if (!shipComputer) return;

		acceleration = (shipComputer.CurrentVelocity - lastValocity) / Time.fixedDeltaTime;
		lastValocity = shipComputer.CurrentVelocity;

		angularAcceleration = (shipComputer.CurrentAngularVelocity - lastAngularVelocity) / Time.fixedDeltaTime;
		lastAngularVelocity = shipComputer.CurrentAngularVelocity;
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

		if (Input.GetKey(KeyCode.RightArrow)) targetDir.y += 1;
		if (Input.GetKey(KeyCode.LeftArrow)) targetDir.y -= 1;

		if (Input.GetKey(KeyCode.UpArrow)) targetDir.x += 1;
		if (Input.GetKey(KeyCode.DownArrow)) targetDir.x -= 1;

		if (Input.GetKey(KeyCode.Q)) targetDir.z += 1;
		if (Input.GetKey(KeyCode.E)) targetDir.z -= 1;


		var targetMove = new Vector3();

		if (Input.GetKey(KeyCode.A)) targetMove.x -= 1;
		if (Input.GetKey(KeyCode.D)) targetMove.x += 1;

		if (Input.GetKey(KeyCode.W)) targetMove.z += 1;
		if (Input.GetKey(KeyCode.S)) targetMove.z -= 1;

		if (Input.GetKey(KeyCode.LeftControl)) targetMove.y -= 1;
		if (Input.GetKey(KeyCode.Space)) targetMove.y += 1;

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

            targetMove = (targetPositionError + acceleration) * C;
            targetDir = (targetRotationError + angularAcceleration) * C;
        }

		if (Input.GetKey(KeyCode.F)) targetMove = -shipComputer.CurrentVelocity ;
		if (Input.GetKey(KeyCode.R)) targetDir = -shipComputer.CurrentAngularVelocity;
		
		targetMove *= multiplier;
		targetDir *= multiplier;

		float mass = shipComputer.ShipMass;
		targetMove *= mass;
		targetDir *= mass;

		shipComputer.SetTarget(targetMove, targetDir);
	}

	void OnGUI()
	{
		GUILayout.Label("Coupled Mode: " + (coupledMode ? "on" : "off"));
	}
}
