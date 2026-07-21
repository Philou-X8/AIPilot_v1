using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityGERunner.UnityApplication;

namespace AIPProvider.src.Radar
{
    internal class TrackFileTWS
    {
        public int id { get; }
        public StateTargetData twsData { get; private set; }

        private bool locked; // under use by a TWS channel

        private float validScanTime; // could be used to identify dead tracks
        private float attemptScanTime; // to be used to sort the queue

        private float TIMEOUT_LIMIT = 30f; // time limit for dead tracks

        public TrackFileTWS(MinimalDetectedTargetData contactData)
        {
            id = contactData.id;
            validScanTime = contactData.detectedTime;
            attemptScanTime = 0; // setting to 0 will put it first in the scan queue
        }

        public bool Available() => !locked;
        public bool Aquire()
        {
            if(!locked)
            {
                locked = true;
                return true;
            }
            return false;
        }
        public bool Release()
        {
            if(locked)
            {
                locked = false;
                return true;
            }
            return false;
        }


        public void UpdateScan(StateTargetData data, float time)
        {
            twsData = data;
            validScanTime = time;
            attemptScanTime = time;
        }
        public void FailedScan(float time)
        {
            attemptScanTime = time;
        }

        /// <summary>
        /// Time since we last tried to track, good or bad
        /// </summary>
        /// <param name="currentTime"></param>
        /// <returns></returns>
        public float Age(float currentTime)
        {
            return currentTime - attemptScanTime;
        }
        /// <summary>
        /// Time since the target was successfuly tracked (TWS)
        /// </summary>
        /// <param name="currentTime"></param>
        /// <returns></returns>
        public float TimeOut(float currentTime)
        {
            return currentTime - validScanTime;
        }
        public bool IsTimedOut(float currentTime)
        {
            return (currentTime - validScanTime) > TIMEOUT_LIMIT; 
        }

    }
}
