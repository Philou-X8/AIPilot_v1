using AIPProvider.src.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityGERunner.UnityApplication;
using UnityGERunner;
using Recorder;

namespace AIPProvider.src.Outdated
{
    internal class PathPrediction
    {
        Utils utils;

        private Transform transform;
        private Transform lastPos;
        private Transform newPos;
        private Vector3 lastVel;
        private Vector3 newVel;

        private List<Transform> pastPos;

        private List<Transform> path;
        private List<DebugLine> linesPath;
        private List<DebugLine> linesUp;

        private float lastT;

        public PathPrediction(Utils utilities)
        {
            utils = utilities;

            lastT = 0;
            lastPos = new Transform();
            newPos = new Transform();
            lastVel = new Vector3();
            newVel = new Vector3();

            pastPos = new List<Transform>();
            for (int i = 0; i < 10; i++)
            {
                pastPos.Add(new Transform());
            }

            path = new List<Transform>();
            linesPath = new List<DebugLine>();
            linesUp = new List<DebugLine>();
            for (int i = 0; i < 10; i++)
            {

                path.Add(new Transform());
                linesPath.Add(utils.MakeLine());
                linesPath[i].color = new NetColor(
                    Utils.MapRange(i, 0, 9, 1.0f, 0.5f),
                    Utils.MapRange(i, 0, 9, 1.0f, 0.5f),
                    0.1f,
                    1);
                linesUp.Add(utils.MakeLine());
                linesUp[i].color = new NetColor(
                    Utils.MapRange(i, 0, 9, 1.0f, 0.5f),
                    Utils.MapRange(i, 0, 9, 1.0f, 0.5f),
                    0.1f,
                    1);
            }

            //lineFront = utils.MakeLine();
            //lineFront.color = new NetColor(1.0f, 1.0f, 0.2f, 1);
            //lineUp = utils.MakeLine();
            //lineUp.color = new NetColor(1.0f, 1.0f, 1.0f, 1);

            transform = new Transform();
        }

        private List<Quaternion> pastRot = new List<Quaternion>();
        public InboundState UpdateV2(OutboundState state)
        {




            return new InboundState();
        }

        public InboundState Update(OutboundState state)
        {
            transform.position = state.kinematics.position;
            transform.rotation = state.kinematics.rotation;
            newPos = transform;
            newVel = state.kinematics.velocity;

            float elapsedTime = state.time - lastT;
            //if (dt != Time.fixedDeltaTime) utils.Log("delta time = " + dt.ToString() + " , fixedDeltaTime = " + Time.fixedDeltaTime.ToString());
            lastT = state.time;

            float timeStep = 0.2f;
            float iterationCount = timeStep / elapsedTime;

            // TODO: 
            Quaternion rotation = OldPos().InverseTransformRotation(newPos.rotation);

            int stepSize = 20;
            for (int i = 0; i < stepSize / pastPos.Count; i++)
            {
                rotation *= rotation.normalized;
                // ^^^ ERROR ^^^ : I must apply the original rotation to the result multiple time, and not the result onto itself 
            }


            //float stepperVel = newVel.magnitude + accel;
            float stepperVel = newVel.magnitude;

            path[0].position = newPos.position + newVel * elapsedTime * stepSize;
            path[0].rotation = transform.rotation; // TODO: remove AOA from the initial rotation
            //path[0].rotation *= rotation;
            for (int i = 1; i < path.Count; i++)
            {
                path[i].position = path[i - 1].position + path[i - 1].forward * (newVel.magnitude * elapsedTime * stepSize);
                path[i].rotation = path[i - 1].rotation;
                path[i].rotation *= rotation;

                //stepperVel += accel;
            }

            for (int i = 0; i < linesPath.Count - 1; i++)
            {

                linesPath[i].start = path[i].position;
                linesPath[i].end = path[i + 1].position;
                linesUp[i].start = path[i].position;
                linesUp[i].end = path[i].position + path[i].up * (newVel.magnitude * elapsedTime * stepSize);
                utils.Draw(linesPath[i]);
                utils.Draw(linesUp[i]);
            }

            MovePastPos();

            lastPos = newPos;
            lastVel = newVel;

            return new InboundState();
        }

        private void MovePastPos()
        {
            for (int i = 0; i < pastPos.Count - 1; i++)
            {
                pastPos[i] = pastPos[i + 1].Clone();
            }

            // TODO: remove AOA from rotation history so it better account for how much the velocity is rotating
            pastPos[pastPos.Count - 1] = transform.Clone();
        }
        private Transform OldPos()
        {
            return pastPos[0];
        }
    }
}
