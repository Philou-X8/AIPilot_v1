using Recorder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityGERunner.UnityApplication;

namespace AIPProvider.src.Utilities
{
    internal class Utils
    {
        private SetupInfo gameInfo;
        private DrawShape drawShape;
        private Action<string> Logger;
        private Action<string, float> Grapher;
        public HardpointManager hardpoint { get; }

        public Utils(SetupInfo info, Action<string> log, Action<DebugLine> RefDrawLine, Action<DebugSphere> RefDrawSphere, Action<int> RefErase, Action<string, float> RefGraph)
        {
            gameInfo = info;
            drawShape = new DrawShape(RefDrawLine, RefDrawSphere, RefErase);
            Logger = log;
            Grapher = RefGraph;
            hardpoint = new HardpointManager();
        }

        public SetupInfo info {
            get {
                return gameInfo;
            }
        }

        public bool IsAllied(Team team)
        {
            string log = "Checking if target (" + team.ToString() + ") is on my team (" + gameInfo.team.ToString() + "): " + (team == gameInfo.team).ToString();
            //Logger(log);
            return team == gameInfo.team;
        }

        public void Log(string log)
        {
            if (gameInfo.team == Team.Enemy) return;
            Logger(log);
        }
        public void Graph(string key, float val)
        {
            string keyId = key + " id_" + info.id.ToString();
            Grapher(keyId, val);
        }

        public DebugLine MakeLine()
        {
            DebugLine shape = DrawShapeCounter.Get.CreateLine();
            Logger("Created DebugLine with id: " + shape.id.ToString());
            return shape;
            //return DrawShapeCounter.Get.CreateLine();
        }
        public DebugLine MakeLine(float r, float g, float b)
        {
            DebugLine shape = DrawShapeCounter.Get.CreateLine();
            shape.color = new NetColor(r, g, b, 1);
            Logger("Created DebugLine with id: " + shape.id.ToString());
            return shape;
        }
        public DebugLine MakeLine(int r, int g, int b)
        {
            return MakeLine(r / 255f, g / 255f, b / 255f);
        }
        public DebugSphere MakeShere()
        {
            DebugSphere shape = DrawShapeCounter.Get.CreateSphere();
            Logger("Created DebugSphere with id: " + shape.id.ToString());
            return shape;
            //return DrawShapeCounter.Get.CreateSphere();
        }

        public void Draw(DebugLine shape) => drawShape.Draw(shape);
        public void Draw(DebugSphere shape) => drawShape.Draw(shape);
        public void Erase(int id) => drawShape.Erase(id);

        public static float MapRange(float t, float i_min, float i_max, float o_min, float o_max)
        {
            float ratio = (t - i_min) / (i_max - i_min);
            return (o_max - o_min) * ratio + o_min;
        }

        public static float m2ft(float meter)
        {
            return meter * 3.28084f;
        }
        public static float km2nm(float km)
        {
            return km * 0.539957f;
        }
        public static float m2nm(float meter)
        {
            return meter * 0.001f * 0.539957f;
        }
    }
}
