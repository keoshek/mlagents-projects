using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using Unity.Mathematics;

namespace Test1
{
    public class MoveToGoalAgent : Agent
    {
        [SerializeField] private Transform targetTransform;
        [SerializeField] private float moveSpeed = 1f;
        
        [SerializeField] private Renderer floorRenderer;
        [SerializeField] private Material winMaterial;
        [SerializeField] private Material loseMaterial;

        [SerializeField] private Vector3 initialAgentPos;
        [SerializeField] private Vector3 initialGoalPos;
        [SerializeField] private bool randomizePositioning;
        [SerializeField] private float2 agentRandomX;
        [SerializeField] private float2 agentRandomZ;
        [SerializeField] private float2 goalRandomX;
        [SerializeField] private float2 goalRandomZ;


        public override void OnEpisodeBegin()
        {
            if (randomizePositioning) { 
                transform.localPosition = new Vector3(UnityEngine.Random.Range(agentRandomX.x, agentRandomX.y), 0, UnityEngine.Random.Range(agentRandomZ.x, agentRandomZ.y));
                targetTransform.localPosition = new Vector3(UnityEngine.Random.Range(goalRandomX.x, goalRandomX.y), 0, UnityEngine.Random.Range(goalRandomZ.x, goalRandomZ.y));
            } else {
                transform.localPosition = initialAgentPos;
                targetTransform.localPosition = initialGoalPos;
            }
        }


        public override void CollectObservations(VectorSensor sensor)
        {
            //sensor.AddObservation(transform.localPosition);
            //sensor.AddObservation(goalTransform.localPosition);

            sensor.AddObservation((transform.position - targetTransform.position).normalized);  // 3 observations

            // overall = 3
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
            floorRenderer.material = winMaterial;
            EndEpisode();
        }


        /*public void Lose()
        {
            float distance = Vector3.Distance(transform.position, goalTransform.position);
            SetReward(-1 + (1 / (distance * distance)));
            floorRenderer.material = loseMaterial;
            EndEpisode();
        }*/


        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out Goal _)) Win();
        }
    }
}


