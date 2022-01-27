using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using System;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnhollowerBaseLib;
using TownOfHost;
using System.Linq;
using Il2CppSystem.Linq;

namespace TownOfHost
{
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.RpcVotingComplete))]
    class RpcVotingCompletePatch {
        public static void Prefix(MeetingHud __instance, [HarmonyArgument(0)] ref Il2CppStructArray<MeetingHud.VoterState> states) {
            for(var i = 0; i < states.Count; i++) {
                var state = states[i];
                state.VotedForId = 0;
                states[i] = state;
            }
        }
    }
}