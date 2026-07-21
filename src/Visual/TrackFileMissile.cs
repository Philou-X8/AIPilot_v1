using AIPProvider.src.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityGERunner;
using UnityGERunner.UnityApplication;

namespace AIPProvider.src.Visual
{
    internal class TrackFileMissile
    {
        public int id;
        private VisualRangeFinder rangeFinder;
        private VisuallySpottedTarget visData;

        public TrackFileMissile(VisuallySpottedTarget data)
        {
            id = data.id;
            visData = data;
            rangeFinder = new VisualRangeFinder(data.type);
        }

        public void UpdateData(VisuallySpottedTarget data, float t, Vector3 ownPos, Vector3 ownVel)
        {
            visData = data;
            rangeFinder.UpdateCompute(visData, t, ownPos, ownVel);
        }

        public bool ShouldGpull()
        {
            if(visData.closure < 0) return false;

            return rangeFinder.GetDistance() < 1800;
        }

        public GpullInfo GetGpullInfo()
        {
            return new GpullInfo
            {
                distance = rangeFinder.GetDistance(),
                closure = visData.closure,
                direction = visData.direction,
                orientation = visData.orientation.quat * Vector3.forward
            };
        }

        public bool DeadMissile(float t)
        {
            if (t - rangeFinder.Age() > 0.2f) return true;

            if (visData.closure < 0) return true;

            return false;
        }
    }

    internal struct GpullInfo
    {
        public float distance;
        public float closure;
        public Vector3 direction;
        public Vector3 orientation;
    }
}
