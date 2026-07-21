using AIPProvider.src.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityGERunner.UnityApplication;
using UnityGERunner;
using Recorder;
using System.Reflection.PortableExecutable;
using System.Runtime.Intrinsics.X86;

namespace AIPProvider.src.Terrain
{
    internal class Ratting
    {
        private Utils utils;
        private GroundUtils ground;
        private Transform transform;
        private Vector3 waypoint;

        private List<DebugLine> ratLines;
        private List<DebugLine> pathBeacon;

        private const int STEP_SCALE = 400; // distance between samples
        private const int STEP_COUNT = 4; // number of sample in a single branch
        private const int BRANCH_COUNT = 5; // number of branch to cast at each segement
        private const int PATH_LENGHT = 5; // total number of segment in the path

        public Ratting(Utils utilities, GroundUtils gutilities)
        {
            utils = utilities;
            ground = gutilities;
            transform = new Transform();

            ratLines = new List<DebugLine>();
            for (int i = 0; i < 3; i++)
            {
                ratLines.Add(utils.MakeLine(195, 255, 92));
            }
            pathBeacon = new List<DebugLine>();
            for (int i = 0; i < PATH_LENGHT; i++)
            {
                pathBeacon.Add(utils.MakeLine(195, 255, 92));
            }
        }

        public Vector3 Update(OutboundState state, Vector3 wayp)
        {
            transform.position = state.kinematics.position;
            transform.rotation = state.kinematics.rotation;
            waypoint = wayp;
            waypoint.y = 1000;

            Vector3 checkpoint = SearchPath(transform.position, transform.forward);
            checkpoint.y += 300;
            //checkpoint.y = ground.Alt(Vector3.Lerp(transform.position, checkpoint, 0.2f)) + 1000;
            if (ground.Cast(transform.position, checkpoint, out Vector3 collision))
            {
                checkpoint = collision;
                checkpoint.y += 300;
            }
            //checkpoint.y = 300f + new List<float> {
            //    ground.Alt(Vector3.Lerp(transform.position, checkpoint, 0.05f)),
            //    ground.Alt(Vector3.Lerp(transform.position, checkpoint, 0.10f)),
            //    ground.Alt(Vector3.Lerp(transform.position, checkpoint, 0.15f)),
            //    ground.Alt(Vector3.Lerp(transform.position, checkpoint, 0.20f)),
            //}.Max();
            //CastCone();
            //Vector3 temp = transform.position;
            //temp.y = 1000;
            //utils.Graph("terrain alt", ground.Alt(temp));


            return checkpoint;
        }

        private Vector3 SearchPath(Vector3 point, Vector3 dir)
        {
            Vector3 ret = waypoint;

            Vector3 stepPoint = point;
            Vector3 stepDir = dir;
            for (int i = 0; i < PATH_LENGHT; i++)
            {
                float alt = CastPivotPoint(stepPoint, stepDir, out Vector3 nextPoint, out Vector3 nextDir);
                pathBeacon[i].start = new Vector3(nextPoint.x, nextPoint.y + alt, nextPoint.z);
                pathBeacon[i].end = new Vector3(nextPoint.x, nextPoint.y + (alt+500) + (alt*2), nextPoint.z);
                utils.Draw(pathBeacon[i]);
                stepPoint = nextPoint;
                stepDir = nextDir;

                if(i==0)
                {
                    utils.Graph("minAlt", alt);
                }

                if (i == 2)
                {
                    ret = stepPoint;
                    ret.y += alt;
                }
            }

            return ret;
        }

        private float CastPivotPoint(Vector3 point, Vector3 dir, out Vector3 nextPoint, out Vector3 nextDir)
        {
            // level the pathing data
            point.y = 0;
            dir.y = 0;
            dir.Normalize(); // probably not needed

            // make pivot point
            Transform pathing = new Transform();
            pathing.position = point;
            pathing.LookAt(point + dir);

            List<Vector3> branchDir = new List<Vector3>();
            List<float> alts = new List<float>();
            for (int i = 0; i < BRANCH_COUNT; i++)
            {
                //float angleOffset = (i - 2) * 20f;
                float angleOffset = Utils.MapRange(i, 0, BRANCH_COUNT, -40f, 40f);
                Vector3 newDir = Quaternion.AngleAxis(angleOffset, Vector3.up) * dir * STEP_SCALE;
                branchDir.Add(newDir); // to remove?

                alts.Add( CastStraight(point, newDir) );
            }

            nextDir = PickBranch(alts, branchDir, dir, out float bestAlt);
            nextPoint = point + nextDir.normalized * STEP_SCALE * STEP_COUNT;

            return bestAlt;

            float minAlt = alts.Min();
            int bestIndex = alts.IndexOf(minAlt);
            nextPoint = point + branchDir[bestIndex].normalized * STEP_SCALE * STEP_COUNT;
            nextDir = branchDir[bestIndex];

            return minAlt;

            Vector3 fStep = pathing.forward * STEP_SCALE; // 2D forward vector
            Vector3 lStep = (pathing.forward + pathing.right * -0.5f).normalized * STEP_SCALE;
            Vector3 rStep = (pathing.forward + pathing.right * 0.5f).normalized * STEP_SCALE;

            nextPoint = point + fStep;
            nextDir = dir;
            //float minAlt = 50000;
            float tempAlt = CastStraight(point, fStep);
            if(tempAlt < minAlt)
            {
                minAlt = tempAlt;
                nextPoint = point + 3 * fStep;
                nextDir = fStep;
            }
            tempAlt = CastStraight(point, lStep);
            if (tempAlt < minAlt)
            {
                minAlt = tempAlt;
                nextPoint = point + 3 * lStep;
                nextDir = lStep;
            }
            tempAlt = CastStraight(point, rStep);
            if (tempAlt < minAlt)
            {
                minAlt = tempAlt;
                nextPoint = point + 3 * rStep;
                nextDir = rStep;
            }

            return minAlt;
        }
        private float CastStraight(Vector3 point, Vector3 dir)
        {
            float avg = 0;
            Vector3 marching = point;
            for (int i = 0; i < STEP_COUNT; i++)
            {
                marching += dir;
                avg += ground.Alt(marching);
            }
            avg = avg / STEP_COUNT;
            //return avg;
            return (avg > 0) ? avg : 0;
        }

        private Vector3 PickBranch(List<float> alts, List<Vector3> dirs, Vector3 forward, out float minAlt)
        {
            float min = alts.Min();
            float max = alts.Max();
            float distribution = (max - min);

            int indexBest = 3;
            float biasedBest = max;
            for (int i = 0; i < BRANCH_COUNT; i++)
            {
                // bias waypoint
                Vector3 dest = (waypoint - transform.position).normalized;
                float waypointBias = Vector3.Dot(dirs[i], dest);
                // bias forward
                float forwardBias = Vector3.Dot(dirs[i], forward);
                // apply bias
                float biasedAlt = alts[i] - waypointBias * distribution * 0.01f - forwardBias * distribution * 0.01f; // bias forward pointing 


                // best score
                if(biasedAlt < biasedBest)
                {
                    biasedBest = biasedAlt;
                    indexBest = i;
                }
            }

            minAlt = min;
            return dirs[indexBest];
        }



        private void CastCone()
        {
            Vector3 lookDir = transform.position + transform.forward;
            lookDir.y = transform.position.y;
            Transform pathing = new Transform();
            pathing.position = transform.position;
            pathing.LookAt(lookDir);

            Vector3 flightDir = pathing.forward; // 2D forward vector
            //flightDir.y = 0;
            //flightDir.Normalize();

            Vector3 flightDirL = (pathing.forward + pathing.right * -0.6f).normalized;
            Vector3 flightDirR = (pathing.forward + pathing.right * 0.6f).normalized;

            Vector3 frontPoint = transform.position + flightDir * 1000;
            Vector3 leftPoint = transform.position + flightDirL * 1000;
            Vector3 rightPoint = transform.position + flightDirR * 1000;

            ratLines[0].end = frontPoint;
            ratLines[1].end = leftPoint;
            ratLines[2].end = rightPoint;

            for (int i = 0; i < 3; i++)
            {
                ratLines[i].start = transform.position;
                utils.Draw(ratLines[i]);
            }


            // find lowest point
            float lowest = transform.position.y;
            Vector3 waypoint = lookDir;
            float tempAlt;
            tempAlt = ground.Alt(frontPoint);
            if(tempAlt < lowest)
            {
                lowest = tempAlt;
                waypoint = frontPoint - Vector3.up*(tempAlt);

            }
            tempAlt = ground.Alt(leftPoint);
            if(tempAlt < lowest)
            {
                lowest = tempAlt;
                waypoint = leftPoint - Vector3.up*(tempAlt);
            }
            tempAlt = ground.Alt(rightPoint);
            if(tempAlt < lowest)
            {
                lowest = tempAlt;
                waypoint = rightPoint - Vector3.up*(tempAlt);
            }

            ratLines[0].end = waypoint;
            utils.Draw(ratLines[0]);
        }
    }
}
