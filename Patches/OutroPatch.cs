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

namespace TownOfHost
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
    class EndGamePatch
    {
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ref EndGameResult endGameResult)
        {
            //winnerListリセット
            TempData.winners = new Il2CppSystem.Collections.Generic.List<WinningPlayerData>();
            var winner = new List<PlayerControl>();
            //勝者リスト作成
            if (TempData.DidHumansWin(endGameResult.GameOverReason))
            {
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (p.Data.Role.Role == RoleTypes.Crewmate) winner.Add(p); //Crewmate
                    if (p.Data.Role.Role == RoleTypes.GuardianAngel) winner.Add(p); //GuardianAngel
                    if (p.Data.Role.Role == RoleTypes.Engineer && main.currentEngineer == EngineerRole.Default) winner.Add(p); //Engineer
                    if (p.Data.Role.Role == RoleTypes.Scientist && main.currentScientist == ScientistRole.Default) winner.Add(p); //Scientist
                    if (p.Data.Role.Role == RoleTypes.Scientist && main.currentScientist == ScientistRole.Bait) winner.Add(p); //bait
                }
            }
            if (TempData.DidImpostorsWin(endGameResult.GameOverReason))
            {
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (p.Data.Role.Role == RoleTypes.Impostor) winner.Add(p); //Impostor
                    if (p.Data.Role.Role == RoleTypes.Shapeshifter) winner.Add(p); //ShapeShifter
                    if (p.Data.Role.Role == RoleTypes.Engineer && main.currentEngineer == EngineerRole.Madmate) winner.Add(p); // MadmateのEngineer
                }
            }
            if (endGameResult.GameOverReason == GameOverReason.HumansDisconnect ||
            endGameResult.GameOverReason == GameOverReason.ImpostorDisconnect ||
            main.currentWinner == CustomWinner.Draw)
            {
                winner = new List<PlayerControl>();
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    winner.Add(p);
                }
            }
            foreach (var p in winner)
            {
                TempData.winners.Add(new WinningPlayerData(p.Data));
            }

            //単独勝利
            if (main.currentWinner == CustomWinner.Jester && main.currentScientist == ScientistRole.Jester)
            { //Jester単独勝利
                TempData.winners = new Il2CppSystem.Collections.Generic.List<WinningPlayerData>();
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (p.PlayerId == main.ExiledJesterID)
                        TempData.winners.Add(new WinningPlayerData(p.Data));
                }
            }
            if (main.currentWinner == CustomWinner.Terrorist && main.currentEngineer == EngineerRole.Terrorist)
            { //Terrorist単独勝利
                TempData.winners = new Il2CppSystem.Collections.Generic.List<WinningPlayerData>();
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (p.PlayerId == main.WonTerroristID)
                        TempData.winners.Add(new WinningPlayerData(p.Data));
                }
            }
            main.winnerList = "winner:";
            foreach (var wpd in TempData.winners)
            {
                main.winnerList += wpd.PlayerName + ", ";
            }
        }
    }
    [HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.SetEverythingUp))]
    class SetEverythingUpPatch
    {
        public static void Postfix(EndGameManager __instance)
        {
            //特殊勝利
            if (main.currentWinner == CustomWinner.Jester)
            {
                __instance.BackgroundBar.material.color = main.JesterColor();
            }
            if (main.currentWinner == CustomWinner.Terrorist)
            {
                __instance.Foreground.material.color = Color.red;
                __instance.BackgroundBar.material.color = Color.green;
            }
            //引き分け処理
            if (main.currentWinner == CustomWinner.Draw)
            {
                __instance.BackgroundBar.material.color = Color.gray;
                __instance.WinText.text = "廃村";
                __instance.WinText.color = Color.white;
            }
            if (main.isFixedCooldown && AmongUsClient.Instance.AmHost)
            {
                PlayerControl.GameOptions.KillCooldown = main.BeforeFixCooldown;
            }
            if (main.SyncButtonMode)
            {
                PlayerControl.GameOptions.EmergencyCooldown = main.BeforeFixMeetingCooldown;
            }
            main.BitPlayers = new Dictionary<byte, (byte, float)>();
            PlayerControl.LocalPlayer.RpcSyncSettings(PlayerControl.GameOptions);

            main.VisibleTasksCount = false;
        }
    }
}
