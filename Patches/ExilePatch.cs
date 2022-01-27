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
            //Debug Message
            if (exiled != null)
            {
                if (main.currentScientist == ScientistRoles.Jester && exiled.Role.Role == RoleTypes.Scientist && AmongUsClient.Instance.AmHost)
                {
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.JesterExiled, Hazel.SendOption.Reliable, -1);
                    writer.Write(exiled.PlayerId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    RPCProcedure.JesterExiled(exiled.PlayerId);
                }
                if (main.currentEngineer == EngineerRoles.Terrorist && exiled.Role.Role == RoleTypes.Engineer && AmongUsClient.Instance.AmHost)
                {
                    main.CheckTerroristWin(exiled);
                }
                if (main.currentEngineer == EngineerRoles.Nekomata && exiled.Role.Role == RoleTypes.Engineer && AmongUsClient.Instance.AmHost)
                {
                    var livingPlayers = new List<PlayerControl>();
                    foreach(PlayerControl p in PlayerControl.AllPlayerControls){
                        if(!p.Data.IsDead && p.PlayerId != exiled.PlayerId)livingPlayers.Add(p);
                    }
                    var pc = livingPlayers[UnityEngine.Random.Range(0, livingPlayers.Count)];
                    pc.Exiled();
                    Logger.info($"{exiled.PlayerName}はネコマタだったため{GameData.Instance.GetPlayerById(pc.PlayerId).PlayerName}を連れて行きました");
                    WrapUpPostfix(GameData.Instance.GetPlayerById(pc.PlayerId));
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.Exiled , Hazel.SendOption.Reliable, -1);
                    writer.Write(pc.PlayerId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                }
            }
            if (AmongUsClient.Instance.AmHost && main.isFixedCooldown)
            {
                main.RefixCooldownDelay = main.BeforeFixCooldown - 3f;
                PlayerControl.GameOptions.KillCooldown = main.BeforeFixCooldown;
                PlayerControl.LocalPlayer.RpcSyncSettings(PlayerControl.GameOptions);
            }
        }
    }
}
