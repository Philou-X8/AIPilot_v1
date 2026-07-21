using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Recorder;
using System.Runtime.Loader;
using UnityGERunner;
using UnityGERunner.UnityApplication;
using System.Runtime.CompilerServices;
using AIPProvider.src.Utilities;

namespace AIPProvider.src
{
    internal class TerrainWarning
    {
        private Utils utils;
        private Transform transform;

        public TerrainWarning(Utils utilities)
        {
            utils = utilities;

            transform = new Transform();
        }

        public void Update(OutboundState state, ref List<int> actions)
        {
            transform.position = state.kinematics.position;
            transform.rotation = state.kinematics.rotation;


        }

        public InboundState Update(OutboundState state)
        {
            transform.position = state.kinematics.position;
            transform.rotation = state.kinematics.rotation;

            return new InboundState();
        }

    }
}
