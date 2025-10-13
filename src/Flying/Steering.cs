using AIPProvider.src.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityGERunner.UnityApplication;
using UnityGERunner;
using Recorder;

namespace AIPProvider.src.Flying
{
    internal class Steering
    {
        private Utils utils;
        private Transform transform;

        const float SMOOTH_TURN_MIN = 10000f;
        const float SMOOTH_TURN_MAX = 50000f;
        const float PITCH_MULT = 4f;
        const float ROLL_MULT= 4f;
        const float YAW_MULT = 8f;

        private DebugLine waypointLine;

        public Steering(Utils utilities)
        {
            utils = utilities;

            waypointLine = utils.MakeLine();

            transform = new Transform();
        }

        public InboundState Update(OutboundState state)
        {
            transform.position = state.kinematics.position;
            transform.rotation = state.kinematics.rotation;

            return new InboundState();
        }

        public Vector3 SteerWaypoint(Transform pos, Vector3 waypoint)
        {
            Vector3 relDir = pos.InverseTransformDirection(waypoint - pos.position);
            return SteerRelative(relDir);
        }
        public Vector3 SteerDirection(Transform pos, Vector3 direction)
        {
            Vector3 relDir = pos.InverseTransformDirection(direction);
            return SteerRelative(relDir);
        }

        private Vector3 SteerRelative(Vector3 direction)
        {
            float distance = direction.magnitude;
            float relaxFactor = 1; // reduce input strengh for far away wayoints
            if(distance > SMOOTH_TURN_MIN)
            {
                // only work if direction is not normalized
                relaxFactor = Utils.MapRange(distance, SMOOTH_TURN_MIN, SMOOTH_TURN_MAX, 1.0f, 0.5f);
                relaxFactor = Math.Clamp(relaxFactor, 0.5f, 1.0f);
            }
            direction.Normalize(); // direction here must be relative direction and normalized
            //utils.Graph("dir x", direction.x);
            //utils.Graph("dir y", direction.y);
            //utils.Graph("dir z", direction.z);
            direction.x = -direction.x; // make X positive on the right 
            float pitch = GetPitch(new Vector2(direction.z, direction.y));
            float roll  =  GetRoll(new Vector2(direction.x, direction.y));
            float yaw   =   GetYaw(new Vector2(direction.x, direction.z));
            Vector3 pyr = Vector3.zero;
            pyr.x = ScalePitch(pitch) * relaxFactor;
            pyr.z = ScaleRoll(roll) * (relaxFactor * relaxFactor);
            pyr.y = ScaleYaw(yaw) * relaxFactor;
            return pyr;
        }

        private float GetPitch(Vector2 zy)
        {
            float z = zy.x;
            float y = zy.y;
            
            if (
                ( (MathF.Abs(z) + MathF.Abs(y) < 1) // inner diamond
                && (y < -MathF.Abs(z)) ) // lower 45deg
                || (zy.magnitude < 0.2f) // small inner circle
                )
            {
                // neutral zone, dont pitch
                return 0;
            }
            else if (z < 0)
            {
                // target behind you, full pitch up
                return -1;
            }
            else if ( (z > 0) && (y < 0) )
            {
                // pitch down
                return -y;
            }
            else
            {
                // remaing uper front zone, pitch up
                return -y;
            }
            return 0;
        }
        private float GetRoll(Vector2 xy)
        {
            float x = xy.x;
            float y = xy.y;

            //if (xy.magnitude < 0.01f)
            //{
            //    // neutral zone, dont roll
            //    return 0;
            //}
            if (
                (y < -MathF.Abs(x))
                && (xy.magnitude < 0.5f)
                )
            {
                // inverted roll zone
                return -x;
            }
            else if (y < -0.1f)
            {
                // target bellow us, roll fast
                return (x > 0) ? 1 : -1;
            }
            else
            {
                // normal zone
                return x;
            }
            return 0;
        }
        private float GetYaw(Vector2 xz)
        {
            float x = xz.x;
            float z = xz.y;

            if (z < MathF.Abs(2 * x))
            {
                // neutral zone
                return 0;
            }
            else
            {
                return -x; // yaw has inverted controls
            }
            return 0;
        }

        private float ScalePitch(float input)
        {
            return Math.Clamp(
                //input + 4 * MathF.Pow(input, 3), 
                PITCH_MULT * input,
                -1f, 1f);
        }
        private float ScaleRoll(float input)
        {
            return Math.Clamp(
                ROLL_MULT * input, 
                -1f, 1f);
        }
        private float ScaleYaw(float input)
        {
            return Math.Clamp(
                YAW_MULT * input, 
                -1f, 1f);
        }
    }
}
