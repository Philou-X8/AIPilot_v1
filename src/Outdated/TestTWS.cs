using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityGERunner.UnityApplication;

namespace AIPProvider.src.Outdated
{
    internal class TestTWS
    {
        private int progress = 0;

        // collected tracking data in order of accuracy
        public List<int> Update(OutboundState state, int id)
        {
            List<int> actions = new List<int>();
            if(progress == 0)
            {
                actions.Add((int)InboundAction.RadarTWS);
                actions.Add(id);

            }
            else if (progress == 1)
            {
            actions.Add((int)InboundAction.RadarSetPDT);
            actions.Add(1);

            }
            else if (progress == 2) 
            {
            actions.Add((int)InboundAction.SelectHardpoint);
            actions.Add(5);
            actions.Add((int)InboundAction.Fire);

            }
            else if (progress == 3)
            {

            }
            progress++;
            return actions;
        }
    }
}
