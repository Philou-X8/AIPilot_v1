using AIPProvider.src.Radar;
using AIPProvider.src.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityGERunner.UnityApplication;

namespace AIPProvider.src.SensorFusion
{
    
    internal class ActiveDataCollector
    {
        private Utils utils;
        // use the attendance system
        private List<TrackFileActive> tracks;

        public ActiveDataCollector(Utils utilities)
        {
            utils = utilities;
            tracks = new List<TrackFileActive>();
        }

        // prototype
        public List<ActivePassiveCarrier> GetTracks(OutboundState state)
        {
            UpdateScanning(state);

            // must collect list before dead tracks are deleted
            List<ActivePassiveCarrier> passiveTracks = tracks.ConvertAll(track => track.MakePassiveData());
            //List<TrackFileActive> ret = tracks.ToList(); 

            UpdateAttendance(state);

            return passiveTracks;
        }

        // collected tracking data in order of accuracy
        private void UpdateScanning(OutboundState state)
        {
            // collect from STT
            if (state.radar.sttedTarget != null)
            {
                UpdateUnique(state.radar.sttedTarget ?? new StateTargetData(), state.time);
            }
            // collect from TWS
            foreach (StateTargetData contact in state.radar.twsedTargets)
            {
                
                UpdateUnique(contact, state.time);
            }

            // collect from Datalink
            foreach (RadarDLData contact in state.datalink.radar)
            {
                UpdateUnique(contact.data, state.time);
            }
        }

        private void UpdateUnique(StateTargetData contact, float t)
        {
            TrackFileActive? track = tracks.Find(known => known.id == contact.id);

            if (track == null) // new contact
            {
                //utils.Log("adding new active track file"); // it makes a new Active track file every 10 frames
                track = new TrackFileActive(contact.id);
                tracks.Add(track);
            }

            track.AddFrame(contact, t);
        }

        private void UpdateAttendance(OutboundState state)
        {
            foreach (var track in tracks)
            {
                track.ChangeStateAfter();
            }

            tracks.RemoveAll(track => track.state == ActiveTrackState.lost);
            // track lost before we had enough data
            // maybe generate a TrackFileWeak that wont be able to path predict

            tracks.RemoveAll(track => track.state == ActiveTrackState.completed);
            // TrackFilePassive should have been generated before we get here

        }
    }
}
