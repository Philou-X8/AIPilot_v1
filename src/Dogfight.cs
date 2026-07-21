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
using static System.Net.Mime.MediaTypeNames;
using AIPProvider.src.Utilities;

namespace AIPProvider.src
{
    internal class Dogfight
    {
        private Utils utils;
        private DebugLine visorLine;

        private Transform transform;
        private Vector3 direction;

        //public Dogfight(SetupInfo info, Action<DebugLine> RefDrawShape)
        public Dogfight(Utils utilities)
        {
            utils = utilities;
            visorLine = utils.MakeLine();
            visorLine.color = new NetColor(1.0f, 0.5f, 0.1f, 1);

            transform = new Transform();
        }

        public InboundState Update(OutboundState state, Vector3 targetDir)
        {
            transform.position = state.kinematics.position;
            transform.rotation = state.kinematics.rotation;

            direction = targetDir;


            InboundState controls = new InboundState();
            controls.pyr = Fly();

            DrawVisor();

            return controls;
        }

        private Vector3 Fly()
        {
            Vector3 pyr = new Vector3(0, 0, 0);
            if (direction == Vector3.zero) return pyr;

            Vector3 relativeDir = transform.InverseTransformDirection(direction).normalized;

            float rollAmount = (relativeDir.z > 0) 
                ? Math.Clamp(-relativeDir.x * 1, -0.2f, 0.2f) 
                : Math.Clamp(-relativeDir.x * 0.1f, -0.2f, 0.2f);
            float pullAmount = (relativeDir.z > 0) ? Math.Clamp(-relativeDir.y * 5, -1f, 1f) : -1;
            float yawAmount = Math.Clamp(-relativeDir.x * 0.1f, -1f, 1f);
            pyr.x = pullAmount;
            pyr.y = yawAmount;
            pyr.z = rollAmount;

            return pyr;
        }

        private void DrawVisor()
        {
            visorLine.start = transform.position;
            visorLine.end = transform.position + (direction * 100);

            utils.Draw(visorLine);
        }
    }
}
