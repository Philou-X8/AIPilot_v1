using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityGERunner;
using UnityGERunner.UnityApplication;

namespace AIPProvider.src.Utilities
{
    internal class GroundUtils
    {

        public delegate bool LinecastDel<NetVector>(NetVector a, NetVector b, out NetVector collisionPoint);
        private LinecastDel<NetVector> GroundCast;
        private Func<NetVector, float> GroundAlt;

        public GroundUtils(LinecastDel<NetVector> RefLinecast, Func<NetVector, float> RefGroundAlt)
        {
            GroundCast = RefLinecast;
            GroundAlt = RefGroundAlt;
        }

        public bool Cast(Vector3 start, Vector3 end, out Vector3 collision)
        {
            bool ret = GroundCast(start, end, out NetVector netColl);
            collision = netColl; // convert NetVector to Vector3
            return ret;
        }

        public float Alt(Vector3 position)
        {
            return GroundAlt(position);
        }

    }
}
