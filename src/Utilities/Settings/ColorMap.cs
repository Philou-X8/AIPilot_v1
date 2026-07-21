using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityGERunner;
using UnityGERunner.UnityApplication;

namespace AIPProvider.src.Utilities.Settings
{
    internal class ColorMap
    {
        public static NetColor Convert(Vector3 color)
        {
            return new NetColor(color.x / 255f, color.y / 255f, color.z / 255f, 1.0f);
        }

        public static Vector3 RadarSpot => new Vector3(166, 36, 108);
        public static Vector3 RadarGuide => new Vector3(237, 119, 156);

        public static Vector3 RwrPing => new Vector3(123, 55, 148);
        public static Vector3 RwrLock => new Vector3(187, 0, 255);

        public static Vector3 TrackPath => new Vector3(92, 32, 232);
        public static Vector3 TrackLocation => new Vector3(25, 87, 255);

        public static Vector3 VisualSpot => new Vector3(13, 243, 255);
        public static Vector3 VisualGimbal => new Vector3(13, 243, 255);

        public static Vector3 NavWaypoint => new Vector3(19, 97, 22);
        public static Vector3 NavCheckpoint => new Vector3(5, 255, 15);
        public static Vector3 NavRatRoute => new Vector3(176, 252, 121);

        public static Vector3 Gun => new Vector3(255, 255, 0);

        public static Vector3 MissileRdr => new Vector3(181, 22, 22);
        public static Vector3 MissileHeat => new Vector3(181, 104, 22);
    }
}
