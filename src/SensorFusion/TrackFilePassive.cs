using AIPProvider.src.Radar.TWS;
using AIPProvider.src.Visual;
using Recorder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityGERunner;
using UnityGERunner.UnityApplication;

namespace AIPProvider.src.SensorFusion
{
    internal enum PassiveTrackState
    {
        Lost, // lost or invalid

        ActiveStrong, // currently updating, can apply strong prediction
        ActiveWeak, // currently updating, but not enough data for prediction
        LostStrong, // not under updates, can apply strong prediction
        LostWeak, // lost track, not enough data for prediction

        // Immobile // track type which doesn't give Velocity (for weak path prediction)

        // bearing only
        // direction only
    }

    internal class TrackFilePassive
    {
        public int id;

        private DebugLine beacon;

        private PassiveTrackState state;
        private float lastUpdateTime = 0f;

        // data from active track
        private Transform radarTransform;
        private Vector3 radarVelocity;
        private Quaternion radarAngular; // angular velocity // turning speed
        private float radarUpdateTime = 0f;

        // data from visual track
        // ...
        private Transform visualTransform;
        private Vector3 visualVelocity;
        private Quaternion visualAngular; // angular velocity // turning speed
        private float visualUpdateTime = 0f;

        // data from bearing track
        // ...


        // pathing and prediction data
        private KeyPath path;
        private float pathCreationTime = 0f;

        // combat history data
        private float missileLaunchRadar = 0f;
        private int missileLaunchSpam = 1; // historical number of missile fired 

        private TrackFilePassive(DebugLine refBeacon)
        {
            radarTransform = new Transform();
            radarVelocity = new Vector3();
            radarAngular = new Quaternion();
            visualTransform = new Transform();
            visualVelocity = new Vector3();
            visualAngular = new Quaternion();

            path = new KeyPath();
            beacon = refBeacon;
        }
        public TrackFilePassive(ActivePassiveCarrier carrier, DebugLine refBeacon) : this(refBeacon) 
        {
            id = carrier.id;

            MakeFromActive(carrier);

        }
        public TrackFilePassive(VisualPassiveCarrier carrier, DebugLine refBeacon) : this(refBeacon)
        {
            id = carrier.id;

            MakeFromVisual(carrier);

        }

        public void MakeFromActive(ActivePassiveCarrier carrier)
        {
            // if (IDs are matching) ...
            if (carrier.updateTime - lastUpdateTime > 1f) // last radar update was long ago
            {
                pathCreationTime = 0; // mark path ready for update // overwrite lower quality path
            }

            lastUpdateTime = carrier.updateTime;
            radarUpdateTime = carrier.updateTime;
            state = carrier.passiveState;
            radarTransform = carrier.trail;
            radarVelocity = carrier.trailVel;

            radarAngular = new Quaternion(); // placeholder angular speed, TODO

            // convert the other data types (like direction, range, bearing)
        }
        
        public void MakeFromVisual(VisualPassiveCarrier carrier)
        {
            // if (IDs are matching) ...

            //if (carrier.updateTime < (lastUpdateTime)) // TEMP
            //{
            //    return; // TEMP
            //}

            if (carrier.updateTime > (lastUpdateTime + 1.0f))
            {
                // radar track lost for more than 1s, rely on visual track instead

                // TODO, maybe move this to an update function (that will check all data source at once)
            }

            visualUpdateTime = carrier.updateTime;
            //state = PassiveTrackState.ActiveWeak;
            visualTransform = carrier.position;
            visualVelocity = carrier.velocity;

            visualAngular = new Quaternion(); // placeholder angular speed, TODO

            // convert the other data types (like direction, range, bearing)
        }

        // updates the prediction path
        public void Update(float t)
        {


            UpdatePredicitonPath(t);
        }

        public DebugLine GetBeacon()
        {
            beacon.start = (radarUpdateTime + 1f > visualUpdateTime) ? radarTransform.position : visualTransform.position;
            beacon.end = beacon.start + new Vector3(0, 1000, 0);
            return beacon;
        }

        private void UpdatePredicitonPath(float t)
        {
            if (t - pathCreationTime > 0.1f)
            {
                Vector3 activePos = (radarUpdateTime + 1f > visualUpdateTime) ? radarTransform.position : visualTransform.position;
                Vector3 activeVel = (radarUpdateTime + 1f > visualUpdateTime) ? radarVelocity : visualVelocity;

                //if (radarUpdateTime > visualUpdateTime)
                //{
                //    utils
                //}

                path = new KeyPath(activePos, activeVel, 5f, 60f);
                pathCreationTime = t;
            }
        }

        public bool TryGetMissileRadar(float t, Vector3 originPos, out GuidanceParameter launch)
        {
            if (t - missileLaunchRadar < 15)
            {
                launch = new GuidanceParameter();
                return false;
            }
            float flightTime = path.TimedDistance(originPos, 600f);
            if (flightTime > 30f)
            {
                launch = new GuidanceParameter();
                return false;
            }

            launch = new GuidanceParameter()
            {
                id = this.id,
                queueTime = t,
                flightDuration = flightTime,
                impactLocation = InterceptionPoint(flightTime, t),
                missileCount = 1,

            };
            missileLaunchRadar = t;
            missileLaunchSpam++;
            return true;
        }

        public float TimedDistance(Vector3 pos, Vector3 vel)
        {
            return path.TimedDistance(pos, vel.magnitude);
        }

        public Vector3 InterceptionPoint(float elapsedTime)
        {
            return path.PointAt(elapsedTime);
        }
        public Vector3 InterceptionPoint(float elapsedTime, float currentTime)
        {

            return path.PointAt(elapsedTime + (currentTime - pathCreationTime));
        }

        //public Vector3 GetFuturePosition(float futureTime)
        //{
        //    // TODO adapt data based on track type/state

        //    if(futureTime < lastUpdateTime)
        //    {
        //        return radarTransform.position;
        //    }
        //    float dt = futureTime - lastUpdateTime;

        //    Vector3 trajectory = radarTransform.position + (radarVelocity * dt);
        //    // TODO: check if trajectory ends up underground

        //    return radarTransform.position + (radarVelocity * dt);
        //}


        // MakePath()
        // have a way to compute the predicted track when scan data gets updated
        // should be lower fidelity, but longer distance than the gun pipper
        // try to account for terrain

        // GetFuturePos()
        // return future position if track type allow for path, otherwise just last known position

        private PassiveTrackState TrackStateConverter(ActiveTrackState active)
        {
            switch (active)
            {
                case ActiveTrackState.adding:
                    return PassiveTrackState.ActiveStrong;

                case ActiveTrackState.tracking:
                    return PassiveTrackState.ActiveWeak;

                case ActiveTrackState.ready:
                case ActiveTrackState.completed:
                    return PassiveTrackState.LostStrong;

                case ActiveTrackState.pending:
                case ActiveTrackState.lost:
                    return PassiveTrackState.LostWeak;

                default:
                    return PassiveTrackState.Lost;
            }
        }
    }

}
