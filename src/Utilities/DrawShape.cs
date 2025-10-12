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
    internal class DrawShape
    {
        private Action<DebugLine> DrawLine;
        private Action<DebugSphere> DrawSphere;
        private Action<int> EraseShape;

        public DrawShape(Action<DebugLine> RefDrawLine, Action<DebugSphere> RefDrawSphere, Action<int> RefEraseShape)
        {

            DrawLine = RefDrawLine;
            DrawSphere = RefDrawSphere;
            EraseShape = RefEraseShape;
        }

        public void Draw(DebugLine shape)
        {
            DrawLine(shape);
        }
        public void Draw(DebugSphere shape)
        {
            DrawSphere(shape);
        }

        public void Erase(int id) 
        {
            EraseShape(id);
        }
    }
}
