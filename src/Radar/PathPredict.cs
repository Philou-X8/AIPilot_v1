using AIPProvider.src.Utilities;
using Recorder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityGERunner;
using UnityGERunner.UnityApplication;

namespace AIPProvider.src.Radar
{
    internal class PathPredict
    {
        Utils utils;

        private const int trail_size = 10;
        private const int path_size = 20;
        private const int path_step_size = 20;

        private float lastTime = 0;

        private List<Transform> path;
        private List<Transform> pastPos; // TODO: rename to "trail"
        private List<Quaternion> pastRot; // 
        private Quaternion rollingAvgRot = new Quaternion();

        private Quaternion aSpeed;
        private Vector3 lastVel;

        private List<DebugLine> linesPath;
        private List<DebugLine> linesUp;

        public PathPredict(Utils utilities)
        {
            utils = utilities;

            pastPos = new List<Transform>();
            for (int i = 0; i < trail_size; i++) { pastPos.Add( new Transform()); }

            pastRot = new List<Quaternion>();
            for (int i = 0; i < trail_size; i++) { pastRot.Add(new Quaternion()); }

            path = new List<Transform>();
            for (int i = 0; i < path_size; i++) { path.Add( new Transform()); }

            aSpeed = Quaternion.identity;
            lastVel = Vector3.zero;

            linesPath = new List<DebugLine>();
            for (int i = 0; i < path_size; i++)
            {
                linesPath.Add(utils.MakeLine());
                linesPath[i].color = new NetColor(
                    Utils.MapRange(i, 0, path_size-1, 1.0f, 0.5f),
                    Utils.MapRange(i, 0, path_size-1, 1.0f, 0.3f),
                    0.1f,
                    1);
            }
            linesUp = new List<DebugLine>();
            for (int i = 0; i < path_size; i++)
            {
                linesUp.Add(utils.MakeLine());
                linesUp[i].color = new NetColor(
                    Utils.MapRange(i, 0, path_size-1, 1.0f, 0.5f),
                    Utils.MapRange(i, 0, path_size-1, 1.0f, 0.3f),
                    0.1f,
                    1);
            }
        }

        public void Update(OutboundState state)
        {

            Transform transform = new Transform();
            transform.position = state.kinematics.position;
            transform.rotation = state.kinematics.rotation;
            Vector3 vel = state.kinematics.velocity;
            AddTrail(transform, vel);

            float dt = state.time - lastTime;
            lastTime = state.time;


            //Quaternion stepRot = pastPos[1].InverseTransformRotation(pastPos[0].rotation);
            Quaternion stepRot = AverageRotation();
            Quaternion scaledRot = stepRot;
            for (int i = 1; i < path_step_size; i++) 
            {
                scaledRot = scaledRot * stepRot;
            }

            Vector3 relativeVel = transform.InverseTransformDirection(vel);
            Vector3 accel = relativeVel - lastVel;
            lastVel = relativeVel;
            accel = 2 * accel; // TEMP - must remove

            path[0].position = transform.position + (vel * dt * path_step_size);
            path[0].rotation = transform.rotation * scaledRot;
            for (int i = 1; i < path.Count; i++)
            {
                path[i].position = path[i - 1].position + (path[i - 1].TransformVector(relativeVel) * dt * path_step_size);
                path[i].rotation = path[i - 1].rotation * scaledRot;

                //relativeVel += accel;
            }


            // trace path
            for (int i = 0; i < path.Count - 1; i++)
            {
                linesPath[i].start = path[i].position;
                linesPath[i].end = path[i + 1].position;
                utils.Draw(linesPath[i]);

                linesUp[i].start = path[i].position;
                linesUp[i].end = path[i].position + path[i].up * (vel.magnitude * dt * path_step_size);
                utils.Draw(linesUp[i]);
            }

        }

        private void AddTrail(Transform transform, Vector3 vel)
        {
            
            for(int i = pastPos.Count - 1; i > 0; i--)
            {
                pastPos[i] = pastPos[i - 1].Clone();
            }
            pastPos[0] = transform;

        }

        private Quaternion AverageRotation()
        {
            Quaternion stepRot = pastPos[1].InverseTransformRotation(pastPos[0].rotation);
            Quaternion avgRot = pastRot[0];

            for(int i = 1; i < (trail_size - 0); i++)
            {
                float weight = 1f / (float)(i + 1);
                avgRot = Quaternion.Slerp(
                    pastRot[i],
                    avgRot,
                    weight
                    );
                pastRot[i - 1] = pastRot[i];
            }
            pastRot[trail_size - 1] = stepRot;
            avgRot = Quaternion.Slerp(
                    stepRot,
                    avgRot,
                    1f / (float)trail_size
                    );
            //return avgRot;

            rollingAvgRot = Quaternion.Slerp(rollingAvgRot, stepRot, 0.05f);
            return rollingAvgRot;
            /*
            int groupSize = (int)Mathf.Floor( trail_size / 2f);

            Quaternion rotA = pastPos[0].rotation;

            for(int i = 1; i < groupSize; i++)
            {
                float weight = 1f / (float)(i + 1);
                rotA = Quaternion.Slerp(
                    pastPos[i].rotation, 
                    rotA,
                    weight
                    );
            }

            Quaternion rotB = pastPos[groupSize].rotation;

            for (int i = groupSize + 1; i < (2 * groupSize); i++)
            {
                float weight = 1f / (float)(i + 1);
                rotB = Quaternion.Slerp(
                    pastPos[i].rotation,
                    rotA,
                    weight
                    );
            }
            pastPos[0].InverseTransformRotation( rotA );
            */
        }

    }
}
