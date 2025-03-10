using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class HummingbirdAgent : Agent
{
    public float moveForce = 2f;
    public float pitchSpeed = 100f;
    public float yawSpeed = 100f;
    public Transform beakTip;
    public Camera agentCamera;
    public bool trainingMode;


    private Rigidbody rigidBody;
    private FlowerArea flowerArea;
    private Flower nearestFlower;
    private float smoothPitchChange = 0f;
    private float smoothYawChange = 0f;
    private const float MaxPitchAngle = 80f;
    private const float BeakTipRadius = 0.008f;
    private bool frozen = false;


    public float NectarObtained { get; private set; }


    public override void Initialize()
    {
        rigidBody = GetComponent<Rigidbody>();
        flowerArea = GetComponentInParent<FlowerArea>();

        if (!trainingMode) MaxStep = 0;
    }


    public override void OnEpisodeBegin()
    {
        // reset flowers
        if (trainingMode) { 
            flowerArea.ResetFlowers();
        }

        // reset collected nectar
        NectarObtained = 0;

        // reset velocity
        rigidBody.linearVelocity = Vector3.zero;
        rigidBody.angularVelocity = Vector3.zero;

        // default to spawning in front of the flower
        bool inFrontOfFlower = true;
        if (trainingMode) {
            // spawn in front of flower 50% of the time during trainig
            inFrontOfFlower = UnityEngine.Random.value > 0.5f;
        }

        MoveToSafeRandomPosition(inFrontOfFlower);

        UpdateNearestFlower();
    }


    /// <summary>
    /// Called when an action is received from either the player or the neural network
    /// 
    /// actions represent:
    /// index 0: move vector x (+1 = right, -1 = left)
    /// index 1: move vector y (+1 = up, -1 = down)
    /// index 2: move vector z (+1 = forward, -1 = back)
    /// index 3: pitch angle (+1 = pitch up, -1 = pitch down)
    /// index 4: yaw angle (+1 = turn right, -1 = turn left)
    /// </summary>
    /// <param name="actions">The actions to take</param>
    public override void OnActionReceived(ActionBuffers actions)
    {
        if (frozen) return;

        // move
        Vector3 move = new(actions.ContinuousActions[0], actions.ContinuousActions[1], actions.ContinuousActions[2]);
        rigidBody.AddForce(move * moveForce);

        // rotate
        Vector3 rotationVector = transform.rotation.eulerAngles;
        float pitchChange = actions.ContinuousActions[3];
        float yawChange = actions.ContinuousActions[4];

        smoothPitchChange = Mathf.MoveTowards(smoothPitchChange, pitchChange, 2f * Time.fixedDeltaTime);
        smoothYawChange = Mathf.MoveTowards(smoothYawChange, yawChange, 2f * Time.fixedDeltaTime);

        float pitch = rotationVector.x + smoothPitchChange * Time.fixedDeltaTime * pitchSpeed;
        if (pitch > 180f) pitch -= 360;
        pitch = Mathf.Clamp(pitch, -MaxPitchAngle, MaxPitchAngle);

        float yaw = rotationVector.y + smoothYawChange * Time.fixedDeltaTime * yawSpeed;

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }


    /// <summary>
    /// Collect vector observations from the environment
    /// </summary>
    /// <param name="sensor">The vector sensor</param>
    public override void CollectObservations(VectorSensor sensor)
    {
        // if the nearest flower is null, observe an empty array and return early
        if (nearestFlower == null) {
            sensor.AddObservation(new float[10]);
            return;
        }

        // Observe the agent's current rotation (4 observations)
        sensor.AddObservation(transform.rotation.normalized);

        // Observe a normalized vector pointing to the nearest flower (3 observations)
        Vector3 toFlower = nearestFlower.FlowerCenterPosition - beakTip.position;
        sensor.AddObservation(toFlower.normalized);

        // Observe a dot product that indicates whether the beak tip is in front of the flower (1 observation)
        // (+1 means tha the beak tip is directly in front of the flower, -1 means directly behind)
        sensor.AddObservation(Vector3.Dot(toFlower.normalized, -nearestFlower.FlowerUpVector.normalized));

        // Observe a dot product that indicates whether the beak is pointing toward the flower (1 observation)
        // (+1 means the beak is pointing directly at the flower, -1 means directly away)
        sensor.AddObservation(Vector3.Dot(beakTip.forward.normalized, -nearestFlower.FlowerUpVector.normalized));

        // Observe the relative distance from the beak tip to the flower (1 observation)
        sensor.AddObservation(toFlower.magnitude / FlowerArea.AreaDiameter);

        // 10 total observations
    }


    /// <summary>
    /// When behavior type is set to "Heuristic only" on the agent's behavior parameters,
    /// this function will be called. Its return will be fed into
    /// <see cref="OnActionReceived(ActionBuffers)"/> instead of using the neural network
    /// </summary>
    /// <param name="actionsOut">An output action array</param>
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // placeholders for all movement/rotation
        Vector3 forward = Vector3.zero;
        Vector3 left = Vector3.zero;
        Vector3 up = Vector3.zero;
        float pitch = 0f;
        float yaw = 0f;

        // convert keyboard inputs to movement and rotation
        // all values must be between -1 and +1

        // forward/backward
        if (Input.GetKey(KeyCode.W)) forward = transform.forward;
        else if (Input.GetKey(KeyCode.S)) forward = -transform.forward;

        // left/right
        if (Input.GetKey(KeyCode.A)) left = -transform.right;
        else if (Input.GetKey(KeyCode.D)) left = transform.right;

        // up/down
        if (Input.GetKey(KeyCode.E)) up = transform.up;
        else if (Input.GetKey(KeyCode.C)) up = -transform.up;

        // pitch up/down
        if (Input.GetKey(KeyCode.UpArrow)) pitch = 1f;
        else if (Input.GetKey(KeyCode.DownArrow)) pitch = -1f;

        // yaw left/right
        if (Input.GetKey(KeyCode.LeftArrow)) yaw = -1f;
        else if (Input.GetKey(KeyCode.RightArrow)) yaw = 1f;

        // combine the movement vectors and normalize
        Vector3 combined = (forward + up + left).normalized;

        // add values to the actionsout array
        var continuousActionsOut = actionsOut.ContinuousActions;
        continuousActionsOut[0] = combined.x;
        continuousActionsOut[1] = combined.y;
        continuousActionsOut[2] = combined.z;
        continuousActionsOut[3] = pitch;
        continuousActionsOut[4] = yaw;
    }


    /// <summary>
    /// Prevent the agent from moving and taking actions
    /// </summary>
    public void FreezeAgent()
    {
        Debug.Assert(trainingMode == false, "Freeze/Unfreeze not supported in training");

        frozen = true;
        rigidBody.Sleep();
    }


    /// <summary>
    /// Resume agent movement and actions
    /// </summary>
    public void UnfreezeAgent()
    {
        Debug.Assert(trainingMode == false, "Freeze/Unfreeze not supported in training");

        frozen = false;
        rigidBody.WakeUp();
    }


    private void MoveToSafeRandomPosition(bool inFrontOfFlower)
    {
        bool safePositionFound = false;
        int attemptsRemaining = 100;
        Vector3 potentialPosition = new();
        Quaternion potentianRotation = new();

        while (!safePositionFound && attemptsRemaining > 0) { 
            attemptsRemaining--;
            if (inFrontOfFlower) {
                Flower randomFlower = flowerArea.Flowers[UnityEngine.Random.Range(0, flowerArea.Flowers.Count)];

                float distanceFromFlower = UnityEngine.Random.Range(0.1f, 0.2f);

                potentialPosition = randomFlower.transform.position + randomFlower.FlowerUpVector * distanceFromFlower;

                Vector3 toFlower = randomFlower.FlowerCenterPosition - potentialPosition;
                potentianRotation = Quaternion.LookRotation(toFlower, Vector3.up);
            } else {
                float height = UnityEngine.Random.Range(1.2f, 2.5f);

                float radius = UnityEngine.Random.Range(2f, 7f);

                Quaternion direction = Quaternion.Euler(0, UnityEngine.Random.Range(-180f, 180f), 0);

                potentialPosition = flowerArea.transform.position + Vector3.up * height + direction * Vector3.forward * radius;

                float pitch = UnityEngine.Random.Range(-60f, 60f);
                float yaw = UnityEngine.Random.Range(-180f, 180f);
                potentianRotation = Quaternion.Euler(pitch, yaw, 0f);
            }

            Collider[] colliders = Physics.OverlapSphere(potentialPosition, 0.05f);

            safePositionFound = colliders.Length == 0;
        }

        Debug.Assert(safePositionFound, "Could not find a safe position to spawn");

        transform.SetPositionAndRotation(potentialPosition, potentianRotation);
    }


    private void UpdateNearestFlower()
    {
        foreach (Flower flower in flowerArea.Flowers)
        {
            if (nearestFlower == null && flower.HasNectar) { 
                nearestFlower = flower;
            } else if (flower.HasNectar) {
                float distanceToFlower = Vector3.Distance(flower.transform.position, beakTip.position);
                float distanceToCurrentNearestFlower = Vector3.Distance(nearestFlower.transform.position, beakTip.position);

                if (!nearestFlower.HasNectar || distanceToFlower < distanceToCurrentNearestFlower) {
                    nearestFlower = flower;
                }
            }
        }
    }


    /// <summary>
    /// Called when the agent's collider enters a trigger collider
    /// </summary>
    /// <param name="other">The trigger collider</param>
    private void OnTriggerEnter(Collider other)
    {
        TriggerEnterOrStay(other);
    }


    /// <summary>
    /// Called when the agent's collider stays inside a trigger collider
    /// </summary>
    /// <param name="other">The trigger collider</param>
    private void OnTriggerStay(Collider other)
    {
        TriggerEnterOrStay(other);
    }


    /// <summary>
    /// Handles when the agent's collider enters or stays in a trigger collider
    /// </summary>
    /// <param name="collider">The trigger collider</param>
    private void TriggerEnterOrStay(Collider collider)
    {
        // check if agent is colliding with nectar
        if (collider.CompareTag("nectar")) {
            Vector3 closestPointToBeakTip = collider.ClosestPoint(beakTip.position);

            // Check if the closest collision point is close to the beak tip
            // Note: a collision with anything but the beak tip should not count
            if (Vector3.Distance(beakTip.position, closestPointToBeakTip) < BeakTipRadius) {
                // look up the flower for this nectar flower
                Flower flower = flowerArea.GetFlowerFromNectar(collider);

                // attempt to take .01 nectar
                // Note: this is per fixed timestep, meaning it happens every .02 seconds, or 50x per second
                float nectarReceived = flower.Feed(.01f);

                // keep track of nectar obtained
                NectarObtained += nectarReceived;

                if (trainingMode) { 
                    // calculate reward for getting nectar
                    float bonus = .02f * Mathf.Clamp01(Vector3.Dot(transform.forward.normalized, -nearestFlower.FlowerUpVector.normalized));
                    AddReward(.01f + bonus);
                }

                // if flower is empty, update the nearest flower
                if (!flower.HasNectar) {
                    UpdateNearestFlower();
                }
            }
        }
    }


    /// <summary>
    /// Called when the agent collides with smth solid
    /// </summary>
    /// <param name="collision">the collision info</param>
    private void OnCollisionEnter(Collision collision)
    {
        if (trainingMode && collision.collider.CompareTag("boundary")) {
            // collided with the area boundary, give a negative reward
            AddReward(-.5f);
        }
    }


    /// <summary>
    /// Called every frame
    /// </summary>
    private void Update()
    {
        // draw a line from he beak tip to the nearest flower
        if (nearestFlower != null)
            Debug.DrawLine(beakTip.position, nearestFlower.FlowerCenterPosition, Color.green);
    }


    /// <summary>
    /// Called every .02 seconds
    /// </summary>
    private void FixedUpdate()
    {
        // avoids scenario where nearest flower nectar is stolen by opponent and not updated
        if (nearestFlower != null && !nearestFlower.HasNectar)
            UpdateNearestFlower();
    }
}
