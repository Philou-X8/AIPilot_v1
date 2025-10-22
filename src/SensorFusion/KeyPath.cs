using AIPProvider.src.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UnityGERunner;

namespace AIPProvider.src.SensorFusion
{
    internal class KeyPath
    {


        private List<float> timestamp;
        private List<Vector3> points;
        private int size;

        private const float STEP_SCALE = 0.4f;

        public KeyPath() 
        {
            timestamp = new List<float>();
            points = new List<Vector3>();
            size = 0;
        }
        public KeyPath(Vector3 start) 
        {
            timestamp = new List<float>();
            points = new List<Vector3>();
            size = 0;

            AddPoint(0f, start);
        }
        public KeyPath(Vector3 start, Vector3 vel, float stepTime, float timeout) 
        {
            timestamp = new List<float>();
            points = new List<Vector3>();
            size = 0;

            float stepping = 0;
            while(stepping < timeout)
            {
                AddPoint(stepping, start);

                stepping += stepTime;
                start += vel * stepTime;
            }
        }

        public void AddPoint(float t, Vector3 v)
        {
            timestamp.Add(t);
            points.Add(v);
            size++;
        }

        public float TimedDistance(Vector3 origin, float speed)
        {
            if (size == 0) return 500f; // no path available? assume really far then
            if (speed <= 0f) speed = 200f; // if own speed is 0 for some reason???
            
            Vector3 p = points[0]; // target point
            Vector3 o = origin; // origin point
            float d = 0f; // distance between points
            float t = 0f; // travel time between points
            float elapsed = 0f; // total marching time
            for (int i = 0; i < 3; i++)
            {
                d = (p - o).magnitude;
                t = d / speed;
                elapsed += t * STEP_SCALE;

                // set next points
                p = PointAt(elapsed); // find next target point along path
                Vector3 direction = (p - o).normalized; // traveling direction for origin point 
                o = o + direction * speed * (t * STEP_SCALE); // slide origin toward target
            }

            return elapsed;
        }

        public Vector3 PointAt(float t)
        {
            if (size < 1) return new Vector3();
            else if (size < 2) return points[0]; // too small to interpolate
            

            for (int i = 1; i < size; i++)
            {
                if (timestamp[i] < t)
                {
                    continue; // upper bound not reached yet
                }

                float lowerT = timestamp[i-1];
                float uperT = timestamp[i];
                float lerp = Utils.MapRange(t, lowerT, uperT, 0f, 1f);

                return Vector3.Lerp(points[i - 1], points[i], lerp);
            }

            return points[size-1];
        }
    }
}
