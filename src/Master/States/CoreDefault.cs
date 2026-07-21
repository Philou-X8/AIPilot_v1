using AIPProvider.src.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityGERunner;
using UnityGERunner.UnityApplication;

namespace AIPProvider.src.Master.States
{
    internal struct SimActions
    {
        public List<int> actions;
        public float throttle;
        public Vector3 pyr;
        public Vector3 lookDir;

        public SimActions()
        {
            actions = new List<int>();
            throttle = 100;
            pyr = Vector3.zero;
            lookDir = Vector3.zero;
        }

        public InboundState Convert()
        {
            return new InboundState
            {
                pyr = this.pyr,
                throttle = this.throttle,
                irLookDir = this.lookDir,
                events = actions.ToArray()
            };
        }
    }

    internal class CoreDefault
    {
        private Utils utils;
        private SetupInfo simInfo;
        private OutboundState simData;

        public CoreDefault(Utils utilities)
        {
            utils = utilities;
        }

        public SetupActions Start(SetupInfo info)
        {
            simInfo = info;

            return new SetupActions();
        }

        public InboundState Update(OutboundState state)
        {
            simData = state;

            return new InboundState();
        }
    }
}
