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
using Hazel;

namespace TownOfHost
{
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.CheckForEndVoting))]
    class RpcVotingCompletePatch {
        public static bool Prefix(MeetingHud __instance) {
            foreach(var ps in __instance.playerStates) {
                if(!(ps.AmDead || ps.DidVote))//死んでいないプレイヤーが投票していない
                    return false;
            }
            var array = new MeetingHud.VoterState[1];
            array.AddItem(new MeetingHud.VoterState(){
                VoterId = 0,
                VotedForId = 0
            });
            MeetingHud.Instance.RpcVotingComplete(array, PlayerControl.LocalPlayer.Data, false);
            return false;
        }
    }
}