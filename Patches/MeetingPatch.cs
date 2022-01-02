using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using Hazel;
using System;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnhollowerBaseLib;
using TownOfHost;
using System.Threading.Tasks;
using System.Threading;

namespace TownOfHost {
    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.RpcClose))]
    class MeetingClosePatch {
        public static void Postfix(MeetingHud __instance) {
            if(!AmongUsClient.Instance.AmHost) return;
            if(main.isFixedCooldown) {
                foreach(var pc in PlayerControl.AllPlayerControls) {
                    if(pc.Data.Role.IsImpostor) {
                        pc.RpcProtectPlayer(pc,0);
                        pc.RpcMurderPlayer(pc);
                    }
                }
            }
        }
    }
}