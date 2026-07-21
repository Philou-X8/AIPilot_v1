using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityGERunner;
using UnityGERunner.UnityApplication;

namespace AIPProvider.src.SensorFusion
{
    internal class SimData
    {
        public Transform transform { get; private set; }
        public Vector3 velocity { get; private set; }
        public Vector3 pos => transform.position;
        public Quaternion rot => transform.rotation;
        public Vector3 vel => velocity;

        public RadarState radar { get; private set; }
        public Dictionary<int, StateRWRContact> rwrContacts { get; private set; }
        public Dictionary<int, VisuallySpottedTarget> visualTargets { get; private set; }


        // ------------------------------------------------------ private
        private Vector3 lastPos = Vector3.zero;

        public SimData()
        {
            transform = new Transform();
        }
        //public SimData(OutboundState state)
        //{
        //    transform = new Transform();
        //}

        public void Update(OutboundState state)
        {

            transform.position = state.kinematics.position;
            transform.rotation = state.kinematics.rotation;

            velocity = transform.position - lastPos;
            lastPos = transform.position;

            radar = state.radar;
        }

    }

    internal class SimRadarState
    {

    }
}
