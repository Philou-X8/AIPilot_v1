using AIPProvider.src.Radar;
using AIPProvider.src.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityGERunner;
using UnityGERunner.UnityApplication;

namespace AIPProvider.src.Outdated
{
    /* 1 Missile Controller is dedicated to 1 target
     *      the dedicated target is masked out of the list of target to AESA
     * The missile controller request and use up a TWS Channel
     *      this TWS Channel should provide an TrackFileActive object (for gpull info)
     * The missile controller keeps track of all the missiles it has fired
     * It should free itself and the used TWS channel if it lose lock or the target dies
     */
    internal class MissileController
    {
        // maybe make a ChannelTWS template and use it both for 
        private Utils utils;

        private TrackFileTWS twsTrack;
        private List<FiredMissile> missileList;
        private float fireTime;
        private float lockTimeout;
        private int twsChannelId;

        private const float RIPPLE_DELAY = 3f;

        public MissileController(Utils utilities, TrackFileTWS tws, int ch, OutboundState state)
        {
            utils = utilities;
            twsTrack = tws;
            twsChannelId = ch;
            missileList = new List<FiredMissile>();
            fireTime = -RIPPLE_DELAY;
            lockTimeout = tws.TimeOut(state.time);
        }
        public MissileController(Utils utilities, OutboundState state, TrackFileTWS tws)
        {
            utils = utilities;
            twsTrack = tws;
            twsChannelId = 0;
            missileList = new List<FiredMissile>();
            fireTime = -RIPPLE_DELAY;
            lockTimeout = tws.TimeOut(state.time);
        }

        public int targetId
        {
            get { return twsTrack.id; }
        }

        public void UpdateTrack(StateTargetData data, float t)
        {
            twsTrack.UpdateScan(data, t);
            lockTimeout = twsTrack.TimeOut(t);
        }
        public float GetTimeout()
        {
            return lockTimeout;
        }

        public List<int> Fire(OutboundState state)
        {
            List<int> actions = new List<int>();

            if (state.time - fireTime < RIPPLE_DELAY) // fired recently, dont fire again
            {
                return actions;
            }

            // TODO: replace with a for(i) loop , to replace the twsedTargets.FindIndex( )
            if (state.radar.twsedTargets.Any(contacts => contacts.id == twsTrack.id)) // check if tws is still there
            {


                //int hardpoint = utils.hardpoint.GetRadarHardpoint();

                if (utils.hardpoint.TryGetRadarHardpoint(out int hardpoint))
                {
                    utils.Log("SHOOTING MISSILE from hardpoint " + hardpoint.ToString());

                    missileList.Add(new FiredMissile(state.time, twsTrack.id, state.kinematics.position));

                    int pdtIndex = state.radar.twsedTargets.ToList().FindIndex(contacts => contacts.id == twsTrack.id);
                    utils.Log("RadarSetPDT index = " + pdtIndex.ToString());

                    actions.Add((int)InboundAction.RadarSetPDT);
                    actions.Add(pdtIndex);
                    actions.Add((int)InboundAction.SelectHardpoint);
                    actions.Add(hardpoint);
                    actions.Add((int)InboundAction.Fire);

                    fireTime = state.time;
                }


            }
            return actions;
        }

        public Vector3 GetTargetPos()
        {
            return twsTrack.twsData.position;
        }
    }

    internal class FiredMissile
    {
        private float launchTime;
        private int targetId;

        private Vector3 launchPos;

        public FiredMissile(float t, int id, Vector3 pos)
        {
            launchTime = t;
            targetId = id;
            launchPos = pos;
        }

        //public float ExpectedTime(Vector3 destination)
        //{

        //    float dist = (destination - launchPos).magnitude;
        //}
    }
}
