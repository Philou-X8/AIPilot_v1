using AIPProvider.src.Radar.STT;
using AIPProvider.src.Radar.TWS;
using AIPProvider.src.Radar;
using AIPProvider.src.RWR;
using AIPProvider.src.SensorFusion;
using AIPProvider.src.Terrain;
using AIPProvider.src.Utilities;
using AIPProvider.src.Visual;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityGERunner;
using UnityGERunner.UnityApplication;
using System.Collections;

namespace AIPProvider.src.Master.States
{
    internal class CoreTesting
    {
        private Utils utils;
        private GroundUtils gUtils;
        private SetupInfo simInfo;
        private OutboundState simData;
        //-------------------------------------------------
        private bool pendingSetup = true;
        // data classes
        private ActiveDataCollector activeTracker;
        private PassiveDataManager passiveTracker;
        private ControllerTWS radarTWS;
        private DeathHistory deads;
        private ControllerRWR rwr;

        // manouveur classes
        private Navigation flyNavigation;
        private Dogfight flyDogfight;

        // combat instruments
        private GunFunnel gun;

        // debugging and others
        private PathPredict pathPredict;

        //private Ratting ratPath;
        //public delegate bool LinecastDel<NetVector>(NetVector a, NetVector b, out NetVector collisionPoint); // TEMP testing

        private VisualSpotter visual;
        private AirstManager airst;

        public CoreTesting(Utils utilities, GroundUtils groundUtils)
        {
            utils = utilities;
            gUtils = groundUtils;

            activeTracker = new ActiveDataCollector(utils);
            passiveTracker = new PassiveDataManager(utils);
            radarTWS = new ControllerTWS(utils);
            deads = new DeathHistory(utils);
            rwr = new ControllerRWR(utils);

            flyNavigation = new Navigation(utils, gUtils);
            flyDogfight = new Dogfight(utils);

            gun = new GunFunnel(utils);

            pathPredict = new PathPredict(utils);

            //ratPath = new Ratting(utils, gUtils);
            visual = new VisualSpotter(utils);
            airst = new AirstManager(utils);

            //radarController = new RadarController(utils);
        }

        public SetupActions Start(SetupInfo info)
        {
            simInfo = info;

            return new SetupActions
            {
                hardpoints = utils.hardpoint.MakeHardpointList(),
                name = "id_" + info.id.ToString(),
            };
        }

        public InboundState Update(OutboundState state)
        {
            simData = state;

            SimActions output = new SimActions();

            if (pendingSetup) TakeoffSetup(ref output);

            DataCollection(ref output);
            CombatActions(ref output);
            Piloting(ref output);

            return output.Convert();
        }

        private void DataCollection(ref SimActions output)
        {
            List<int> deaths = deads.Deaths(simData);
            visual.RemoveDead(deaths);
            passiveTracker.RemoveDead(deaths);

            // collect data
            output.actions.AddRange(radarTWS.Update(simData, deaths));


            // update data
            List<ActivePassiveCarrier> tracks = activeTracker.GetTracks(simData);
            passiveTracker.UpdateFromActive(tracks);

            List<VisualPassiveCarrier> vis = visual.Update(simData);
            passiveTracker.UpdateFromVisual(vis);

            rwr.Update(simData);

            gun.Update(simData);


            passiveTracker.Update(simData);

        }

        private void CombatActions(ref SimActions output)
        {
            Transform transform = new Transform();
            transform.position = simData.kinematics.position;
            transform.rotation = simData.kinematics.rotation;
            passiveTracker.NearestWaypoint(transform, simData.kinematics.velocity, simData.time, out int targetId, out Vector3 checkpoint);
            if (targetId > 0 && (checkpoint - simData.kinematics.position).magnitude < 10000)
            {
                flyNavigation.ForceWaypoint(checkpoint);

                //output.lookDir = airst.TryFireAt(simData, passiveTracker.LocationOfId(targetId, simData.time), ref output.actions);

                //utils.Log("target within range, should turn");

            }
            List<GuidanceParameter> rdrMissileQueue = passiveTracker.GetMissileQueue(simData.time, simData.kinematics.position);
            radarTWS.AddMissileTargets(rdrMissileQueue);

            if(gun.TryGetFunnel(simData, ref output.actions, out Vector3 pipper))
            {
                flyNavigation.ForceWaypoint(pipper);
                utils.Log("pipper");
            }
        }

        private void Piloting(ref SimActions output)
        {

            // fly 
            InboundState navControls = flyNavigation.Update(simData);
            output.pyr = navControls.pyr;
            if (visual.ShouldGpull(out Vector3 pyr))
            {
                output.pyr = pyr;
                //output.pyr = new Vector3(-1, 0.5f, -0.2f);
            }

        }

        private void TakeoffSetup(ref SimActions output)
        {
            output.actions.Add((int)InboundAction.RadarState);
            output.actions.Add(1);
            output.actions.Add((int)InboundAction.SetUncage);
            output.actions.Add(1);

            pendingSetup = false;
        }
    }
}
