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

namespace AIPProvider.src.Radar.TWS
{
    internal enum ChannelType
    {
        available, // idle
        spotting,
        guiding,
    }

    internal struct GuidanceParameter
    {
        public int id;
        public float queueTime; // time added to queue

        public float launchTime;
        public float impactTime; // predicted impact time
        public float flightDuration;

        //public float expirationTime;
        public Vector3 impactLocation;
        public int missileCount;
    }

    internal class RadarChannel
    {
        private Utils utils;

        private TrackFileTWS track;
        private ChannelType type;

        public int pdt;
        private GuidanceParameter missile;

        private DebugLine line;

        private int spotTicks; // used for spotting channels
        private int spotLimit = 10;



        public RadarChannel(Utils u, int channelId)
        {
            utils = u;
            pdt = channelId;

            track = new TrackFileTWS(new MinimalDetectedTargetData());
            type = ChannelType.available;
            line = utils.MakeLine();
        }

        public int id => track.id;

        // check if the channel is free and available to be used
        public bool IsAvailable()
        {
            return type == ChannelType.available;
        }

        public void Aquire(TrackFileTWS trackFile, ChannelType channelType, ref List<int> actions)
        {
            track = trackFile;
            type = channelType;
            spotTicks = 0;

            if(type == ChannelType.spotting)
            {
                line.color = ColorMap.Convert(ColorMap.RadarSpot);
            }
            else if (type == ChannelType.guiding)
            {
                line.color = ColorMap.Convert(ColorMap.RadarGuide);
            }

            actions.Add((int)InboundAction.RadarTWS);
            actions.Add(track.id);

        }

        public void Update(OutboundState state, ref List<int> actions)
        {
            //List<int> actions = new List<int>();

            if (type == ChannelType.available) return;

            FindData(state.radar, out bool isLocked, out StateTargetData tws);

            if(isLocked)
            {
                switch(type)
                {
                    case ChannelType.available:
                        break;
                    case ChannelType.spotting:
                        UpdateSpot(tws, state.time);
                        break;
                    case ChannelType.guiding:
                        UpdateGuide(state, tws, state.time, ref actions);
                        break;
                }
            }
            else
            {
                //actions.AddRange(UnlockRadar()); // TWS is already lost, no point in dropping it...
                QuickRelease();
            }

            // TODO: return actions
        }

        public void DrawLine(Vector3 origin)
        {
            if (type == ChannelType.available)
            {
                // line should be erased when the track is lost
                line.start = origin;
                line.end = origin;
            }
            else
            {
                line.start = origin;
                utils.Draw(line);
            }
            
        }

        // should be overwritten by the channel type
        public bool IsTimedOut(RadarState radar, float t)
        {
            bool runTimeoutRoutine = type switch
            {
                ChannelType.available => false, // broken lock already detected when running Update(). State already changed.
                ChannelType.spotting => SpotTimedOut(),
                ChannelType.guiding => GuideTimedOut(t),
                _ => false
            };

            if (runTimeoutRoutine)
            {
                QuickRelease();
            }

            return runTimeoutRoutine; // TODO: replace with Actions List
        }

        public void QuickRelease()
        {
            track.Release();
            type = ChannelType.available;
            utils.Erase(line.id);
        }

        public void UnlockRadar(RadarState radar, ref List<int> actions)
        {
            if (radar.twsedTargets.Any(locks => locks.id == track.id)) // only drop lock if target is locked
            {
                actions.Add((int)InboundAction.RadarDropTWS);
                actions.Add(track.id);
            }
        }

        private void FindData(RadarState radar, out bool isLocked, out StateTargetData tws)
        {
            isLocked = false;
            tws = StateTargetData.invalid;
            pdt = -1;

            if (track == null) return;
            
            foreach(StateTargetData contact in radar.twsedTargets)
            {
                pdt++;
                if (contact.id == track.id)
                {
                    isLocked = true;
                    tws = contact;
                    return;
                }
            }
            pdt = -1;
        }

        private bool StillLocked(RadarState radar)
        {
            if (track == null) return false;

            return radar.twsedTargets.Any(tws => tws.id == track.id);
        }

        // -----------------------------------------------------------
        // Spotting implementation
        // -----------------------------------------------------------
        private void UpdateSpot(StateTargetData tws, float t)
        {
            track.UpdateScan(tws, t);
            spotTicks++;

            line.end = tws.position;
        }
        private bool SpotTimedOut()
        {
            return spotTicks >= spotLimit;
        }


        // -----------------------------------------------------------
        // Guiding implementation
        // -----------------------------------------------------------
        public void SetMissileParams(GuidanceParameter param, float t)
        {
            missile = param;
            missile.launchTime = -5f;
            missile.impactTime = t + missile.flightDuration;
            //flightDuration = param.expirationTime;
            //launchTime = t;
            //impactTime = launchTime + flightDuration;
        }

        private void UpdateGuide(OutboundState state, StateTargetData tws, float t, ref List<int> actions)
        {
            track.UpdateScan(tws, t);

            if(missile.missileCount > 0 && (t - missile.launchTime) > 3f)
            {
                if (HardpointManager.TryGetRadarMissile(state.weapons, out int hardpoint))
                {
                    // run weapon launch
                    missile.missileCount--;

                    missile.launchTime = t;
                    missile.impactTime = t + missile.flightDuration;

                    actions.Add((int)InboundAction.RadarSetPDT);
                    actions.Add(pdt);
                    actions.Add((int)InboundAction.SelectHardpoint);
                    actions.Add(hardpoint);
                    actions.Add((int)InboundAction.Fire);

                    line.end = missile.impactLocation;
                }
            }
        }

        private bool GuideTimedOut(float t)
        {
            return t > missile.impactTime && missile.missileCount <= 0;
            //return lockTime >= guideLimit;
        }
    }
}
