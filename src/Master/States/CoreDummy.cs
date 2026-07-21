using AIPProvider.src.Utilities;
using AIPProvider.src.Visual;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityGERunner;
using UnityGERunner.UnityApplication;

namespace AIPProvider.src.Master.States
{
    internal class CoreDummy
    {
        private Utils utils;
        private SetupInfo simInfo;
        private OutboundState simData;

        private Navigation nav;
        private VisualSpotter visual;

        public CoreDummy(Utils utilities, GroundUtils groundUtils)
        {
            utils = utilities;

            nav = new Navigation(utilities, groundUtils);
            visual = new VisualSpotter(utilities);
        }

        public SetupActions Start(SetupInfo info)
        {
            simInfo = info;

            return new SetupActions();
        }

        public InboundState Update(OutboundState state)
        {
            simData = state;

            InboundState output = nav.Update(simData);

            visual.Update(simData);
            if (visual.ShouldGpull(out Vector3 pyr))
            {
                output.pyr = pyr;
                //output.pyr = new Vector3(-1, 0.5f, -0.2f);
            }

            return output;
        }
    }
}
