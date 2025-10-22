using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityGERunner.UnityApplication;

namespace AIPProvider.src.Utilities
{
    

    internal class DeathHistory
    {
        private Utils utils;

        private Dictionary<int, int> dead;

        public DeathHistory(Utils utilities)
        {
            utils = utilities;
            dead = new Dictionary<int, int>();
        }

        public List<int> Deaths(OutboundState state)
        {
            List<int> list = new List<int>();
            foreach (KillFeedEntry entry in state.killFeed)
            {
                if ( ! dead.TryGetValue(entry.entityId, out int id)) // not recorded yet
                {
                    dead.Add(entry.entityId, entry.entityId);
                    list.Add(entry.entityId);
                }
            }
            return list;
        }
    }
}
