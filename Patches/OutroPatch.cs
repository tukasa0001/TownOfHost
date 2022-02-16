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
                    CustomRoles role = p.getCustomRole();
                    IntroTypes introType = role.GetIntroType();
                    bool canWin = introType == IntroTypes.Crewmate;
                    if(canWin) winner.Add(p);
                }
            }
            if (TempData.DidImpostorsWin(endGameResult.GameOverReason))
            {
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    CustomRoles role = p.getCustomRole();
                    IntroTypes introType = role.GetIntroType();
                    bool canWin = introType == IntroTypes.Impostor || introType == IntroTypes.Madmate;
                    if(canWin) winner.Add(p);
                }
            }

            //Opportunist
            foreach(var pc in PlayerControl.AllPlayerControls) {
                if(pc.isOpportunist() && !pc.Data.IsDead)
                    TempData.winners.Add(new WinningPlayerData(pc.Data));
            }

            //廃村時の処理など
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
            if (main.currentWinner == CustomWinner.Jester && main.JesterCount > 0)
            { //Jester単独勝利
                TempData.winners = new Il2CppSystem.Collections.Generic.List<WinningPlayerData>();
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (p.PlayerId == main.ExiledJesterID)
                        TempData.winners.Add(new WinningPlayerData(p.Data));
                }
            }
            if (main.currentWinner == CustomWinner.Terrorist && main.TerroristCount> 0)
            { //Terrorist単独勝利
                TempData.winners = new Il2CppSystem.Collections.Generic.List<WinningPlayerData>();
                foreach (var p in PlayerControl.AllPlayerControls)
                {
                    if (p.PlayerId == main.WonTerroristID)
                        TempData.winners.Add(new WinningPlayerData(p.Data));
                }
            }
            //HideAndSeek専用
            if(main.IsHideAndSeek && main.currentWinner != CustomWinner.Draw) {
                var winners = new List<PlayerControl>();
                foreach(var pc in PlayerControl.AllPlayerControls) {
                    var hasRole = main.AllPlayerCustomRoles.TryGetValue(pc.PlayerId, out var role);
                    if(!hasRole) continue;
                    if(role == CustomRoles.Default) {
                        if(pc.Data.Role.IsImpostor && TempData.DidImpostorsWin(endGameResult.GameOverReason))
                            winners.Add(pc);
                        if(!pc.Data.Role.IsImpostor && TempData.DidHumansWin(endGameResult.GameOverReason))
                            winners.Add(pc);
                    }
                    if(role == CustomRoles.Fox && !pc.Data.IsDead) winners.Add(pc);
                    if(role == CustomRoles.Troll && pc.Data.IsDead) {
                        winners = new List<PlayerControl>();
                        winners.Add(pc);
                        break;
                    }
                }
                TempData.winners = new Il2CppSystem.Collections.Generic.List<WinningPlayerData>();
                foreach(var pc in winners) {
                    TempData.winners.Add(new WinningPlayerData(pc.Data));
                }
            }
            main.winnerList = "winner:";
            foreach (var wpd in TempData.winners)
            {
                main.winnerList += wpd.PlayerName;
                if(wpd != TempData.winners[TempData.winners.Count - 1]) main.winnerList += ", ";
            }
        }
    }
    [HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.SetEverythingUp))]
    class SetEverythingUpPatch
    {
        public static void Postfix(EndGameManager __instance)
        {
            // Additional code
            GameObject bonusText = UnityEngine.Object.Instantiate(__instance.WinText.gameObject);
            bonusText.transform.position = new Vector3(__instance.WinText.transform.position.x, __instance.WinText.transform.position.y - 0.5f, __instance.WinText.transform.position.z);
            bonusText.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
            TMPro.TMP_Text textRenderer = bonusText.GetComponent<TMPro.TMP_Text>();
            textRenderer.text = "";

            //特殊勝利
            if (main.currentWinner == CustomWinner.Jester)
            {
                __instance.BackgroundBar.material.color = main.getRoleColor(CustomRoles.Jester);
                textRenderer.text = $"<color={main.getRoleColorCode(CustomRoles.Jester)}>ジェスター勝利";
            }
            if (main.currentWinner == CustomWinner.Terrorist)
            {
                __instance.Foreground.material.color = Color.red;
                __instance.BackgroundBar.material.color = Color.green;
                textRenderer.text = $"<color={main.getRoleColorCode(CustomRoles.Terrorist)}>テロリスト勝利";
            }
            //引き分け処理
            if (main.currentWinner == CustomWinner.Draw)
            {
                __instance.BackgroundBar.material.color = Color.gray;
                __instance.WinText.text = "廃村";
                __instance.WinText.color = Color.white;
            }
            if(main.IsHideAndSeek) {
                foreach(var p in PlayerControl.AllPlayerControls) {
                    if(p.Data.IsDead) {
                        var hasRole = main.AllPlayerCustomRoles.TryGetValue(p.PlayerId, out var role);
                        if(hasRole && role == CustomRoles.Troll) {
                            __instance.BackgroundBar.material.color = Color.green;
                        }
                    }
                }
            }
            main.BitPlayers = new Dictionary<byte, (byte, float)>();
            main.VisibleTasksCount = false;
            if(AmongUsClient.Instance.AmHost) {
                PlayerControl.LocalPlayer.RpcSyncSettings(main.RealOptionsData);
            }
            //main.ApplySuffix();
        }
    }
}
