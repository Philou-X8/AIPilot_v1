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

namespace AIPProvider.src.Utilities
{
    internal class DrawShapeCounter
    {
        private static readonly DrawShapeCounter instance = new DrawShapeCounter();

        private int shapeCount;

        private DrawShapeCounter() {
            shapeCount = 0;
        }

        public static DrawShapeCounter Get { get { return instance; } }

        public DebugLine CreateLine()
        {
            int id = int.MaxValue - shapeCount;
            shapeCount++;
            return new DebugLine(id);
        }

        public DebugSphere CreateSphere()
        {
            int id = int.MaxValue - shapeCount;
            shapeCount++;
            return new DebugSphere(id);
        }

    }
}
