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

namespace TownOfHost {
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
    class EndGamePatch {
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)]ref EndGameResult endGameResult) {
            //勝者削除
            foreach(var p in PlayerControl.AllPlayerControls) {
                if(p.Data.Role.Role == RoleTypes.Engineer && main.MadmateEnabled && TempData.DidHumansWin(endGameResult.GameOverReason))
                    TempData.winners.Remove(new WinningPlayerData(p.Data));
                if(p.Data.Role.Role == RoleTypes.Scientist && main.JesterEnabled && TempData.DidHumansWin(endGameResult.GameOverReason))
                    TempData.winners.Remove(new WinningPlayerData(p.Data));
            }
            //勝者追加
            if(main.MadmateEnabled) {
                if(TempData.DidImpostorsWin(endGameResult.GameOverReason)) {
                    foreach(var p in PlayerControl.AllPlayerControls) {
                        if(p.Data.Role.Role == RoleTypes.Engineer) {
                            TempData.winners.Add(new WinningPlayerData(p.Data));
                        }
                    }
                }
            }
            if(main.currentWinner == CustomWinner.Jester && main.JesterEnabled) {
                foreach(var p in PlayerControl.AllPlayerControls) {
                    TempData.winners = new Il2CppSystem.Collections.Generic.List<WinningPlayerData>();
                    if(p.PlayerId == main.ExiledJesterID)
                        TempData.winners.Add(new WinningPlayerData(p.Data));
                }
            }
        }
    }
    [HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.SetEverythingUp))]
    class SetEverythingUpPatch {
        public static void Postfix(EndGameManager __instance) {
            //特殊勝利
            if(main.currentWinner == CustomWinner.Jester) {
                __instance.BackgroundBar.material.color = main.JesterColor();
            }
            //引き分け処理
            if(main.currentWinner == CustomWinner.Draw) {
                __instance.BackgroundBar.material.color = Color.gray;
                __instance.WinText.text = "廃村";
                __instance.WinText.color = Color.white;
            }
        }
    }
}