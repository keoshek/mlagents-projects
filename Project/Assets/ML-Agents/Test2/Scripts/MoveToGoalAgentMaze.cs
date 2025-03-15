using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.Mathematics;
using Test1;

namespace Test2
{
    public class MoveToGoalAgentMaze : Agent
    {
        [Header("General")]
        [SerializeField] private bool randomizePositioning;
        [SerializeField] private Renderer winIndicationRenderer;
        [SerializeField] private Material winMaterial;
        [SerializeField] private Material loseMaterial;
        
        [Header("Agent")]
        [SerializeField] private float moveSpeed = 1f;
        [SerializeField] private Vector3 initialAgentPos;
        [SerializeField] private float2 agentRandomX;
        [SerializeField] private float2 agentRandomZ;
        
        [Header("Goal")]
        [SerializeField] private Transform targetTransform;
        [SerializeField] private Vector3 initialGoalPos;
        [SerializeField] private float2 goalRandomX;
        [SerializeField] private float2 goalRandomZ;

        [Header("Moving Walls")]
        [SerializeField] private Transform[] walls;
        [SerializeField] private Vector2 wallRandomXPos;


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
                Vector3 wallLocalPos = wall.localPosition;
                wallLocalPos.x = UnityEngine.Random.Range(wallRandomXPos.x, wallRandomXPos.y);
                wall.localPosition = wallLocalPos;
            }

            // positioning of the agent and the goal
            if (randomizePositioning) { 
                transform.localPosition = new Vector3(UnityEngine.Random.Range(agentRandomX.x, agentRandomX.y), 0, UnityEngine.Random.Range(agentRandomZ.x, agentRandomZ.y));
                targetTransform.localPosition = new Vector3(UnityEngine.Random.Range(goalRandomX.x, goalRandomX.y), 0, UnityEngine.Random.Range(goalRandomZ.x, goalRandomZ.y));
            } else {
                transform.localPosition = initialAgentPos;
                targetTransform.localPosition = initialGoalPos;
            }

            // reset win condition
            wonPrevRound = false;
            
            // must run at the last end of this method
            isFirstRound = false;
        }


        public override void CollectObservations(VectorSensor sensor)
        {
            Vector3 dir = targetTransform.position - transform.position;

            sensor.AddObservation(dir.normalized);  // 3 observations

            sensor.AddObservation(dir.magnitude);   // 1 observation

            // overall = 4
        }


        public override void OnActionReceived(ActionBuffers actions)
        {
            float moveX = actions.ContinuousActions[0];
            float moveZ = actions.ContinuousActions[1];

            AddReward(-0.0002f);
            transform.localPosition += moveSpeed * Time.deltaTime * new Vector3(moveX, 0, moveZ);
        }


        public override void Heuristic(in ActionBuffers actionsOut)
        {
            ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
            continuousActions[0] = Input.GetAxisRaw("Horizontal");
            continuousActions[1] = Input.GetAxisRaw("Vertical");
        }


        public void Win()
        {
            AddReward(+1);
            wonPrevRound = true;
            EndEpisode();
        }


        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out Goal _)) Win();
        }


        private void OnCollisionEnter(Collision collision)
        {
            if (collision.transform.TryGetComponent(out Wall _)) { 
                AddReward(-0.001f);
            }
        }


        private void OnCollisionStay(Collision collision)
        {
            if (collision.transform.TryGetComponent(out Wall _))
            {
                AddReward(-0.001f);
            }
        }
    }
}


