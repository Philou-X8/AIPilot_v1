using AIPProvider.src.Radar;
using AIPProvider.src.SensorFusion;
using AIPProvider.src.Utilities;
using Recorder;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using UnityGERunner;
using UnityGERunner.UnityApplication;
using static AIPProvider.src.Outdated.RadarController;

namespace AIPProvider.src.Outdated
{
    internal class RadarControllerTWS
    {
        private class ChannelTWS
        {
            public int channelId;
            private TrackFileTWS? tws;
            private MissileController? missile;
            private DebugLine line;
            private float lockTime; // time at witch this TWS was obtained
            private int usage; // 0 free, 1 spotting, 2 guiding

            public ChannelTWS(int channel, DebugLine l)
            {
                channelId = channel;
                line = l;

                tws = null;
                missile = null;
                lockTime = 0;
                usage = 0;
            }

            public void MakeSpotting(TrackFileTWS track, float t)
            {
                usage = 1;
                tws = null;
                missile = null;
                tws = track;
                lockTime = t;
            }
            public void MakeGuiding(MissileController controller, float t)
            {
                usage = 2;
                tws = null;
                missile = null;
                missile = controller;
                lockTime = t;
            }
            public void FreeChannel()
            {
                usage = 0;
                tws = null;
                missile = null;
                lockTime = 0;
            }

            public bool IsSpotting()
            {
                return tws != null && usage == 1;
            }
            public bool IsGuiding()
            {
                return tws != null && usage == 1;
            }
            public TrackFileTWS? GetTWS()
            {
                return tws;
            }
            public MissileController? GetMissile()
            {
                return missile;
            }

        }

        Utils utils;
        private Transform transform;
        private RadarState radar;

        private List<TrackFileTWS> twsQueue;
        private TrackFileTWS? twsChannel;
        private int twsChannelCount; // number of datapoint collected
        private MissileController? missileChannel;

        private DebugLine twsLine;
        private DebugLine missileLine;

        //private List<ChannelTWS> channels;

        public RadarControllerTWS(Utils utilities)
        {
            utils = utilities;
            transform = new Transform();

            twsQueue = new List<TrackFileTWS>();
            twsChannel = null;
            missileChannel = null;
            //channels = new List<ChannelTWS>();
            //for (int i = 0; i < 4; i++)
            //{
            //    channels.Add(new ChannelTWS(i, utils.MakeLine()));
            //}

            twsLine = utils.MakeLine(255, 40, 200);
            missileLine = utils.MakeLine(200, 100, 100);
        }

        public List<int> Update(OutboundState state)
        {
            List<int> actions = new List<int>();
            radar = state.radar;
            transform.position = state.kinematics.position;
            transform.rotation = state.kinematics.rotation;

            UpdateQueue(state.time);
            actions.AddRange(UpdateSpotting(state.time));

            if (missileChannel != null)
            {
                missileLine.start = transform.position;
                missileLine.end = missileChannel.GetTargetPos();
                utils.Draw(missileLine);
            }
            else
            {
                //utils.Erase(missileLine.id);
            }

            //utils.Graph("twsQueue count", twsQueue.Count);

            return actions;
        }


        private void UpdateQueue(float t)
        {
            twsQueue.RemoveAll(known => known.IsTimedOut(t)); // remove dead tracks

            foreach (MinimalDetectedTargetData contact in radar.detectedTargets)
            {
                if (utils.IsAllied(contact.team)) { continue; } // skip friendlies

                if (contact.team == Team.Unknown) { continue; } // skip own missile

                if (!twsQueue.Any(known => known.id == contact.id)) // new contact
                {
                    twsQueue.Add(new TrackFileTWS(contact));
                }

            }
        }

        private List<int> UpdateSpotting(float t)
        {
            List<int> actions = new List<int>();

            if (twsChannel != null) // channel is used
            {
                bool isFound = false;
                foreach (StateTargetData tws in radar.twsedTargets)
                {
                    if (tws.id != twsChannel.id) continue;
                    isFound = true;

                    // COLLECT DATA ON CURRENT TWS
                    twsChannel.UpdateScan(tws, t);
                    twsChannelCount++;

                    twsLine.end = twsChannel.twsData.position;
                    twsLine.start = transform.position;
                    utils.Draw(twsLine);

                    break;
                }

                if (!isFound) // cannot find target in radar tws list. Lock lost.
                {
                    twsChannel.FailedScan(t);
                    actions.Add((int)InboundAction.RadarDropTWS);
                    actions.Add(twsChannel.id);
                    twsChannel = null;
                    twsChannelCount = 0;
                }

                // enough data collected, change lock
                else if (twsChannelCount >= TrackFileActive.SAMPLE_COUNT)
                {
                    // free the tws channel
                    actions.Add((int)InboundAction.RadarDropTWS);
                    actions.Add(twsChannel.id);

                    twsChannel = null;
                    twsChannelCount = 0;
                }
            }

            // sort the tws queue
            if (twsChannel == null && twsQueue.Count >= 2)
            {
                twsQueue = twsQueue.OrderByDescending(known => known.Age(t)).ToList();
            }

            // request a new tws
            if (twsChannel == null)
            {
                foreach (TrackFileTWS known in twsQueue)
                {
                    if ((missileChannel?.targetId ?? -1) == known.id) // check the ID is not currently under missile guidance
                    {
                        continue; // target already under TWS from missile guidance
                    }

                    if (radar.detectedTargets.Any(tws => tws.id == known.id))
                    {
                        twsChannel = known;

                        actions.Add((int)InboundAction.RadarTWS);
                        actions.Add(known.id);

                        break;
                    }
                    else // front of cue is not visible on radar
                    {
                        // push them back to end of queue?
                        // no, since we've not seen them in a while, we ranna get update ASAP
                    }
                }
            }

            return actions;
        }


        public List<int> AttackTarget(OutboundState state, int id)
        {
            List<int> actions = new List<int>();

            //utils.Log("AttackTarget()");

            if (missileChannel != null && missileChannel.targetId != id)
            {
                // missile channel is already used for another target

                // for now lets just forcefully drop lock and switch to the new target

                actions.Add((int)InboundAction.RadarDropTWS);
                actions.Add(missileChannel.targetId);
                missileChannel = null;
            }

            if (missileChannel == null) // missile channel is free
            {
                if (twsChannel != null && twsChannel.id == id) // currently under AESA, drop that lock
                {
                    // aesa will try to drop the TWS lock once it's done, so must drop it now to avoid breaking the track
                    actions.Add((int)InboundAction.RadarDropTWS);
                    actions.Add(twsChannel.id);

                    twsChannel.FailedScan(state.time); // move to end of queue
                    twsChannel = null;
                    twsChannelCount = 0;
                }


                if (twsQueue.Any(known => known.id == id))
                {
                    //utils.Log("requesting TWS - ID (" + id.ToString() + ") exist");
                }
                else
                {
                    utils.Log("requesting TWS - ID (" + id.ToString() + ") DOES NOT exist");
                }

                TrackFileTWS? target = twsQueue.Find(known => known.id == id);
                if (target != null)
                {
                    utils.Log("requesting TWS on ID " + id.ToString());

                    missileChannel = new MissileController(utils, state, target);

                    actions.Add((int)InboundAction.RadarTWS);
                    actions.Add(id);

                    actions.Add((int)InboundAction.RadarSetPDT);
                    actions.Add(0);
                }

            }
            else if (missileChannel != null) // already guiding, on the right target
            {
                //utils.Log("maybe trying to fire missile");

                // check if we lost lock
                if (radar.twsedTargets.Any(contact => contact.id == id))
                {
                    // track still there, update it
                    StateTargetData twsData = radar.twsedTargets.ToList().Find(contact => contact.id == id);
                    missileChannel.UpdateTrack(twsData, state.time);

                    // fire missile
                    actions.AddRange(missileChannel.Fire(state));
                }
                else if (missileChannel.GetTimeout() > 2f)
                {
                    // no lock for more than 2s
                    utils.Log("Missile guidance lock lost against ID " + missileChannel.targetId.ToString());

                    missileChannel = null;
                }
            }

            return actions;
        }
    }
}
