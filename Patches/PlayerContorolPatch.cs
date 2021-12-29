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
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
    class MurderPlayerPatch {
        public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)]PlayerControl target) {
            if(!target.Data.IsDead)
                return;
            //When Bait is killed
            if(target.Data.Role.Role == RoleTypes.Scientist && main.currentScientist == ScientistRole.Bait && AmongUsClient.Instance.AmHost) {
                Thread.Sleep(150);
                __instance.CmdReportDeadBody(target.Data);
            } else
            //Terrorist
            if(main.isTerrorist(target)) {
                main.CheckTerroristWin(target.Data);
            }
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckMurder))]
    class CheckMurderPatch {
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)]PlayerControl target) {
            if(!AmongUsClient.Instance.AmHost) return false;
            if(main.isSidekick(__instance)) {
                var ImpostorCount = 0;
                foreach(var pc in PlayerControl.AllPlayerControls) {
                    if(pc.Data.Role.Role == RoleTypes.Impostor &&
                       !pc.Data.IsDead) ImpostorCount++;
                }
                if(ImpostorCount > 0) return false;
            }
            if(false) { //キルキャンセル&自爆処理
                //キルクール二倍
                var cooldown = PlayerControl.GameOptions.KillCooldown;
                PlayerControl.GameOptions.KillCooldown = cooldown * 2;
                PlayerControl.LocalPlayer.RpcSyncSettings(PlayerControl.GameOptions);

                //キルクールリセット
                __instance.RpcProtectPlayer(__instance, 0);
                __instance.RpcMurderPlayer(__instance);

                //キルクール元に戻す
                PlayerControl.GameOptions.KillCooldown = cooldown;
                PlayerControl.LocalPlayer.RpcSyncSettings(PlayerControl.GameOptions);

                //キル
                target.RpcMurderPlayer(target);
                return false;
            }

            __instance.RpcMurderPlayer(target);
            return false;
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
    class ReportDeadBodyPatch {
        public static bool Prefix(PlayerControl __instance) {
            if(main.IsHideAndSeek) return false;
            return true;
        }
    }
}