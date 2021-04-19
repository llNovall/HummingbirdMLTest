using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using UnityEngine;


/// <summary>
/// A ML Agent for hummingbird
/// </summary>
public class HummingbirdAgent : Agent
{
    [Tooltip("Force to apply when moving"), SerializeField]
    private float _moveForce = 2f;

    [Tooltip("Speed to pitch up or down"), SerializeField]
    private float _pitchSpeed = 100f;

    [Tooltip("Speed to rotate around the up axis"), SerializeField]
    private float _yawSpeed = 100f;

    [Tooltip("Transform at the tip of the beak"), SerializeField]
    private Transform _beakTip;

    [Tooltip("The agent's camera"), SerializeField]
    private Camera _agentCamera;

    [Tooltip("Whether this is training mode or gameplay mode"), SerializeField]
    private bool _trainingMode;

    //Ridigbody of the agent
    new private Rigidbody _rigidbody;

    //Current flower area agent is in
    private FlowerArea _flowerArea;

    private Flower _nearestFlower;

    private float _smoothPitchChange = 0f;
    private float _smoothYawChange = 0f;

    private const float MAX_PITCH_ANGLE = 80f;

    private const float BEAK_TIP_RADIUS = 0.008f;

    //Whether the agent is frozen (intentionally not flying)
    private bool _frozen = false;

    /// <summary>
    /// The amount of nectar the agent has obtained this episode
    /// </summary>
    public float NectarObtained { get; private set; }


    /// <summary>
    /// Initialize the agent
    /// </summary>
    public override void Initialize()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _flowerArea = GetComponentInParent<FlowerArea>();

        if (!_trainingMode) MaxStep = 0;

    }

    /// <summary>
    /// Reset the agent when an episode begins
    /// </summary>
    public override void OnEpisodeBegin()
    {
        if (_trainingMode)
        {
            _flowerArea.ResetFlowers();
        }

        NectarObtained = 0;

        //Zero out velocities so that movement stops before a new episode begins
        _rigidbody.velocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;

        bool inFrontOfFlower = true;
        if (_trainingMode)
        {
            //Spawn in front of flower 50% of the time during training
            inFrontOfFlower = UnityEngine.Random.value > .5f;
        }

        MoveToSafeRandomPosition(inFrontOfFlower);

        UpdateNearestFlower();
    }

    /// <summary>
    /// Called when an action is received from either the player input or the neural network.
    /// 
    /// vectorAction[i] represents:
    /// Index 0 : move vector x (+1 = right, -1 = left)
    /// Index 1 : move vector y (+1 = up, -1 = down)
    /// Index 2 : move vector z (+1 = forward, -1 = backward)
    /// Index 3 : pitch angle (+1 = pitch up, -1 = pitch down)
    /// Index 4 : yaw angle (+1 = turn right, -1 = turn left)
    /// 
    /// </summary>
    /// <param name="vectorAction">The actions to take</param>
    public override void OnActionReceived(float[] vectorAction)
    {
        if (_frozen) return;

        Vector3 move = new Vector3(vectorAction[0], vectorAction[1], vectorAction[2]);
        _rigidbody.AddForce(move * _moveForce);

        Vector3 rotationVector = transform.rotation.eulerAngles;

        float pitchChange = vectorAction[3];
        float yawChange = vectorAction[4];

        _smoothPitchChange = Mathf.MoveTowards(_smoothPitchChange, pitchChange, 2f * Time.fixedDeltaTime);
        _smoothYawChange = Mathf.MoveTowards(_smoothYawChange, yawChange, 2f * Time.fixedDeltaTime);

        float pitch = rotationVector.x + _smoothPitchChange * Time.fixedDeltaTime * _pitchSpeed;
        if (pitch > 180f) pitch -= 360f;
        pitch = Mathf.Clamp(pitch, -MAX_PITCH_ANGLE, MAX_PITCH_ANGLE);

        float yaw = rotationVector.y + _smoothYawChange * Time.fixedDeltaTime * _yawSpeed;

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    /// <summary>
    /// Collect vector observations from the environment
    /// </summary>
    /// <param name="sensor">The vector sensor</param>
    public override void CollectObservations(VectorSensor sensor)
    {
        if (!_nearestFlower)
        {
            sensor.AddObservation(new float[10]);
            return;
        }

        //Observe the agent's local rotation (4 observations
        sensor.AddObservation(transform.localRotation.normalized);
       
        Vector3 toFlower = _nearestFlower.FlowerCenterPosition - _beakTip.position;
        //Observe a normalized Vector pointing to the nearest flower (3 observations)
        sensor.AddObservation(toFlower.normalized);

        //Observe a dot product that indicates whether the beak tip is in front of the flower (1 Observation)
        // (+1 means that the beak tip is directly in front of the flower, -1 means directly behind)
        sensor.AddObservation(Vector3.Dot(toFlower.normalized, -_nearestFlower.FlowerUpVector.normalized));

        //Observe a dot product that indicates whether the beak is pointing toward the flower (1 Observation)
        //(+1 means that the beak is pointing directly at the flower, -1 means directly away)
        sensor.AddObservation(Vector3.Dot(_beakTip.forward.normalized, -_nearestFlower.FlowerUpVector.normalized));

        //Observe the relative distance from the beak tip to the flower (1 observation)
        sensor.AddObservation(toFlower.magnitude / FlowerArea.AREA_DIAMETER);
    }

    /// <summary>
    /// When behaviour Type is set to "Heuristic Only" on the agent's Behaviour Parameters,
    /// this function will be called. Its return values will be fed into <see cref="OnActionReceived(float[])"/> instead of using the neural network
    /// </summary>
    /// <param name="actionsOut">An output action array</param>
    public override void Heuristic(float[] actionsOut)
    {
        Vector3 forward = Vector3.zero;
        Vector3 left = Vector3.zero;
        Vector3 up = Vector3.zero;
        float pitch = 0f;
        float yaw = 0f;

        if (Input.GetKey(KeyCode.W)) forward = transform.forward;
        else if (Input.GetKey(KeyCode.S)) forward = -transform.forward;

        if (Input.GetKey(KeyCode.A)) left = -transform.right;
        else if (Input.GetKey(KeyCode.D)) left = transform.right;

        if (Input.GetKey(KeyCode.E)) up = -transform.up;
        else if (Input.GetKey(KeyCode.C)) up = -transform.up;

        if (Input.GetKey(KeyCode.UpArrow)) pitch = 1f;
        else if (Input.GetKey(KeyCode.DownArrow)) pitch = -1f;

        if (Input.GetKey(KeyCode.RightArrow)) yaw = 1f;
        else if (Input.GetKey(KeyCode.LeftArrow)) yaw = -1f;


        Vector3 combined = (forward + left + up).normalized;

        actionsOut[0] = combined.x;
        actionsOut[1] = combined.y;
        actionsOut[2] = combined.z;
        actionsOut[3] = pitch;
        actionsOut[4] = yaw;
    }

    /// <summary>
    /// Prevent agent from moving and taking actions
    /// </summary>
    public void FreezeAgent()
    {
        Debug.Assert(_trainingMode == false, "Freeze/Unfreeze not supported in training.");
        _frozen = true;
        _rigidbody.Sleep();
    }


    /// <summary>
    /// Prevent agent from moving and taking actions
    /// </summary>
    public void UnfreezeAgent()
    {
        Debug.Assert(_trainingMode == false, "Freeze/Unfreeze not supported in training.");
        _frozen = false;
        _rigidbody.WakeUp();
    }


    /// <summary>
    /// Update the nearest flower to the agent
    /// </summary>
    private void UpdateNearestFlower()
    {
        foreach (Flower flower in _flowerArea.Flowers)
        {
            if(!_nearestFlower && flower.HasNectar)
            {
                _nearestFlower = flower;
            }else if (flower.HasNectar)
            {
                float distanceToFlower = Vector3.Distance(flower.transform.position, _beakTip.position);
                float distanceToCurrentNearestFlower = Vector3.Distance(_nearestFlower.transform.position, _beakTip.position);

                if(!_nearestFlower.HasNectar || distanceToFlower < distanceToCurrentNearestFlower)
                {
                    _nearestFlower = flower;
                }
            }
        }
    }


    /// <summary>
    /// Move the agent to a safe random position. If in front of flower, also point the beak at the flower
    /// </summary>
    /// <param name="inFrontOfFlower"></param>
    private void MoveToSafeRandomPosition(bool inFrontOfFlower)
    {
        bool safePositionFound = false;

        int attemptsRemaining = 100;

        Vector3 potentialPosition = Vector3.zero;
        Quaternion potentialRotation = new Quaternion();

        while(!safePositionFound && attemptsRemaining > 0)
        {
            attemptsRemaining--;
            if (inFrontOfFlower)
            {
                Flower randomFlower = _flowerArea.Flowers[UnityEngine.Random.Range(0, _flowerArea.Flowers.Count)];

                float distanceFromFlower = UnityEngine.Random.Range(0.1f, 0.2f);

                potentialPosition = randomFlower.transform.position + randomFlower.FlowerUpVector * distanceFromFlower;

                Vector3 toFlower = randomFlower.FlowerCenterPosition - potentialPosition;
                potentialRotation = Quaternion.LookRotation(toFlower, Vector3.up);
            }
            else
            {
                float height = UnityEngine.Random.Range(1.2f, 2.5f);

                float radius = UnityEngine.Random.Range(2f, 7f);

                Quaternion direction = Quaternion.Euler(0f, UnityEngine.Random.Range(-180, 180), 0);

                //Combine height, radius and direction to pick potential position
                potentialPosition = _flowerArea.transform.position + Vector3.up * height + direction * Vector3.forward * radius;

                float pitch = UnityEngine.Random.Range(-60f, 60f);
                float yaw = UnityEngine.Random.Range(-180, 180f);

                potentialRotation = Quaternion.Euler(pitch, yaw, 0f);
            }

            Collider[] colliders = Physics.OverlapSphere(potentialPosition, 0.05f);

            safePositionFound = colliders.Length == 0;
        }

        Debug.Assert(safePositionFound, "Could not find a safe position to spawn.");

        transform.position = potentialPosition;
        transform.rotation = potentialRotation;
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
    /// Called when the agent's collider stays a trigger collider
    /// </summary>
    /// <param name="other">The trigger collider</param>
    private void OnTriggerStay(Collider other)
    {
        TriggerEnterOrStay(other);
    }

    /// <summary>
    /// Handles when the agent's collider enters / stays in a trigger collider
    /// </summary>
    /// <param name="other"></param>
    private void TriggerEnterOrStay(Collider collider)
    {
        if (collider.CompareTag("nectar"))
        {
            Vector3 closestPointToBeakTip = collider.ClosestPoint(_beakTip.position);

            if(Vector3.Distance(_beakTip.position, closestPointToBeakTip) < BEAK_TIP_RADIUS)
            {
                Flower flower = _flowerArea.GetFlowerFromNectar(collider);

                float nectarReceived = flower.Feed(0.01f);

                NectarObtained += nectarReceived;

                if (_trainingMode)
                {
                    float bonus = 0.02f * Mathf.Clamp01(Vector3.Dot(transform.forward.normalized, -_nearestFlower.FlowerUpVector.normalized));
                    AddReward(0.01f + bonus);
                }

                if (!flower.HasNectar)
                    UpdateNearestFlower();
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(_trainingMode && collision.collider.CompareTag("boundary"))
        {
            AddReward(-0.5f);
        }
    }

    private void Update()
    {
        if (_nearestFlower)
            Debug.DrawLine(_beakTip.position, _nearestFlower.FlowerCenterPosition, Color.green);
    }

    private void FixedUpdate()
    {
        //Avoid scenario where nearest flower nectar is stolen by opponent and not updated
        if (!_nearestFlower)
            if(!_nearestFlower.HasNectar)
                UpdateNearestFlower();
    }
}
