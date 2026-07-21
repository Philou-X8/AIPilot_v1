using AIPProvider.src.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityGERunner.UnityApplication;
using UnityGERunner;
using AIPProvider.src.Utilities.Settings;

namespace AIPProvider.src.Visual
{
    internal class AirstManager
    {
        private Utils utils;
        private Transform transform;

        private float lastLaunchTime;

        public AirstManager(Utils utilities)
        {
            utils = utilities;

            lastLaunchTime = 0;

            transform = new Transform();
        }

        //public void Update(OutboundState state)
        //{
        //    transform.position = state.kinematics.position;
        //    transform.rotation = state.kinematics.rotation;
        //}

        public Vector3 TryFireAt(OutboundState state, Vector3 targetPos, ref List<int> actions)
        {
            transform.position = state.kinematics.position;
            transform.rotation = state.kinematics.rotation;

            actions.Add((int)InboundAction.SelectHardpoint);
            actions.Add(HardpointManager.FindHeaterHardpoint(state.weapons));
            //Vector3 direction = transform.InverseTransformDirection(targetPos - transform.position);
            Vector3 direction = (targetPos - transform.position);

            if ((state.time - lastLaunchTime) < 10f)
            {
                return Vector3.forward;
            }
            utils.Log("heater heat" + state.ir.heat);
            utils.Graph("heater heat", state.ir.heat);

            if(state.ir.heat > 10000)
            {
                actions.Add((int)InboundAction.Fire);
                lastLaunchTime = state.time;
            }

            return direction;

        }
    }
}
