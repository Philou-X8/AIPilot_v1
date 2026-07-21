using AIPProvider.src.SensorFusion;
using AIPProvider.src.Utilities;
using Recorder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityGERunner;
using UnityGERunner.UnityApplication;

namespace AIPProvider.src.Visual
{
    internal class VisualSpotter
    {
        private Utils utils;

        private Transform transform;

        //private TrackFileVisual track;

        private List<TrackFileVisual> planes;
        private List<TrackFileMissile> missiles;


        public VisualSpotter(Utils utilities)
        {
            utils = utilities;
            transform = new Transform();

            planes = new List<TrackFileVisual>();
            missiles = new List<TrackFileMissile>();

            //track = new TrackFileVisual(utils, 2);

        }

        //public void UpdateDeprecated(OutboundState state)
        //{
        //    if (state.visualTargets.Length < 1)
        //    {
        //        track.GetData(out float lastSpeed, out float lastDist, out float lastSummed);
        //        if (lastDist > 10000) lastDist = 10000;
        //        utils.Graph("visual speed", 0.1f * lastSpeed);
        //        utils.Graph("visual dist", 0.01f * lastDist);
        //        utils.Graph("visual summed", 0.01f * lastSummed);
        //        return;
        //    }
        //    foreach (var target in state.visualTargets)
        //    {
        //        if (target.id != 2) continue;
        //        track.UpdateData(target, state.time, state.kinematics.position, state.kinematics.velocity);
        //    }

        //    track.GetData(out float speed, out float dist, out float summed);

        //    utils.Graph("visual speed", 0.1f * speed);
        //    utils.Graph("visual dist", 0.01f * dist);
        //    utils.Graph("visual summed", 0.01f * summed);


        //    return;
        //}

        public List<VisualPassiveCarrier> Update(OutboundState state)
        {
            transform.position = state.kinematics.position;
            transform.rotation = state.kinematics.rotation;

            utils.Graph("visual count", planes.Count);

            UpdateQueue(state);
            RemoveTimedOut(state.time);

            return GetTracks(state.kinematics.position);
        }

        public void RemoveDead(List<int> dead)
        {
            if (dead.Count == 0) return;
            foreach (int id in dead)
            {
                //planes[id].TimedOut(10000f); // HACK mark timed out so the line is erased
                //planes.RemoveAll(x => x.id == id);
                planes.RemoveAll(x => x.TimedOut(10000f)); // HACK mark timed out so the line is erased
            }
        }

        public bool ShouldGpull(out Vector3 pyr)
        {
            pyr = new Vector3(-1, 0, 0);

            float closest = 10001f;
            foreach(TrackFileMissile missile in missiles)
            {
                GpullInfo info = missile.GetGpullInfo();
                if(info.closure < 0) continue;

                if (Vector3.Dot(-info.direction, info.orientation) < 0.2f) continue;

                if(info.distance < closest && info.distance < 1800)
                {
                    closest = info.distance;
                    Vector3 relDir = transform.InverseTransformDirection(info.direction).normalized;

                    if(relDir.y < -0.1f ) // missile bellow horizon
                    {
                        pyr.z = (relDir.x >= 0) ? -0.2f : 0.2f; // roll to level the missile
                    }
                    else if(relDir.y > 0.2f)
                    {
                        pyr.z = (relDir.x >= 0) ? 0.5f : -0.5f;
                    }
                    else 
                    {
                    }
                        pyr.y = (relDir.x < 0) ? 0.6f : -0.6f;
                    
                }
            }

            return closest <= 1800;
            //return missiles.Any(missile => missile.ShouldGpull());
        }


        // TODO: Replace this with data center query so we can get info from friendlies too
        private bool TryGetRadarTrack(OutboundState state, int id, out Vector3 targetPos, out Vector3 targetVel)
        {
            // TODO: Replace this with data center query so we can get info from friendlies too
            targetPos = Vector3.zero;
            targetVel = Vector3.zero;

            if(state.radar.sttedTarget != null)
            {
                targetPos = state.radar.sttedTarget?.position ?? Vector3.zero;
                targetVel = state.radar.sttedTarget?.velocity ?? Vector3.zero;
                return true;
            }

            StateTargetData data = state.radar.twsedTargets.ToList().Find(contact => contact.id == id);
            if(data.id <= 0) return false;

            targetPos = data.position;
            targetVel = data.velocity;
            return true;
        }

        private void UpdateQueue(OutboundState state)
        {
            foreach(VisuallySpottedTarget vis in state.visualTargets)
            {
                if (utils.IsAllied(vis.team))
                {
                    // friendly, we do not care
                    continue;
                }

                if (vis.type == VisualTargetType.Missile)
                {
                    AddMissileData(state, vis);
                    continue;
                }

                if (vis.type != VisualTargetType.Aircraft)
                {
                    // it's a missile
                    continue;
                }
                
                if ( !planes.Any(known => known.id == vis.id)) // new visual
                {
                    planes.Add(new TrackFileVisual(utils, vis));
                }

                // TODO: check if position info is available in DataCenter
                // if available ? AddData() : UpdateData()
                if (TryGetRadarTrack(state, vis.id, out Vector3 targetPos, out Vector3 targetVel))
                {
                    planes.Find(known => known.id == vis.id)?.ForceData(
                        targetPos,
                        targetVel,
                        state.time,
                        state.kinematics.position,
                        state.kinematics.velocity
                        );
                }
                else
                {
                    planes.Find(known => known.id == vis.id)?.UpdateData(
                        vis,
                        state.time,
                        state.kinematics.position,
                        state.kinematics.velocity
                        );
                }
            }

            return;

            // collect visual data from friendlies
            foreach (VisualDLData friendVis in state.datalink.visual)
            {
                VisuallySpottedTarget vis = friendVis.data;
                FriendlyData friendState = state.datalink.friendlies.ToList().Find(friend => friend.id == friendVis.contributedBy);

                Vector3 position = friendState.position;
                Vector3 velocity = friendState.velocity;

                //Vector3 position = state.datalink.friendlies.ToList().Find(friend => friend.id == friendVis.contributedBy).position;


                // TODO: do something with friendly's data
            }
        }

        private void AddMissileData(OutboundState state, VisuallySpottedTarget vis)
        {
            if (!missiles.Any(known => known.id == vis.id)) // new visual
            {
                missiles.Add(new TrackFileMissile(vis));
            }

            // TODO: check if position info is available in DataCenter
            // if available ? AddData() : UpdateData()
            missiles.Find(known => known.id == vis.id)?.UpdateData(
                vis,
                state.time,
                state.kinematics.position,
                state.kinematics.velocity
                );
        }

        private void RemoveTimedOut(float t)
        {
            //const float TIMEOUT_LIMIT = 1.0f; // 1 sec
            //planes.RemoveAll(known => (t - known.Age()) > TIMEOUT_LIMIT);
            planes.RemoveAll(known => known.TimedOut(t));

            missiles.RemoveAll(known => known.DeadMissile(t));
        }


        private List<VisualPassiveCarrier> GetTracks(Vector3 ownPos)
        {
            //List<VisualPassiveCarrier> passiveTracks = planes.ConvertAll(track => track.MakePassiveData(ownPos));
            return planes.ConvertAll(track => track.MakePassiveData(ownPos));

            //foreach (TrackFileVisual known in planes)
            //{

            //}
        }

        //private bool TryGetSpeed(Vector3 ownVel, float closure, Vector3 dir, Vector3 rot, out float enemySpeed)
        //{
        //    enemySpeed = ownVel.magnitude;
        //    if (Vector3.Cross(dir, rot).magnitude > 0.95)
        //    {
        //        // notching, cannot guess speed
        //        // assume same speed as AIP
        //        utils.Graph("enemy speed", ownVel.magnitude);
        //        return false; // notching, cannot guess speed
        //    }

        //    // guess speed
        //    float directionnal = closure - Vector3.Dot(ownVel, dir);
        //    enemySpeed = -directionnal / Vector3.Dot(dir, rot);
        //    return true;
        //}

        //private bool GetNearest(OutboundState state, out float closure, out Vector3 dir, out Vector3 rot)
        //{
        //    closure = 0;
        //    dir = Vector3.zero;
        //    rot = Vector3.zero;
        //    if (state.visualTargets.Length < 1)
        //    {
        //        return false;
        //    }

        //    closure = state.visualTargets[0].closure;
        //    dir = state.visualTargets[0].direction;
        //    Quaternion quat = state.visualTargets[0].orientation;
        //    rot = quat * Vector3.forward;
        //    return true;
        //}
    }
}
