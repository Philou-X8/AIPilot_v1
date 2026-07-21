using AIPProvider.src;
using AIPProvider.src.Master.States;
using AIPProvider.src.Outdated;
using AIPProvider.src.Radar;
using AIPProvider.src.Radar.STT;
using AIPProvider.src.Radar.TWS;
using AIPProvider.src.RWR;
using AIPProvider.src.SensorFusion;
using AIPProvider.src.Terrain;
using AIPProvider.src.Utilities;
using AIPProvider.src.Visual;
using Recorder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Loader;
using System.Xml.Linq;
using UnityGERunner;
using UnityGERunner.UnityApplication;
using static AIPLoader.AIPProvider;

namespace AIPLoader
{
    class AIPProvider : IAIPProvider
    {
        Utils? utils;
        GroundUtils? gUtils;

        CoreTesting? coreTesting;
        CoreDefault? coreDefault;
        CoreDummy? coreDummy;

        public override SetupActions Start(SetupInfo info)
        {
            utils = new Utils(info, Log, DebugShape, DebugShape, RemoveDebugShape, Graph);
            gUtils = new GroundUtils(Linecast, HeightAt);
            coreTesting = new CoreTesting(utils, gUtils);
            coreDefault = new CoreDefault(utils);
            coreDummy = new CoreDummy(utils, gUtils);

            return coreTesting.Start(info);
        }

        public override InboundState Update(OutboundState state)
        {
            if (utils?.IsAllied(Team.Allied) ?? true)
            {
                return coreTesting?.Update(state) ?? new InboundState();
            }
            else
            {
                return coreDummy?.Update(state) ?? new InboundState();
            }
        }
    }
}
