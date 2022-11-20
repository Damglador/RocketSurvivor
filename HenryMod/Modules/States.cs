﻿using RocketSurvivor.SkillStates;
using RocketSurvivor.SkillStates.BaseStates;
using System.Collections.Generic;
using System;
using EntityStates.RocketSurvivorSkills.Primary;
using EntityStates.RocketSurvivorSkills.Secondary;
using EntityStates.RocketSurvivorSkills.Utility;
using EntityStates.RocketSurvivorSkills.Special;

namespace RocketSurvivor.Modules
{
    public static class States
    {
        internal static void RegisterStates()
        {
            Modules.Content.AddEntityState(typeof(FireRocket));
            Modules.Content.AddEntityState(typeof(FireRocketAlt));
            Modules.Content.AddEntityState(typeof(EnterReload));
            Modules.Content.AddEntityState(typeof(Reload));
            Modules.Content.AddEntityState(typeof(AirDet));
            Modules.Content.AddEntityState(typeof(FireAllRockets));
            Modules.Content.AddEntityState(typeof(FireAllRocketsScepter));
            Modules.Content.AddEntityState(typeof(Rearm));
            Modules.Content.AddEntityState(typeof(PrepComicallyLargeSpoon));
            Modules.Content.AddEntityState(typeof(ComicallyLargeSpoon));
            Modules.Content.AddEntityState(typeof(C4));
        }
    }
}