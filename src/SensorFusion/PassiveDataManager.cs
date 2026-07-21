using AIPProvider.src.Radar.TWS;
using AIPProvider.src.Utilities;
using AIPProvider.src.Utilities.Settings;
using AIPProvider.src.Visual;
using Recorder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityGERunner;
using UnityGERunner.UnityApplication;

namespace AIPProvider.src.SensorFusion
{
    internal class PassiveDataManager
    {
        private Utils utils;

        //private List<TrackFilePassive> trackList;
        private Dictionary<int, TrackFilePassive> tracks; // try to use dict so searching by ID is easier

        public PassiveDataManager(Utils utilities) { 
            utils = utilities;
            //trackList = new List<TrackFilePassive>();
            tracks = new Dictionary<int, TrackFilePassive>();
        }

        public void Update(OutboundState state)
        {
            foreach (TrackFilePassive track in tracks.Values)
            {
                track.Update(state.time);
                utils.Draw(track.GetBeacon());
            }
        }

        public void RemoveDead(List<int> dead)
        {
            if (dead.Count == 0) return;
            foreach (int id in dead)
            {
                // TODO: erase debug lines
                tracks.Remove(id);
            }
        }
        public void UpdateFromActive(List<ActivePassiveCarrier> carriers)
        {
            foreach (ActivePassiveCarrier carrier in carriers)
            {
                if (tracks.TryGetValue(carrier.id, out TrackFilePassive? track))
                {
                    track?.MakeFromActive(carrier);
                }
                else // new track, first register it
                {
                    TrackFilePassive passiveTrack = new TrackFilePassive(carrier, utils.MakeLine(ColorMap.TrackLocation));
                    tracks.Add(carrier.id, passiveTrack);

                    //utils.Log("-------------------- ADDING NEW TRACK TO TSD ---------------------");
                }
            }
        }

        public void UpdateFromVisual(List<VisualPassiveCarrier> carriers)
        {
            foreach (VisualPassiveCarrier carrier in carriers)
            {
                if (tracks.TryGetValue(carrier.id, out TrackFilePassive? track))
                {
                    track?.MakeFromVisual(carrier);
                }
                else // new track, first register it
                {
                    TrackFilePassive passiveTrack = new TrackFilePassive(carrier, utils.MakeLine(ColorMap.TrackLocation));
                    tracks.Add(carrier.id, passiveTrack);

                    //utils.Log("-------------------- ADDING NEW TRACK TO TSD ---------------------");
                }
            }
        }

        public void NearestWaypoint(Transform selfTrans, Vector3 vel, float time, out int targetId, out Vector3 targetDestination)
        {
            targetId = -1;
            targetDestination = selfTrans.position + 10 * vel;
            

            if (tracks.Count <= 0) 
            {
                //utils.Log("could not find nearest target");
                return;
            }

            TrackFilePassive track = tracks.MinBy(pair => pair.Value.TimedDistance(selfTrans.position, vel)).Value;
            float travelT = track.TimedDistance(selfTrans.position, vel);
            targetId = track.id;
            targetDestination = track.InterceptionPoint(travelT, time);

        }

        public Vector3 LocationOfId(int id, float t)
        {
            Vector3 ret = Vector3.zero;
            if (tracks.TryGetValue(id, out TrackFilePassive? track))
            {
                track?.InterceptionPoint(0, t);
            }
            return ret;
        }

        public List<GuidanceParameter> GetMissileQueue(float t, Vector3 originPos)
        {
            List<GuidanceParameter> queue = new List<GuidanceParameter>();

            foreach (TrackFilePassive track in tracks.Values)
            {
                if(track.TryGetMissileRadar(t, originPos, out GuidanceParameter launch))
                {
                    queue.Add(launch);
                }
            }
            return queue;
        }

        public bool TryGetMissileRadar(int targetId, float t, Vector3 originPos, out GuidanceParameter launch)
        {
            if(tracks.TryGetValue(targetId, out TrackFilePassive? track))
            {
                return track.TryGetMissileRadar(t, originPos, out launch);
            }
            //if (tracks.ContainsKey(targetId))
            //{
            //    return tracks[targetId].TryGetMissileRadar(t, originPos, out launch);
            //}
            launch = new GuidanceParameter();
            return false;
        }
        //public int GetNearest(Transform selfTrans, float t)
        //{
        //    //utils.Graph("TSD count", tracks.Count);

        //    float bestDistance = 50000;
        //    int bestId = -1;
        //    foreach(TrackFilePassive track in tracks.Values)
        //    {
        //        float distance = (track.GetFuturePosition(t) - selfTrans.position).magnitude;
        //        //utils.Log("target id: " + track.id.ToString() + " , distance: " + distance.ToString());

        //        if (distance < bestDistance)
        //        {
        //            bestDistance = distance;
        //            bestId = track.id;
        //        }
        //    }
        //    //utils.Log("nearest distance = " + bestDistance.ToString());
        //    return bestId;
        //}
    }
}
