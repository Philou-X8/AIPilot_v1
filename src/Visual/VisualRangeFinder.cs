using AIPProvider.src.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityGERunner.UnityApplication;
using UnityGERunner;

namespace AIPProvider.src.Visual
{
    internal class VisualRangeFinder
    {
        
        private float updateTime;

        private TrailVect rot;
        private TrailFloat speed;
        private TrailFloat distance;

        private Vector3 origin; // no incertitude on direction
        private Vector3 dir; // no incertitude on direction

        private Vector3 lastOrigin;
        private Vector3 lastDir;

        private float closure; // no incertitude on closure
        private float summedDist;

        private const float ERR_SPD_MIN = 0.3f;
        private const float ERR_SPD_MAX = 2.0f; // dot product, there's not max so set it to 2
        private const float ERR_DIS_MIN = 0.01f;
        private const float ERR_DIS_MAX = 0.03f;

        private float MAX_RANGE;

        public VisualRangeFinder(VisualTargetType type)
        {
            rot = new TrailVect(20);
            speed = new TrailFloat(10);
            distance = new TrailFloat(10);

            origin = new Vector3();
            dir = new Vector3();
            lastOrigin = new Vector3();
            lastDir = new Vector3();

            MAX_RANGE = (type == VisualTargetType.Aircraft) ? 10000 : 5000;
            closure = 0;
            summedDist = MAX_RANGE;
        }

        // run full update based on own visual data
        public void UpdateCompute(VisuallySpottedTarget data, float t, Vector3 ownPos, Vector3 ownVel)
        {
            updateTime = t;
            origin = ownPos;
            dir = data.direction;
            closure = data.closure;
            rot.Add(data.orientation.quat * Vector3.forward);

            bool goodData = true;
            //goodData = goodData && TryGetSpeed(ownVel, out float noseSpeed);
            //speed.Add(noseSpeed);

            if(TryGetSpeed(ownVel, out float noseSpeed))
            {
                speed.Add(noseSpeed);
            }
            else
            {
                speed.Add(noseSpeed);
                goodData = false;
            }

            if(TryGetDistance(out float dist))
            {
                distance.Add(dist);
            }
            else
            {
                distance.Add(summedDist - closure * Time.fixedDeltaTime);
                goodData = false;
            }

            if(goodData)
            {
                summedDist = distance.Avg();
            }
            else
            {
                summedDist += -closure * Time.fixedDeltaTime;
            }

            summedDist = Math.Clamp(summedDist, 0, MAX_RANGE);

            lastDir = dir;
            lastOrigin = ownPos;
        }

        // run lightweight update using data from better sensor
        public void UpdateForced(float t, float trueDist, Vector3 trueVel, Vector3 newPos, Vector3 newDir)
        {
            updateTime = t;
            distance.Add(trueDist);
            rot.Add(trueVel.normalized);
            speed.Add(trueVel.magnitude);
            summedDist = trueDist;

            origin = newPos;
            dir = newDir;
            lastOrigin = newPos;
            lastDir = newDir;
        }

        public void GetFullData(out float outDist, out float outSpeed, out Vector3 outRot)
        {
            outDist = summedDist;
            outSpeed = speed.Avg();
            outRot = rot.Avg();
        }

        public float GetDistance()
        {
            return summedDist;
        }

        private bool TryGetSpeed(Vector3 ownVel, out float noseSpeed)
        {
            noseSpeed = speed.Latest();
            //noseSpeed = ownVel.magnitude;

            float confidence = Math.Abs(Vector3.Dot(dir, rot.Avg()));
            if(confidence < ERR_SPD_MIN || confidence > ERR_SPD_MAX)
            {
                // notching, cannot guess speed
                return false;
            }

            // guess speed
            float directionnal = closure - Vector3.Dot(ownVel, dir);
            noseSpeed = -directionnal / Vector3.Dot(dir, rot.Avg());

            return true;
        }

        private bool TryGetDistance(out float dist)
        {
            dist = summedDist;

            bool ret = Utils.SkewLines(
                lastOrigin,
                lastDir,
                origin - rot.Avg().normalized * speed.Avg() * Time.fixedDeltaTime,
                dir,
                out Vector3 center,
                out float confidence);

            if(confidence < ERR_DIS_MIN || confidence > ERR_DIS_MAX)
            {
                ret = false;
            }
            if (ret)
            {
                dist = (center - origin).magnitude;
            }
            return ret;
        }


        public float Age()
        {
            return updateTime;
        }
    }
}
