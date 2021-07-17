using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Humanoid_Basics.Player;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Humanoid_Basics.Npc
{
    [DisallowMultipleComponent]
    public class NpcBehaviour : MonoBehaviour
    {
        [Header("Main Setup")]
        public HumanoidCore humanoidCore;
        public NavMeshAgent agent;
        
        public enum AITarget
        {
            Idle,
            Waypoints,
            Target,
            Fleeing
        }

        private struct WaypointsData
        {
            public WaypointGroup waypointGroup;
            public float closestDistance;

            public WaypointsData(WaypointGroup wg, float dist)
            {
                waypointGroup = wg;
                closestDistance = dist;
            }
        }

        [Header("AI Settings")] 
        public AITarget aiType = AITarget.Waypoints;
        public float agentRotationSpeed = 5f;

        private Vector3 lastCorrectDestination;
        
        public Vector3 lastWaypointPos;
        public Waypoint nextWaypoint;
        public WaypointGroup waypoints;
        
        [Header("Development Only")] 
        public bool switchWeapon;
        public bool aim;

        private void Awake()
        {
            agent = humanoidCore.playerAnimator.GetComponent<NavMeshAgent>();
        }

        private void Start()
        {
            agent.updateRotation = false;
            agent.updatePosition = false;
            agent.isStopped = false;
            humanoidCore.crouch = false;
            humanoidCore = GetComponent<HumanoidCore>();

            switch (aiType)
            {
                case AITarget.Idle:
                    agent.updatePosition = false;
                    agent.isStopped = true;
                    break;
                case AITarget.Waypoints:
                    agent.updatePosition = true;
                    var nextWaypoints = FindClosestWaypoints();
                    if (nextWaypoints != waypoints)
                    {
                        waypoints = FindClosestWaypoints();
                    }
                    SetAgentDestination(NextWaypoint());
                    break;
                case AITarget.Target:
                    break;
                case AITarget.Fleeing:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (!agent.isOnNavMesh)
            {
                Debug.LogError("[NpcBehaviour] Npc is not on a nav mesh.");
            }
        }

        private IEnumerator EquipWeapon()
        {
            yield return new WaitForSeconds(5f);
            humanoidCore.SwitchWeapon(0);
        }

        private void Update()
        {
            if (humanoidCore.humanoidStatus == HumanoidCore.Status.Dead) return;
            
            if (switchWeapon)
            {
                humanoidCore.SwitchWeapon(0);
                switchWeapon = false;
            }
            humanoidCore.aim = aim;

            // AI
            switch (aiType)
            {
                case AITarget.Idle:
                    break;
                case AITarget.Waypoints:
                    WaypointAI();
                    break;
                case AITarget.Target:
                    HuntTarget();
                    break;
                case AITarget.Fleeing:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

        }

        private void HuntTarget()
        {
            
        }

        private WaypointGroup FindClosestWaypoints()
        {
            var waypointData = (from w in FindObjectsOfType<WaypointGroup>()
                                         select new WaypointsData(w, 0)).ToArray();
    
            for (var i = 0; i < waypointData.Length; i++)
            {
                float distance = 0;
    
                foreach (var point in waypointData[i].waypointGroup.waypoints)
                {
                    float newDistance = 0;
    
                    if ((newDistance = Vector3.Distance(transform.position, point.transform.position)) < distance || distance == 0)
                    {
                        distance = newDistance;
                    }
                }
    
                waypointData[i].closestDistance = distance;
            }
    
            return waypointData.OrderBy(x => x.closestDistance).FirstOrDefault().waypointGroup;
        }

        private void WaypointAI()
        {

            if (humanoidCore.ragdollHelper.ragdolled)
            {
                aiType = AITarget.Idle;
            }
            
            // Check if Npc is at destination
            if (!agent.updatePosition) return;

            if (!AtDestination())
            {
                RotateTowards(agent.steeringTarget);
                humanoidCore.isRunning = true;
                humanoidCore.runKeyPressed = 2f;
                humanoidCore.SetYAxis(2f);
            }
            else
            {
                SetAgentDestination(NextWaypoint());
                humanoidCore.SetYAxis(0f);
            }
        }

        public void SetAgentDestination(Vector3? destination, bool stopAgent = false)
        {
            var path = new NavMeshPath();
            if (destination == null) return;
            var dest = destination.Value;

            agent.isStopped = stopAgent;

            if (agent.CalculatePath(dest, path))
            {
                agent.SetDestination(dest);
                lastCorrectDestination = dest;
            }
            else
            {
                agent.SetDestination(lastCorrectDestination);
            }
        }

        private bool IsPathPossible(Vector3 path)
        {
            var navMeshPath = new NavMeshPath();
            agent.CalculatePath(path, navMeshPath);
            return navMeshPath.status != NavMeshPathStatus.PathPartial && navMeshPath.status != NavMeshPathStatus.PathInvalid;
        }

        private Vector3? NextWaypoint()
        {
            if (waypoints && waypoints.waypoints.Count > 1)
            {

                var possibleWaypoints = waypoints.waypoints.Where(x => (!x.isOccupied|| x.isOccupied && x.occupiedBy == gameObject) && IsPathPossible(x.transform.position)).OrderBy(x => x.gameObject.name).ToList();
                //var possibleWaypoints = waypoints.waypoints.Where(x => (!x.isOccupied) && IsPathPossible(x.transform.position)).OrderBy(x => x.gameObject.name).ToList();

                if (nextWaypoint)
                {
                    nextWaypoint.isOccupied = false;
                    nextWaypoint.occupiedBy = null;
                }

                var next = nextWaypoint != null ? possibleWaypoints.IndexOf(nextWaypoint) == possibleWaypoints.Count - 1 ? 0 : possibleWaypoints.IndexOf(nextWaypoint) + 1 : 0;

                nextWaypoint = possibleWaypoints[next];
                nextWaypoint.isOccupied = true;
                nextWaypoint.occupiedBy = gameObject;

                return lastWaypointPos = nextWaypoint.transform.position;
            }

            Debug.LogError("[NextWaypoint] Could not set next waypoint!");
            return null;
        }
        
        private bool AtDestination()
        {
            return agent.remainingDistance <= agent.stoppingDistance && agent.velocity.sqrMagnitude <= 0.1f && !agent.pathPending;
        }

        private void RotateTowards(Vector3 target)
        {
            // Make sure that agent.updateRotation is false
            agent.updateRotation = false;

            // Rotate transform towards target
            var direction = (target - humanoidCore.playerAnimator.transform.position).normalized;
            var lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            humanoidCore.playerAnimator.transform.rotation = Quaternion.SlerpUnclamped(humanoidCore.playerAnimator.transform.rotation, lookRotation, Time.deltaTime * agentRotationSpeed);
        }

    }
}
