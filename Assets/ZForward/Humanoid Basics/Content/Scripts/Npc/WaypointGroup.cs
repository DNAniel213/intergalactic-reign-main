using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Humanoid_Basics.Npc
{
    [ExecuteInEditMode]
    public class WaypointGroup : MonoBehaviour
    {
        public List<Waypoint> waypoints = new List<Waypoint>();
        public Color waypointsColor = Color.magenta;

        private void Update()
        {
            if (transform.childCount < waypoints.Count)
            {
                waypoints.Clear();
            }
            
            // Exit if no waypoints
            if (transform.childCount <= waypoints.Count) return;
            
            foreach (Transform t in transform)
            {
                waypoints.Add(!t.gameObject.GetComponent<Waypoint>()
                    ? t.gameObject.AddComponent<Waypoint>()
                    : t.gameObject.GetComponent<Waypoint>());
            }
        }

        private void OnDrawGizmos()
        {
            if (waypoints.Count <= 0) return;
            Gizmos.color = waypointsColor;
            foreach (var point in waypoints.Where(point => point != null))
            {
                Gizmos.DrawSphere(point.transform.position, 0.5f);
            }
        }
    }
}
