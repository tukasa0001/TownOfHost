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
            foreach(var ds in main.SpelledPlayer)if(ds.Data.IsDead)main.SpelledPlayer.Remove(ds);
            foreach(var cd in main.CursedPlayerDie)if(cd.Data.IsDead)main.CursedPlayerDie.Remove(cd);
            main.SpelledPlayer.RemoveAll(pc => pc == null || pc.Data == null || pc.Data.IsDead || pc.Data.Disconnected);
            if (exiled != null)
            {
                var role = exiled.getCustomRole();
                if (role == CustomRoles.Witch)
                {
                    main.SpelledPlayer.Clear();
                }
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
            foreach(var p in main.CursedPlayerDie)
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
            }
            main.FirstCursedCheck = false;
            main.CustomSyncAllSettings();
            main.NotifyRoles();
            main.witchMeeting = false;
        }
    }
}
