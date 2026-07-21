using AIPProvider.src.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityGERunner;
using UnityGERunner.UnityApplication;

namespace AIPProvider.src.RWR
{
    internal class ControllerRWR
    {
        private Utils utils;
        private Transform transform;



        public ControllerRWR(Utils utilities)
        {
            utils = utilities;

            transform = new Transform();
        }

        public void Update(OutboundState state)
        {
            transform.position = state.kinematics.position;
            transform.rotation = state.kinematics.rotation;

            List<StateRWRContact> pings = state.rwrContacts.ToList();

            string prettyPrint = "[";
            foreach (var ping in pings)
            {
                prettyPrint += ping.actorId + ", ";
            }
            prettyPrint += "] , size = " + pings.Count;
            //utils.Log(prettyPrint);
        }
    }
}
