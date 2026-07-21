using Recorder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityGERunner.UnityApplication;
using UnityGERunner;
using AIPProvider.src.Utilities;
using System.Collections;

namespace AIPProvider.src.Outdated
{
    internal class RadarController
    {
        Utils utils;
        private Transform transform;
        private RadarState radar;
        private DebugLine scanLine;
        private DebugLine sttLine;
        private DebugLine twsLine;
        private bool isRadarOn;

        public RadarController(Utils utilities)
        {
            utils = utilities;
            transform = new Transform();
            radar = new RadarState();
            isRadarOn = false;

            scanLine = utils.MakeLine();
            scanLine.color = new NetColor(1.0f, 0.7f, 0.7f, 1);
            sttLine = utils.MakeLine();
            sttLine.color = new NetColor(1.0f, 0.3f, 0.3f, 1);
            twsLine = utils.MakeLine();
            twsLine.color = new NetColor(0.9f, 0.3f, 1.0f, 1);

        }

        public List<int> Update(OutboundState state)
        {
            List<int> actions = new List<int>();

            transform.position = state.kinematics.position;
            transform.rotation = state.kinematics.rotation;
            radar = state.radar;

            if (!isRadarOn)
            {
                utils.Log("Turning radar ON");
                actions.Add(0); // radar power control
                actions.Add(1); // radar power state selection 
                isRadarOn = true;
            }

            //actions.AddRange(AutoSTT());
            //AutoSTT(actions);

            UpdateTWS(state, actions);
            foreach (TWSUnit known in twsQueue)
            {
                if (known.lastTWS > 0)
                {
                    known.PredictPosition(state.time);
                }
            }
            //DrawTWS();

            //DrawScanLine();

            twsLine.start = transform.position;
            utils.Draw(twsLine);



            return actions;
        }

        private void AutoSTT(List<int> actions)
        {
            if (radar.sttedTarget == null) // no target are locked
            {
                if (radar.detectedTargets.Length > 0) // attempt to lock target
                {
                    actions.Add(1);
                    actions.Add(radar.detectedTargets[0].id);
                }
                // no target on radar


            }

            // target is locked already

            DrawSTT(radar.sttedTarget?.position);
        }

        private void DrawSTT(Vector3? target)
        {
            if (target == null)
            {
                sttLine.start = transform.position;
                sttLine.end = transform.position;
                utils.Draw(sttLine);
            }
            else
            {
                sttLine.start = transform.position;
                Vector3 sttVect = (target ?? transform.position) - transform.position;
                sttLine.end = transform.position + sttVect * 0.8f;
                utils.Draw(sttLine);
            }
        }

        private void DrawScanLine()
        {

            Vector3 rdrVect = new Vector3(
                (float)Mathf.Sin(Mathf.Deg2Rad * radar.angle),
                0,
                (float)Mathf.Cos(Mathf.Deg2Rad * radar.angle)
                );
            Vector3 rdrAbsolue = transform.TransformVector(rdrVect) * 1000; //change scale from normalized

            //Transform world = new Transform();
            //Vector3 worldPosition = new Vector3();
            //Quaternion worldRotation = new Quaternion();
            //world.GetGlobalTransform(out worldPosition, out worldRotation);
            //world.position = worldPosition; world.rotation = worldRotation;
            //Vector3 temp = transform.InverseTransformVector(rdrAbsolue);

            //rdrAbsolue = transform.TransformVector(temp);
            //rdrAbsolue = world.InverseTransformVector(temp);

            scanLine.start = transform.position;
            scanLine.end = transform.position + rdrAbsolue;
            utils.Draw(scanLine);
        }


        private List<TWSUnit> twsQueue = new List<TWSUnit>();
        private List<TWSUnit> twsSlots = new List<TWSUnit>();

        private TWSUnit? currentTWS = null;
        private float currentTimeout = 0;
        //private DebugLine currentSphere;
        private int frameCollected = 0;

        private void UpdateTWS(OutboundState state, List<int> actions)
        {
            foreach (MinimalDetectedTargetData contact in radar.detectedTargets)
            {
                if (utils.IsAllied(contact.team)) { continue; } // skip friendlies

                if (twsQueue.Any(known => known.id == contact.id)) // check if the unit is known
                {
                    TWSUnit? unit = twsQueue.Find(known => known.id == contact.id);
                    if (unit != null) { unit.contactData = contact; }
                }
                else // this is a new contact
                {
                    DebugLine line = utils.MakeLine();
                    line.start = transform.position;
                    line.end = transform.position;
                    line.color = new NetColor(0.4f, 0.1f, 0.8f, 1);
                    twsQueue.Add(new TWSUnit(contact, line)); // add new contact to 
                    //twsQueue.Add(new TWSUnit(contact, sphere)); // add new contact to 
                }

                //twsQueue.Find(known => known.id == contact.id);
                //twsQueue.Select(known => known.id);
                //twsQueue.Any(known => known.id == contact.id);
            }

            if (currentTWS == null && twsQueue.Count >= 2) // only sort if there's free TWS slot, we're not checking the queue if not anyway
            {
                // sort to put the oldest TWS first
                twsQueue = twsQueue.OrderByDescending(known => known.Age(state.time)).ToList();
            }

            //currentSphere.end = transform.position;

            if (currentTWS == null) // TWS is idle
            {
                frameCollected = 0;

                // go throught the list of known contact until we find one that's also on radar
                for (int i = 0; i < twsQueue.Count; i++)
                {
                    // TODO: [continue;] if the id is used by the missile guidance


                    // make sure we actually see the TWS we want
                    if (radar.detectedTargets.Any(tws => tws.id == twsQueue[i].id))
                    {
                        currentTWS = twsQueue[i];
                        currentTimeout = state.time;
                        actions.Add(3); // RadarTWS
                        actions.Add(currentTWS.id); // Actor ID
                        //utils.Log("trying to get a TWS on id: " + currentTWS.id.ToString());

                        if (currentTWS.twsData.position != Vector3.zero) twsLine.end = currentTWS.predictionData.position;
                        else twsLine.end = transform.position;

                        break;
                    }
                }


            }
            else if (radar.twsedTargets.Any(tws => tws.id == currentTWS.id)) // check if TWS successful
            {
                bool twsFound = false;


                //StateTargetData lockData = radar.twsedTargets.Find(known => (StateTargetData)known.id == currentTWS.id);
                foreach (StateTargetData tws in radar.twsedTargets) // look for our TWS data
                {
                    if (tws.id != currentTWS.id) continue;

                    // collect TWS data
                    currentTWS.lastTWS = state.time;
                    currentTWS.twsData = tws;
                    twsFound = true;

                    frameCollected++;
                    //utils.Log("Lock obtained on id: " + currentTWS.id.ToString());
                    //utils.Log("Position Z: " + tws.position.z.ToString());

                    twsLine.end = currentTWS.twsData.position;
                    //currentTWS.lastKnown.start = transform.position;
                    //currentTWS.lastKnown.end = tws.position; // update debug sphere
                    //utils.Draw(currentTWS.lastKnown);

                    //currentSphere.end = tws.position;

                    // TODO: collect data for a few frames for better interpolation in the future
                }

                if (!twsFound) // for some reason ID didn't match
                {
                    if (state.time - currentTimeout > 5)
                    {
                        // TODO maybe
                    }
                }

                if (frameCollected > 1)
                {

                    // TODO: wait a few game updates before droping lock
                    actions.Add(4); // RadarDropTWS
                    actions.Add(currentTWS.id); // Actor ID
                    currentTWS.lastTWS = state.time;
                    currentTWS = null;
                    currentTimeout = state.time;
                }

            }
            else if (state.time - currentTimeout > 0.2f)
            {
                if (currentTWS.twsData.position != Vector3.zero) twsLine.end = currentTWS.predictionData.position;
                else twsLine.end = transform.position;


                // Tried to TWS for X seconds, give up
                //utils.Log("TWS timeout on id: " + currentTWS.id.ToString());

                //twsQueue.RemoveAll(known => known.id == currentTWS.id);
                currentTWS.lastTWS = state.time;
                currentTWS = null;
                currentTimeout = state.time;

                frameCollected = 0;
            }

        }

        private void DrawTWS()
        {
            foreach (TWSUnit tws in twsQueue)
            {
                if (tws.twsData.position != Vector3.zero)
                {
                    tws.lastKnown.start = transform.position;
                    tws.lastKnown.end = tws.predictionData.position;
                    utils.Draw(tws.lastKnown);
                }
            }
        }


        internal class TWSUnit
        {
            public int id;
            public MinimalDetectedTargetData contactData { get; set; }

            public float validScanTime; // could be used to identify dead tracks
            public float attemptScanTime; // to be used to sort the queue
            public float lastTWS;

            public StateTargetData twsData { get; set; }
            public StateTargetData predictionData;

            public DebugLine lastKnown { get; set; }


            public TWSUnit(MinimalDetectedTargetData tws)
            {
                id = tws.id;
                contactData = tws;
                lastTWS = tws.detectedTime;
                lastTWS = 0;
            }
            public TWSUnit(MinimalDetectedTargetData tws, DebugLine shere)
            {
                id = tws.id;
                contactData = tws;
                lastTWS = tws.detectedTime;
                lastTWS = 0; // new contact, we gotta inspect him asap
                lastKnown = shere;
            }

            public float Age(float currentTime)
            {
                return currentTime - lastTWS;
            }

            public void PredictPosition(float t)
            {

                float dt = t - lastTWS;
                if (dt > 10) return;
                predictionData.position = (Vector3)twsData.position + (Vector3)twsData.velocity * dt;

            }

        }
    }

}
