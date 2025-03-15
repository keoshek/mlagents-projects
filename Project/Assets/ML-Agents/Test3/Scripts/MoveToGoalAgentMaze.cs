using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.Mathematics;
using Test1;

namespace Test3
{
    public class MoveToGoalAgentMaze : Agent
    {
        [Header("General")]
        [SerializeField] private Renderer winIndicationRenderer;
        [SerializeField] private Material winMaterial;
        [SerializeField] private Material loseMaterial;
        
        [Header("Agent")]
        [SerializeField] private float moveSpeed = 1f;
        [SerializeField] private float rotateSpeed = 1f;
        [SerializeField] private float2 agentRandomX;
        [SerializeField] private float2 agentRandomZ;

        [Header("Goal")]
        [SerializeField] private Transform goalTransform;
        [SerializeField] private float2 goalRandomX;
        [SerializeField] private float2 goalRandomZ;

        [Header("Moving Walls")]
        [SerializeField] private Transform[] walls;
        [SerializeField] private Vector2 wallRandomXPos;
        [SerializeField] private Vector2 wallRandomZPos;


        private bool isFirstRound;
        private bool wonPrevRound;


        public override void Initialize()
        {
            isFirstRound = true;
        }


        public override void OnEpisodeBegin()
        {
            // win/lose condition rendering
            if (!isFirstRound) {
                winIndicationRenderer.material = wonPrevRound ? winMaterial : loseMaterial;
            }

            // random positioning of the walls
            foreach (Transform wall in walls)
            {
                // position
                wall.localPosition = new (UnityEngine.Random.Range(wallRandomXPos.x, wallRandomXPos.y), 0, UnityEngine.Random.Range(wallRandomZPos.x, wallRandomZPos.y));

                // rotation
                wall.rotation = Quaternion.Euler(new(0, UnityEngine.Random.Range(0, 360), 0));
            }

            // random positioning of the agent
            transform.localPosition = new Vector3(UnityEngine.Random.Range(agentRandomX.x, agentRandomX.y), 0, UnityEngine.Random.Range(agentRandomZ.x, agentRandomZ.y));

            // random positioning of the goal
            goalTransform.localPosition = new Vector3(UnityEngine.Random.Range(goalRandomX.x, goalRandomX.y), 0, UnityEngine.Random.Range(goalRandomZ.x, goalRandomZ.y));
            while (DirectionToGoal().magnitude < 5)
            {
                goalTransform.localPosition = new Vector3(UnityEngine.Random.Range(goalRandomX.x, goalRandomX.y), 0, UnityEngine.Random.Range(goalRandomZ.x, goalRandomZ.y));
            }

            // random rotation of the agent
            transform.rotation = Quaternion.Euler(new (0, UnityEngine.Random.Range(0, 360), 0));

            // reset win condition
            wonPrevRound = false;
            
            // must run at the last end of this method
            isFirstRound = false;
        }


        public override void CollectObservations(VectorSensor sensor)
        {
            sensor.AddObservation(DirectionToGoal().normalized);  // 3 observations

            sensor.AddObservation(DirectionToGoal().magnitude);   // 1 observation

            // overall = 4
        }


        public override void OnActionReceived(ActionBuffers actions)
        {
            // total of 3 actions
            float moveX = actions.ContinuousActions[0];
            float moveZ = actions.ContinuousActions[1];
            float rotateY = actions.ContinuousActions[2];

            // clamp
            moveX = Mathf.Clamp(moveX, -1, 1);
            moveZ = Mathf.Clamp(moveZ, -1, 1);
            rotateY = Mathf.Clamp(rotateY, -1, 1);

            // movement
            transform.localPosition += moveSpeed * Time.deltaTime * new Vector3(moveX, 0, moveZ);

            // rotation
            transform.Rotate(transform.up, rotateSpeed * Time.deltaTime * rotateY);

            // existential negative reward
            AddReward(-0.0002f);

            // positive reward as long as the agents keeps looking at the goal
            // max reward when looking directly at it = 0.001
            // min reward when angle is 180 = 0;
            float angle = Vector3.Angle(transform.forward, DirectionToGoal().normalized);
            float normalizedReward = (angle - 180) / (0 - 180);
            AddReward(normalizedReward * 0.0002f);
        }


        public override void Heuristic(in ActionBuffers actionsOut)
        {
            ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
            
            // movement input
            continuousActions[0] = Input.GetAxisRaw("Horizontal");
            continuousActions[1] = Input.GetAxisRaw("Vertical");

            // rotation input
            float rotateValue = 0f;
            if (Input.GetKey(KeyCode.Q)) rotateValue--;
            if (Input.GetKey(KeyCode.E)) rotateValue++;
            continuousActions[2] = rotateValue;
        }


        private void Win()
        {
            AddReward(+1);
            wonPrevRound = true;
            EndEpisode();
        }


        private Vector3 DirectionToGoal()
        {
            return goalTransform.position - transform.position;
        }


        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out Goal _)) Win();
        }


        private void OnCollisionEnter(Collision collision)
        {
            if (collision.transform.TryGetComponent(out Wall _)) { 
                AddReward(-0.0005f);
            }
        }


        private void OnCollisionStay(Collision collision)
        {
            if (collision.transform.TryGetComponent(out Wall _))
            {
                AddReward(-0.0005f);
            }
        }
    }
}


