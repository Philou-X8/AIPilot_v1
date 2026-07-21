using AIPProvider.src.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityGERunner.UnityApplication;
using UnityGERunner;
using AIPProvider.src.SensorFusion;
using System.Collections;

namespace AIPProvider.src.Radar.TWS
{
    internal class ControllerTWS
    {
        private Utils utils;
        private PassiveDataManager dataCenter;

        private Transform transform;
        private RadarState radar;

        private List<TrackFileTWS> twsQueue;
        private List<GuidanceParameter> missileQueue;
        private List<RadarChannel> twsChannels;

        private OutboundState state; // must update every loop
        private List<int> actions; // must reset every loop

        public ControllerTWS(Utils utilities)
        {
            utils = utilities;
            transform = new Transform();

            twsQueue = new List<TrackFileTWS>();
            missileQueue = new List<GuidanceParameter>();
            twsChannels = new List<RadarChannel>();
            for (int i = 0; i < 4; i++)
            {
                twsChannels.Add( new RadarChannel(utilities, i));
            }

            actions = new List<int>();
        }

        public List<int> Update(OutboundState refState, List<int> deaths)
        {
            state = refState;
            transform.position = state.kinematics.position;
            transform.rotation = state.kinematics.rotation;
            radar = state.radar;
            actions = new List<int>(); // must reset action list every loop

            // data collection and managmeent
            AddQueuedTarget();
            RemoveDeadPlayers(deaths); // dead players might still show on radar
            UpdateActiveChannels();
            ReleaseTimedOutChannels();
            RemoveTimeoutTracks();

            SortQueue();
            // make combat actions
            AquireGuidanceChannels();
            AquireSpottingChannels();
            DrawChannelLines();

            return actions;
        }

        /// <summary>
        /// Add missile target to queue
        /// </summary>
        /// <param name="targets"></param>
        public void AddMissileTargets(List<GuidanceParameter> targets)
        {
            foreach(GuidanceParameter target in targets)
            {
                int index = missileQueue.FindIndex(msl => msl.id == target.id);
                if(index < 0)
                {
                    utils.Log("added missile to queue");
                    missileQueue.Add(target);
                }
                else
                {
                    missileQueue[index] = target;
                }
            }
        }

        /// <summary>
        /// Phase 1: collect environement data
        /// </summary>
        /// <returns></returns>
        public List<int> CollectData()
        {


            return actions;
        }
        
        /// <summary>
        /// Phase 2: make combat decisions
        /// </summary>
        /// <returns></returns>
        public List<int> MakeDecision()
        {


            return actions;
        }

        // global: make/update Missile Target queue
        // channels: run data collection and missile guidance
        // channels: free timed out channels
        // global: remove dead players from queues and lists
        // channels: try to assign Target queue to open channels
        // channels: try to assign AESA queue to open channels
        // draw new debug lines

        /// <summary>
        /// add new radar contact to the TWS queue
        /// </summary>
        public void AddQueuedTarget()
        {
            foreach (MinimalDetectedTargetData contact in radar.detectedTargets)
            {
                if (utils.IsAllied(contact.team)) { continue; } // skip friendlies
                if (contact.team == Team.Unknown) { continue; } // skip own missile

                if (!twsQueue.Any(known => known.id == contact.id)) // new contact
                {
                    utils.Log("found a new radar contact");
                    twsQueue.Add(new TrackFileTWS(contact));
                }
            }


        }

        /// <summary>
        /// remove killed players from waiting lists and queue
        /// </summary>
        /// <param name="deads"></param>
        private void RemoveDeadPlayers(List<int> deads)
        {
            if (deads.Count == 0) return;

            foreach (int dead in deads)
            {
                // remove from TWS
                foreach (RadarChannel channel in twsChannels)
                {
                    if (channel.id == dead)
                    {
                        channel.UnlockRadar(radar, ref actions);
                        channel.QuickRelease();
                    }

                }
                // remove from spotting queue
                twsQueue.RemoveAll(known => known.id == dead);
                // remove from missile queue
                missileQueue.RemoveAll(known => known.id == dead);
            }
        }

        private void UpdateActiveChannels()
        {
            foreach(RadarChannel channel in twsChannels)
            {
                channel.Update(state, ref actions);
                // this might fire missile
            }
        }

        private void ReleaseTimedOutChannels()
        {
            foreach (RadarChannel channel in twsChannels)
            {
                bool timeout = channel.IsTimedOut(radar, state.time);
                if (timeout)
                {
                    channel.UnlockRadar(radar, ref actions);
                }
            }
            // also check for dead players in used channels?
        }


        private void RemoveTimeoutTracks()
        {
            twsQueue.RemoveAll(known => known.IsTimedOut(state.time)); // remove dead tracks
        }

        private void SortQueue()
        {

            // sort the tws queue
            if (twsQueue.Count >= 2)
            {
                twsQueue = twsQueue.OrderByDescending(known => known.Age(state.time)).ToList();
            }
        }

        private void AquireGuidanceChannels()
        {
            // check for Missile in queue
            // // check if corresponding TrackFile is available
            // // check if there's a free TWS channel
            // // check if target is visible on radar

            //GuidanceParameter;
            //RadarChannel;
            //TrackFileTWS;

            for(int i = missileQueue.Count - 1; i >= 0; i--)
            {
                GuidanceParameter msl = missileQueue[i];

                RadarChannel? channel = twsChannels.Find(ch => ch.IsAvailable());
                TrackFileTWS? track = twsQueue.Find(known => (known.id == msl.id) && known.Available() );

                if ((channel != null && track != null) && radar.detectedTargets.Any(tws => tws.id == msl.id))
                {


                    track.Aquire();
                    channel.Aquire(track, ChannelType.guiding, ref actions);

                    channel.SetMissileParams(msl, state.time);
                    missileQueue.RemoveAt(i);

                }
            }


            // SetMissileParams()
        }


        private void AquireSpottingChannels()
        {
            int usedChannel = 0;
            twsChannels.ForEach(channel => usedChannel += (channel.IsAvailable() ? 0 : 1));
            if(usedChannel >= 3)
            {
                // 3 or more channels are already under use
                // dont assign any more Spotting to avoid killing the scanning speed
                return;
            }

            
            
            foreach (RadarChannel channel in twsChannels)
            {
                if ( !channel.IsAvailable() || usedChannel >= 3)
                {
                    continue;
                }

                //utils.Log("free TWS channel");

                foreach (TrackFileTWS known in twsQueue)
                {
                    if ( !known.Available() )
                    {
                        continue; // already under TWS
                    }

                    if (radar.detectedTargets.Any(tws => tws.id == known.id))
                    {
                        //utils.Log("Aquired target " +  known.id);

                        // target visible to the radar
                        known.Aquire(); // the check for availability was done with [ known.Available() ]

                        channel.Aquire(known, ChannelType.spotting, ref actions);

                        usedChannel++;
                        break;
                    }
                }
            }
        }

        private void DrawChannelLines()
        {
            twsChannels.ForEach(channel => channel.DrawLine(transform.position));
        }
    }
}
