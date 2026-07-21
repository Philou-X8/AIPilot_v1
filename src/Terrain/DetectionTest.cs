using AIPProvider.src.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityGERunner.UnityApplication;
using UnityGERunner;
using Recorder;

namespace AIPProvider.src.Terrain
{
    internal class DetectionTest
    {
        private Utils utils;
        private Transform transform;

        public delegate bool LinecastDel<NetVector>(NetVector a, NetVector b, out NetVector collisionPoint);
        private LinecastDel<NetVector> Linecast;
        private Func<NetVector, float> GroundAlt;

        private int frameCounter;
        private float lastAlt;

        private DebugLine collisionLine;


        public DetectionTest(Utils utilities, LinecastDel<NetVector> RefLinecast, Func<NetVector, float> RefGroundAlt)
        {
            utils = utilities;
            Linecast = RefLinecast;
            GroundAlt = RefGroundAlt;

            frameCounter = 0;
            lastAlt = 0;

            //visorLine = new DebugLine(100000 + info.id * 1000);
            //visorLine.color = new NetColor(1.0f, 1.0f, 0.2f, 1);
            collisionLine = utils.MakeLine(26, 120, 33);

            transform = new Transform();

            //utils.Graph("rdr alt", 0f);
        }

        public InboundState Update(OutboundState state)
        {
            transform.position = state.kinematics.position;
            transform.rotation = state.kinematics.rotation;

            frameCounter++;
            if (frameCounter < 10)
            {
                ForwardTerrain();
            }
            frameCounter = 0;

            //ForwardTerrain();
            //ComputeTerrain();

            return new InboundState();
        }

        private bool AtRisk()
        {
            // safe to assume we're above ground
            float alt = GroundAlt(transform.position); // ground altitude

            Vector3 direction = transform.forward.normalized;
            direction.y = direction.y - 0.1f; // add a downward bias to terrain check
            direction.Normalize();

            if (direction.y < 0) // we're pointing down, check for terrain in front
            {
                float dist = transform.position.y / -direction.y;
                if (dist > 2000) dist = 2000;
                Vector3 endPoint = transform.position + direction * dist;
            }
            else if(alt < 2000) // we're close to ground, check for cliff
            {

            }
            else // not at imminent risk of terrain collision
            {

            }

            return true;
        }

        private void ForwardTerrain()
        {
            Vector3 direction = transform.forward.normalized;
            float dist = transform.position.y / (direction.y != 0f ? -direction.y : 1);
            if (dist > 2000) dist = 2000;

            Vector3 endPoint = transform.position + direction * dist;

            bool foundGround = Linecast(transform.position, endPoint, out NetVector collision);

            if (foundGround)
            {
                collisionLine.end = collision;
            }
            else
            {
                collisionLine.end = endPoint;
            }
            collisionLine.start = transform.position;
            utils.Draw(collisionLine);
        }


        private void ComputeTerrain()
        {
            frameCounter++;
            if(frameCounter < 10)
            {
                //utils.Graph("rdr alt", Utils.m2ft(transform.position.y - lastAlt));
                //utils.Graph("rdr alt", Utils.m2ft(transform.position.y - lastAlt) * 0.001f);
                return;
            }
            frameCounter = 0;

            Vector3 bottom = transform.position;
            bottom.y = 0f;

            //Vector3 collision;
            bool foundTerrain = Linecast(transform.position, bottom, out NetVector collision);
            if (foundTerrain)
            {
                lastAlt = collision.y;
            }
            //utils.Graph("rdr alt", Utils.m2ft(transform.position.y - lastAlt));
            //utils.Graph("rdr alt", Utils.m2ft(transform.position.y - lastAlt) * 0.001f);
        }
    }
}
