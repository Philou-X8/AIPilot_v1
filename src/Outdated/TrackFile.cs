using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Recorder;
using System.Runtime.Loader;
using UnityGERunner;
using UnityGERunner.UnityApplication;
using System.Runtime.CompilerServices;
using AIPProvider.src.Utilities;

namespace AIPProvider.src.Outdated
{
    internal class SensorFusionOld
    {
        Utils utils;
        //private List<Track> aircraftTracks;
        private Dictionary<int, TrackFile> aircraftTracks;
        private Dictionary<int, TrackFile> missileTracks;

        public SensorFusionOld(Utils utilities)
        {
            utils = utilities;
            //aircraftTracks = new List<Track>();
            aircraftTracks = new Dictionary<int, TrackFile>();
            missileTracks = new Dictionary<int, TrackFile>();

        }

        public InboundState Update(OutboundState state)
        {
            UpdateVisual(state);


            return new InboundState();
        }

        private void UpdateRadar(OutboundState state)
        {
            // TWS Targets
            foreach (StateTargetData target in state.radar.twsedTargets)
            {
                if (utils.IsAllied(target.team))
                {
                    continue;
                }

                if (aircraftTracks.TryGetValue(target.id, out TrackFile? trackInstance))
                {
                    trackInstance.SetTrackFile(target);
                }
                else
                {
                    aircraftTracks.Add(target.id, new TrackFile(utils, target.id));
                    aircraftTracks[target.id].SetTrackFile(target);
                }
            }

            // STT Targets
            if (state.radar.sttedTarget != null)
            {
                StateTargetData target = (StateTargetData)state.radar.sttedTarget;

                if (aircraftTracks.TryGetValue(target.id, out TrackFile? trackInstance))
                {
                    trackInstance.SetTrackFile(target);
                }
                else
                {
                    aircraftTracks.Add(target.id, new TrackFile(utils, target.id));
                    aircraftTracks[target.id].SetTrackFile(target);
                }
            }

            foreach (MinimalDetectedTargetData target in state.radar.detectedTargets)
            {
                if (utils.IsAllied(target.team))
                {
                    continue;
                }

                if (aircraftTracks.TryGetValue(target.id, out TrackFile? trackInstance))
                {
                    // TODO
                    // use [ state.radar.angle; ] to get a bearing from [ MinimalDetectedTargetData ] 
                }
                else
                {
                    aircraftTracks.Add(target.id, new TrackFile(utils, target.id));
                }
            }
        }

        private void UpdateVisual(OutboundState state)
        {
            foreach (VisuallySpottedTarget target in state.visualTargets)
            {
                if (target.type == VisualTargetType.Aircraft)
                {
                    if (utils.IsAllied(target.team))
                    {
                        continue;
                    }

                    if (aircraftTracks.TryGetValue(target.id, out TrackFile? trackInstance))
                    {
                        trackInstance.SetVisual(target.direction);
                    }
                    else
                    {
                        aircraftTracks.Add(target.id, new TrackFile(utils, target.id));
                        aircraftTracks[target.id].SetVisual(target.direction);
                    }
                }
                else if (target.type == VisualTargetType.Missile)
                {
                    // TODO
                }
            }
        }

        private void UpdateRWR(OutboundState state)
        {
            foreach (StateRWRContact ping in state.rwrContacts)
            {
                // cry
                int wtf_is_this_shit_i_have_to_cross_reference = ping.actorId;
                // TODO
            }
        }

    }



    internal class TrackFile
    {
        Utils utils;
        private Action<DebugLine> DrawShape;
        public int entityID;

        private Transform transform;

        private Vector3 positionKnown;
        private float positionTimeout;
        private Vector3 positionGuess;

        private Vector3 velocity;

        public Vector3 direction;
        private float directionTimeout;
        public float distance;

        // could probably use a priority system to give a position estimate
        // 1) give position if available
        // 2) convert direction and distance to position if available
        // 3) give direction and guess distance based on last known position 
        // 4) interpolate distance somehow
        // 5) assume distance is in/out of visual range


        public TrackFile(Utils utilities, int id)
        //public Track(int id)
        {
            utils = utilities;
            entityID = id;
            transform = new Transform();
            velocity = new Vector3();
        }

        public void SetVisual(Vector3 dirr)
        {
            direction = dirr;
            directionTimeout = 0;
            // compute other prediction stuff
        }

        public void SetTrackFile(StateTargetData data)
        {
            transform.position = data.position;
            positionKnown = data.position;
            positionTimeout = 0;
            positionGuess = data.position;

            velocity = data.velocity;

            transform.rotation = data.rotation;

        }

    }
}
