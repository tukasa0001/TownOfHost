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
            main.CursedPlayerDie.RemoveAll(pc => pc == null || pc.Data == null || pc.Data.IsDead || pc.Data.Disconnected);//呪われた人が死んだ場合にリストから削除する
            main.SpelledPlayer.RemoveAll(pc => pc == null || pc.Data == null || pc.Data.IsDead || pc.Data.Disconnected);
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

            if (main.isLovers && main.isLoversDead == false) {
                foreach(var loversPlayer in main.LoversPlayers) {
                    if (exiled.PlayerId == loversPlayer.PlayerId) {
                        // Loversが死んだとき
                        main.isLoversDead = true;
                        foreach(var partnerPlayer in main.LoversPlayers) {
                            //残った恋人を全て殺す(2人以上可)
                            if (loversPlayer.PlayerId != partnerPlayer.PlayerId)
                            {
                                loversPlayer.RpcMurderPlayer(partnerPlayer);
                                main.IgnoreReportPlayers.Add(partnerPlayer.PlayerId);
                            }
                        }
                    }
                }
            }
            
            main.CustomSyncAllSettings();
            main.NotifyRoles();
            main.witchMeeting = false;
        }
    }
}
