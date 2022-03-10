using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using Hazel;
using System;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnhollowerBaseLib;
using TownOfHost;
using System.Threading.Tasks;
using System.Threading;

namespace TownOfHost
{
    class ExileControllerWrapUpPatch
    {
        [HarmonyPatch(typeof(ExileController), nameof(ExileController.WrapUp))]
        class BaseExileControllerPatch
        {
            public static void Postfix(ExileController __instance)
            {
                WrapUpPostfix(__instance.exiled);
            }
        }

        [HarmonyPatch(typeof(AirshipExileController), nameof(AirshipExileController.WrapUpAndSpawn))]
        class AirshipExileControllerPatch
        {
            public static void Postfix(AirshipExileController __instance)
            {
                WrapUpPostfix(__instance.exiled);
            }
        }
        static void WrapUpPostfix(GameData.PlayerInfo exiled)
        {
            main.CursedPlayerDie.RemoveAll(pc => pc == null || pc.isAlive() == false);//呪われた人が死んだ場合にリストから削除する
            main.SpelledPlayer.RemoveAll(pc => pc == null || pc.isAlive() == false);
            foreach(var p in main.SpelledPlayer)
            {
                main.ps.setDeathReason(p.PlayerId, PlayerState.DeathReason.Kill);
                main.IgnoreReportPlayers.Add(p.PlayerId);
                p.RpcMurderPlayer(p);
            }
            if (exiled != null)
            {
                var role = exiled.getCustomRole();
                if (role == CustomRoles.Jester && AmongUsClient.Instance.AmHost)
                {
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.JesterExiled, Hazel.SendOption.Reliable, -1);
                    writer.Write(exiled.PlayerId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    RPCProcedure.JesterExiled(exiled.PlayerId);
                }
                if (role == CustomRoles.Terrorist && AmongUsClient.Instance.AmHost)
                {
                    main.CheckTerroristWin(exiled);
                }
                if (role == CustomRoles.BlackCat && AmongUsClient.Instance.AmHost)
                {
                    DieAlongside(exiled.Object);
                }
                
                main.ps.setDeathReason(exiled.PlayerId,PlayerState.DeathReason.Vote);
            }
            foreach(var p in main.SpelledPlayer)
            {
                p.RpcMurderPlayer(p);
            }
            foreach(var p in main.CursedPlayerDie)//呪われた人を確定で殺す
            {
                p.RpcMurderPlayer(p);
            }
            if (AmongUsClient.Instance.AmHost && main.isFixedCooldown)
            {
                main.RefixCooldownDelay = main.RealOptionsData.KillCooldown - 3f;
            }
            foreach(var wr in PlayerControl.AllPlayerControls){
                if(wr.isWarlock())wr.RpcGuardAndKill(wr);
                main.CursedPlayers.Remove(wr.PlayerId);
                main.FirstCursedCheck.Remove(wr.PlayerId);
                main.FirstCursedCheck.Add(wr.PlayerId, false);
            }
            foreach(var wr in PlayerControl.AllPlayerControls)if(wr.isSerialKiller())wr.RpcGuardAndKill(wr);
            foreach(var wr in PlayerControl.AllPlayerControls)if(wr.isSerialKiller())main.SerialKillerTimer.Add(wr.PlayerId,0f);
            main.CustomSyncAllSettings();
            main.NotifyRoles();
            main.witchMeeting = false;
        }

        // 黒猫の道連れ処理
        private static void DieAlongside(PlayerControl blackCatPlayer)
        {
            var crews = new List<PlayerControl>();

            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc != null && pc.isAlive() == false) continue;

                var role = pc.getCustomRole();
                if (role.isCrewmateTeam() || (role.isNeutralTeam() && main.RevengeOnNeutral))
                {
                    crews.Add(pc);                            
                }
            }

            if (crews.Any() == false) return;
            
            var index = new System.Random().Next(0, crews.Count);
            var target = crews[index];
            blackCatPlayer.RpcMurderPlayer(target);
            main.IgnoreReportPlayers.Add(target.PlayerId);
            main.ps.setDeathReason(target.PlayerId, PlayerState.DeathReason.Revenge);
        }
    }
}
