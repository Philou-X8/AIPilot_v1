using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIPProvider.src.Utilities.Settings
{
    internal class HardpointManager
    {
        public enum WeaponType
        {
            gun,
            radar,
            heater,
        }

        private List<int> rdrLaunchOrder; // list of hardpoint id
        private List<int> heaterLaunchOrder; // list of hardpoint id
        private List<int> rdrRemaining; // radar remaining on each hardpoint
        private List<int> heaterRemaining;
        private int rdrFiredCount;
        private int heaterFiredCount;

        public HardpointManager()
        {
            rdrFiredCount = 0;
            heaterFiredCount = 0;
            //rdrRemaining    = new List<int> { 0, 0, 1,1, 1,1,1,1, 1,1, 0, 2,2};
            //heaterRemaining = new List<int> { 0, 3, 0, 0, 0, 0, 0, 0, 0, 0, 3, 0, 0 };
            //rdrLaunchOrder = new List<int> { 2, 9, 3, 8, 11, 12, 11, 12, 4, 5, 6, 7 };
            //heaterLaunchOrder = new List<int> { 1, 10, 1, 10, 1, 10 };
            rdrRemaining = new List<int> { 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0, 0, 1, 1, 1, 1 };
            heaterRemaining = new List<int> { 0, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0 };
            rdrLaunchOrder = new List<int> { 4, 5, 6, 7, 8, 9, 10, 11, 15, 16, 17, 18 };
            heaterLaunchOrder = new List<int> { 1, 2, 3, 12, 13, 14 };
        }

        /// <summary>
        /// allowed weapons: r=radar, h=heater
        /// 0: gun
        /// 1: r1, r2, h1, h2, h3
        /// 2: r1, h1
        /// 3: r1, h1
        /// 4: r0
        /// 5: r0
        /// 6: r0
        /// 7: r0
        /// 8: r1, h1
        /// 9: r1, h1
        /// 10: r1, r2, h1, h2, h3
        /// 11: r1, r2, h1
        /// 12: r1, r2, h1
        /// </summary>
        public string[] MakeHardpointList()
        {
            return new string[] {
                "HPEquips/AFighter/fa26_gun", // 0
                "HPEquips/AFighter/fa26_iris-t-x3", // 1
                "HPEquips/AFighter/af_amraamRail", // 2
                "HPEquips/AFighter/af_amraamRail", // 3
                "HPEquips/AFighter/af_amraam", // 4
                "HPEquips/AFighter/af_amraam", // 5
                "HPEquips/AFighter/af_amraam", // 6
                "HPEquips/AFighter/af_amraam", // 7
                "HPEquips/AFighter/af_amraamRail", // 8
                "HPEquips/AFighter/af_amraamRail", // 9
                "HPEquips/AFighter/fa26_iris-t-x3", // 10
                "HPEquips/AFighter/af_amraamRailx2", // 11
                "HPEquips/AFighter/af_amraamRailx2", // 12
            };
        }

        public static int FindRadarHardpoint(string[] weapons)
        {
            return weapons.ToList().FindIndex(str => str == "Weapons/Missiles/AIM-120");
        }

        public static int FindHeaterHardpoint(string[] weapons)
        {
            return weapons.ToList().FindIndex(str => str == "Weapons/Missiles/AIRS-T");
        }

        public static bool TryGetRadarMissile(string[] weapons, out int hardpoint)
        {
            hardpoint = weapons.ToList().FindIndex(str => str == "Weapons/Missiles/AIM-120");
            return hardpoint != -1;
        }

        public static bool TryGetHeaterMissile(string[] weapons, out int hardpoint)
        {
            hardpoint = weapons.ToList().FindIndex(str => str == "Weapons/Missiles/AIRS-T");
            return hardpoint != -1;
        }


        public int GetRadarHardpoint()
        {
            for (int i = rdrFiredCount; i < 12; i++)
            {
                int hardpointId = rdrLaunchOrder[i];
                int available = rdrRemaining[hardpointId];
                if (available <= 0) continue; // no more weapon on this hardpoint

                // use up the missile:
                rdrRemaining[hardpointId]--;
                rdrFiredCount++;
                return hardpointId;
                // TODO: return hardpointId
            }
            // TODO: do something if no missile was available
            return -1;
        }
        public bool TryGetRadarHardpoint(out int hardpointId)
        {
            for (int i = rdrFiredCount; i < 12; i++)
            {
                int seekId = rdrLaunchOrder[i] - 1;
                int available = rdrRemaining[seekId];
                if (available <= 0) continue; // no more weapon on this hardpoint

                // use up the missile:
                hardpointId = seekId;
                //hardpointId = seekId - 1; // TODO ERROR: hardpoint are offset for some reason
                rdrRemaining[seekId]--;
                rdrFiredCount++;
                return true;
            }
            hardpointId = -1;
            return false;
        }

        public int GetHeaterHardpoint()
        {
            for (int i = heaterFiredCount; i < 12; i++)
            {
                int hardpointId = heaterLaunchOrder[i];
                int available = heaterRemaining[hardpointId];
                if (available <= 0) continue; // no more weapon on this hardpoint

                // use up the missile:
                heaterRemaining[hardpointId]--;
                heaterFiredCount++;
                return hardpointId;
                // TODO: return hardpointId
            }
            // TODO: do something if no missile was available
            return -1;
        }


    }
}
