using AIPProvider.src.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityGERunner.UnityApplication;
using UnityGERunner;
using Recorder;

namespace AIPProvider.src.Radar.STT
{
    internal class Gimball
    {
        private Utils utils;
        private Transform transform;

        private DebugLine lineX;
        private DebugLine lineY;
        private DebugLine lineZ;

        public Gimball(Utils utilities)
        {
            utils = utilities;
            lineX = utils.MakeLine(1.0f, 0.0f, 0.0f);
            lineY = utils.MakeLine(0.0f, 1.0f, 0.0f);
            lineZ = utils.MakeLine(0.0f, 0.0f, 1.0f);
            transform = new Transform();
        }

        public void Update(Transform t)
        {
            lineX.start = t.position;
            lineY.start = t.position;
            lineZ.start = t.position;

            lineX.end = t.position + t.right * 100;
            lineY.end = t.position + t.up * 100;
            lineZ.end = t.position + t.forward * 100;

            utils.Draw(lineX);
            utils.Draw(lineY);
            utils.Draw(lineZ);
        }


    }
}
