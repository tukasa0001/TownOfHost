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
            foreach (var ds in main.SpelledPlayer)if(ds.Data.IsDead)main.SpelledPlayer.Remove(ds);
            if (exiled != null)
            {
                var role = exiled.getCustomRole();
                if (role == CustomRoles.Witch)
                {
                    main.SpelledPlayer.Clear();
                }
                if (role != CustomRoles.Witch)
                {
                    foreach(var p in main.SpelledPlayer)
                    {
                        p.RpcMurderPlayer(p);
                    }
                    //ここでSpelledPlayerをClearしたらいいのでは？
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
            }
            if (exiled == null)
            {
                foreach(var p in main.SpelledPlayer)
                {
                    p.RpcMurderPlayer(p);
                }
            }
            if (AmongUsClient.Instance.AmHost && main.isFixedCooldown)
            {
                main.RefixCooldownDelay = main.RealOptionsData.KillCooldown - 3f;
            }
            main.CustomSyncAllSettings();
            main.NotifyRoles();
            main.witchMeeting = false;
        }
    }
}
