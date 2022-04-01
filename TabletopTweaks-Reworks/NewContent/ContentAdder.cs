﻿using HarmonyLib;
using Kingmaker.Blueprints.JsonSystem;
using static TabletopTweaks.Reworks.Main;

namespace TabletopTweaks.Reworks.NewContent {
    class ContentAdder {
        [HarmonyPatch(typeof(BlueprintsCache), "Init")]
        static class BlueprintsCache_Init_Patch {
            static bool Initialized;

            [HarmonyPriority(Priority.First)]
            static void Postfix() {
                if (Initialized) return;
                Initialized = true;
                TTTContext.Logger.LogHeader("Loading New Content");

                MythicAbilities.DimensionalRetribution.AddDimensionalRetribution();
                Classes.Lich.AddLichFeatures();
                Classes.Aeon.AddAeonFeatures();
                Classes.Trickster.AddTricksterDomains();
                Classes.Trickster.AddTricksterTricks();
            }
        }
    }
}