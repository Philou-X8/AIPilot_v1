using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Recorder;
using System.Runtime.Loader;
using UnityGERunner;
using UnityGERunner.UnityApplication;
using System.Runtime.CompilerServices;
using AIPProvider.src.Utilities;
using AIPProvider.src.Flying;
using AIPProvider.src.Terrain;

namespace AIPProvider.src
{
    internal class Navigation
    {
        private Utils utils;
        private Transform transform;

        private Vector3 bullseye;
        private float zoneRadius;

        private Vector3 waypoint;
        private float lastPathChange;

        private DebugLine lineDestination;

        private Steering steering;
        private Ratting ratting;

        public Navigation(Utils utilities, GroundUtils gutilities)
        {
            utils = utilities;
            transform = new Transform();
            steering = new Steering(utilities);
            ratting = new Ratting(utilities, gutilities);

            bullseye = utils.info.mapCenterPoint;
            zoneRadius = utils.info.spawnDist / 1f;
            ChangeWaypoint();

            //waypoint = bullseye;
            //if (utils.IsAllied(Team.Allied)) waypoint.y = 5000; else { waypoint.y = 4000; waypoint.z += -2000; }
            //waypoint.y = 4000;

            lastPathChange = 0;

            lineDestination = utils.MakeLine();
            lineDestination.color = new NetColor(0.0f, 1.0f, 0.2f, 1);

            utils.Log("bullseye location: " + bullseye.ToString());
        }

        public InboundState Update(OutboundState state)
        {
            InboundState output = new InboundState();

            transform.position = state.kinematics.position;
            transform.rotation = state.kinematics.rotation;

            if( (waypoint - transform.position).magnitude < 20)
            {
                if(utils.IsAllied(Team.Enemy)) ChangeWaypoint();
                lastPathChange = state.time;

                //utils.Log("new NAV waypoint (destination): " + transform.InverseTransformVector(waypoint).normalized.ToString());
            }
            else if ( (state.time - lastPathChange) > 30f)
            {
                if (utils.IsAllied(Team.Enemy)) ChangeWaypoint();
                lastPathChange = state.time;
                //utils.Log("new NAV waypoint (timeout): " + transform.InverseTransformVector(waypoint).normalized.ToString());
            }

            Vector3 checkpoint = waypoint;
            //if(transform.position.y < 2000) checkpoint = ratting.Update(state, waypoint);
            Vector3 pyr = steering.SteerWaypoint(transform, checkpoint);

            //Vector3 direction = transform.InverseTransformVector(waypoint - transform.position);
            //Vector3 pyr = TurnToWaypoint(direction);

            output.pyr = pyr;
            output.throttle = (state.kinematics.velocity.vec3.magnitude > 300) ? 0 : 100;

            DrawWaypoint(checkpoint);
            return output;
        }

        public Vector3 GetWaypoint()
        {
            return waypoint;
        }

        public void ForceWaypoint(Vector3 checkpoint)
        {
            waypoint = checkpoint;
        }

        private void ChangeWaypoint()
        {
            waypoint.x = UnityGERunner.Random.Range(bullseye.x - zoneRadius, bullseye.x + zoneRadius);
            waypoint.y = UnityGERunner.Random.Range(50, 10000);
            waypoint.z = UnityGERunner.Random.Range(bullseye.z - zoneRadius, bullseye.z + zoneRadius);
        }

        public void DrawWaypoint()
        {
            lineDestination.start = transform.position;
            lineDestination.end = waypoint;

            utils.Draw(lineDestination);
        }
        public void DrawWaypoint(Vector3 checkpoint)
        {
            lineDestination.start = transform.position;
            lineDestination.end = checkpoint;

            utils.Draw(lineDestination);
        }

        private Vector3 TurnToWaypoint(Vector3 direction)
        {
            Vector3 pyr = Vector3.zero;
            Vector3 dirNorm = direction.normalized;

            pyr.z = Math.Clamp(-dirNorm.x * 5, -1f, 1f);
            pyr.x = (dirNorm.z > 0) ? Math.Clamp(-dirNorm.y * 5, -1f, 1f) : -1;
            pyr.y = Math.Clamp(-dirNorm.x * 0.1f, -1f, 1f);

            return pyr;
        }

    }
}
