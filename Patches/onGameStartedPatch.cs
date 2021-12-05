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

namespace TownOfHost {
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.StartGame))]
    class changeRoleSettings {
        public static void Postfix(AmongUsClient __instance) {
            main.currentWinner = CustomWinner.Default;
            main.JesterWinTrigger = false;
            if(__instance.AmHost) {
                main.SyncCustomSettingsRPC();
                var opt = PlayerControl.GameOptions;
                if(main.currentScientist != ScientistRole.Jester) {
                    opt.RoleOptions.ScientistBatteryCharge = 0f;
                    opt.RoleOptions.ScientistCooldown = 99f;
                }
                if(main.currentEngineer == EngineerRole.Madmate) {
                    opt.RoleOptions.EngineerCooldown = 0.2f;
                    opt.RoleOptions.EngineerInVentMaxTime = float.PositiveInfinity;
                }
                PlayerControl.LocalPlayer.RpcSyncSettings(opt);
            }
        }
    }
}