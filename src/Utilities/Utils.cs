using AIPProvider.src.Utilities.Settings;
using Recorder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityGERunner;
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
        public DebugLine MakeLine(Vector3 color)
        {
            return MakeLine(color.x / 255f, color.y / 255f, color.z / 255f);
        }
        public DebugSphere MakeShere()
        {
            DebugSphere shape = DrawShapeCounter.Get.CreateSphere();
            Logger("Created DebugSphere with id: " + shape.id.ToString());
            return shape;
            //return DrawShapeCounter.Get.CreateSphere();
        }

        public void Draw(DebugLine shape, float min, float max)
        {
            Vector3 start = Vector3.Lerp(shape.start?.vec3 ?? Vector3.zero, shape.end?.vec3 ?? Vector3.zero, min);
            Vector3 end = Vector3.Lerp(shape.start?.vec3 ?? Vector3.zero, shape.end?.vec3 ?? Vector3.zero, max);
            shape.start = start;
            shape.end = end;
            drawShape.Draw(shape);
        }
        public void Draw(DebugLine shape) => drawShape.Draw(shape);
        public void Draw(DebugSphere shape) => drawShape.Draw(shape);
        public void Erase(int id) => drawShape.Erase(id);

        public static float MapRange(float t, float i_min, float i_max, float o_min, float o_max)
        {
            float ratio = (t - i_min) / (i_max - i_min);
            return (o_max - o_min) * ratio + o_min;
        }

        /// <summary>
        /// https://en.wikipedia.org/wiki/Skew_lines#Nearest_points
        /// </summary>
        /// <param name="p1">Starting point of line A</param>
        /// <param name="d1">Direction of line A</param>
        /// <param name="p2">Starting point of line B</param>
        /// <param name="d2">Direction of line B</param>
        /// <param name="center">Intersection Point of both line</param>
        /// <param name="offset">Distance between the two lines at intersection point</param>
        /// <returns>return True if a solution exist</returns>
        public static bool SkewLines(Vector3 p1, Vector3 d1, Vector3 p2, Vector3 d2, out Vector3 center, out float offset)
        {
            center = p1;
            offset = (p1 - p2).magnitude;

            if (p1 == p2 || d1 == d2) return false;

            Vector3 n = Vector3.Cross(d1, d2);
            if (n.magnitude < 0.0005f)
            {
                return false;
            }
            Vector3 n1 = Vector3.Cross(d1, n);
            Vector3 n2 = Vector3.Cross(d2, n);

            Vector3 c1 = p1 + (Vector3.Dot((p2 - p1), n2) / Vector3.Dot(d1, n2)) * d1;
            Vector3 c2 = p2 + (Vector3.Dot((p1 - p2), n1) / Vector3.Dot(d2, n1)) * d2;

            center = Vector3.Lerp(c1, c2, 0.5f);
            offset = (c1 - c2).magnitude;
            return true;
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
