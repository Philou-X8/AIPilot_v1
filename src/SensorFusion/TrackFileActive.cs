using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityGERunner;
using UnityGERunner.UnityApplication;

namespace AIPProvider.src.SensorFusion
{
    internal enum ActiveTrackState
    {
        tracking, // still not enough data
        pending, // awaiting scan to know if track is dead
        lost, // track not detected by any form of scan
        adding, // collecting additionnal data point
        ready, // all needed datapoint collected
        completed, // enough data point, gen passive track before deleting
    }

    internal class TrackFileActive
    {
        public int id { get; private set; }
        public ActiveTrackState state { get; private set; }
        public float stateUpdateTime { get; private set; }

        private List<Transform> trail; // past positions and rotations in absolute, index 0 most recent
        private List<Vector3> trailVel; // velocity history

        //leave this computation to the path prediction
        //private List<Quaternion> angularSpeed; // past rotations (between frames) in relative space

        //public StateTargetData data;
        private int frameCount; // number of sample data colected by this track

        public static int SAMPLE_COUNT = 10;
        private const int SAMPLE_MAX = 20; // max number of sample saved


        public TrackFileActive(int actorId)
        {
            id = actorId;
            state = ActiveTrackState.tracking;
            frameCount = 0;

            trail = new List<Transform>();
            trailVel = new List<Vector3>();
        }

        // enough data to generate a path prediction
        public bool IsReady()
        {
            return frameCount >= SAMPLE_COUNT;
        }

        public void AddFrame(StateTargetData tws, float t)
        {
            if(tws.id != id) { return; } // missmatching IDs

            if(stateUpdateTime == t)
            {
                return; //trackfile already update this frame 
            }
            stateUpdateTime = t;

            RecordData(tws);

            ChangeStateOnScan();
        }
        private void RecordData(StateTargetData tws)
        {
            Transform transform = new Transform();
            transform.position = tws.position;
            transform.rotation = tws.rotation;

            trail.Insert(0, transform);
            trailVel.Insert(0, tws.velocity);

            if(trail.Count > SAMPLE_MAX) // remove excess to keep list a reasonable size
            {
                trail.RemoveRange(SAMPLE_MAX-1, trail.Count - SAMPLE_MAX);
                trailVel.RemoveRange(SAMPLE_MAX-1, trail.Count - SAMPLE_MAX);
            }
            
            frameCount = trail.Count;
        }
        private void ChangeStateOnScan()
        {
            if(frameCount < SAMPLE_COUNT)
            {
                state = ActiveTrackState.tracking;
            }
            else
            {
                state = ActiveTrackState.adding;
            }
        }

        public void ChangeStateAfter()
        {
            switch (state)
            {
                case ActiveTrackState.tracking:
                    state = ActiveTrackState.pending;
                    break;
                case ActiveTrackState.pending:
                    state = ActiveTrackState.lost;
                    break;
                case ActiveTrackState.lost:
                    break;
                case ActiveTrackState.adding:
                    state = ActiveTrackState.ready;
                    break;
                case ActiveTrackState.ready:
                    state = ActiveTrackState.completed;
                    break;
                case ActiveTrackState.completed:
                    break;
            }
        }

        public void GetConversionData(out Transform tr, out Vector3 vel)
        {
            tr = (trail.Count >= 1) ? trail[0] : new Transform();
            vel = (trail.Count >= 1) ? trailVel[0] : Vector3.zero;
        }

        public ActivePassiveCarrier MakePassiveData()
        {
            PassiveTrackState tempState = state switch
            {
                ActiveTrackState.adding => PassiveTrackState.ActiveStrong,
                ActiveTrackState.tracking => PassiveTrackState.ActiveWeak,
                ActiveTrackState.ready or ActiveTrackState.completed => PassiveTrackState.LostStrong,
                ActiveTrackState.pending or ActiveTrackState.lost => PassiveTrackState.LostWeak,
                _ => PassiveTrackState.Lost,
            };

            // Position conversion
            Transform pos = new Transform();
            pos.position = trail[0].position;
            pos.rotation = trail[0].rotation;

            // Velocity conversion

            // Angular Speed conversion
                // TODO

            return new ActivePassiveCarrier
            {
                id = this.id,
                updateTime = stateUpdateTime,
                passiveState = tempState,
                trail = pos,
                trailVel = this.trailVel[0],
            };
        }
    }


    // carries data needed for conversion between 
    internal struct ActivePassiveCarrier
    {
        public int id;
        public float updateTime;
        public PassiveTrackState passiveState;
        public Transform trail; // past positions and rotations in absolute, index 0 most recent
        public Vector3 trailVel; // velocity history
        // angular
    }
}
