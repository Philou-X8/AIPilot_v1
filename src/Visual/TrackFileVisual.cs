using AIPProvider.src.SensorFusion;
using AIPProvider.src.Utilities;
using AIPProvider.src.Utilities.Settings;
using Recorder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityGERunner;
using UnityGERunner.UnityApplication;

namespace AIPProvider.src.Visual
{
    internal class TrackFileVisual
    {
        public int id;
        private Utils utils;


        private VisualRangeFinder rangeFinder;
        private VisuallySpottedTarget visData;

        private DebugLine line;

        public TrackFileVisual(Utils utilities, VisuallySpottedTarget data)
        {
            utils = utilities;
            id = data.id;
            visData = data;
            rangeFinder = new VisualRangeFinder(data.type);

            line = utils.MakeLine(ColorMap.VisualSpot);
        }

        //~TrackFileVisual()
        //{
        //    utils.Erase(line.id);
        //}


        public void UpdateData(VisuallySpottedTarget data, float t, Vector3 ownPos, Vector3 ownVel)
        {
            visData = data;
            rangeFinder.UpdateCompute(visData, t, ownPos, ownVel);

            line.start = ownPos;
            line.end = ownPos + data.direction.vec3 * rangeFinder.GetDistance();
            utils.Draw(line, 0.6f, 1.0f);
        }

        public void ForceData(Vector3 targetPos, Vector3 targetVel, float t, Vector3 ownPos, Vector3 ownVel)
        {
            Vector3 direction = targetPos - ownPos;
            visData.direction = direction.normalized;

            rangeFinder.UpdateForced(t, direction.magnitude, targetVel, ownPos, direction.normalized);

            line.start = ownPos;
            line.end = targetPos;
            utils.Draw(line, 0.6f, 1.0f);
        }

        public float Age()
        {
            return rangeFinder.Age();
        }

        public bool TimedOut(float t)
        {
            if(t - rangeFinder.Age() > 1.0f)
            {
                utils.Erase(line.id);
                return true;
            }
            return false;
        }
        public VisualPassiveCarrier MakePassiveData(Vector3 ownPos)
        {
            Transform pos = new Transform();

            rangeFinder.GetFullData(out float outDist, out float outSpeed, out Vector3 outRot);

            pos.position = ownPos + visData.direction.vec3 * outDist;
            pos.rotation = Quaternion.LookRotation(outRot);
            

            return new VisualPassiveCarrier
            {
                id = this.id,
                updateTime = rangeFinder.Age(),
                position = pos,
                velocity = outRot * outSpeed,
                //velocity = rot.Avg(),
            };
        }



    }

    internal struct VisualPassiveCarrier
    {
        public int id;
        public float updateTime;
        //public PassiveTrackState passiveState;
        public Transform position; // position
        public Vector3 velocity; // 
        // angular
    }
}
