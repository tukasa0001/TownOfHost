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
                var B_id = exiled.PlayerId;
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
                if (B_id == main.b_target.PlayerId)
                {
                    var rand = new System.Random();
                    main.BountyTargetPlayer = new List<PlayerControl>();
                    foreach (var p in PlayerControl.AllPlayerControls)if(!p.Data.IsDead && p.Data.Role.Role != RoleTypes.Impostor)main.BountyTargetPlayer.Add(p);
                    main.b_target = main.BountyTargetPlayer[rand.Next(0,main.BountyTargetPlayer.Count - 1)];
                    main.BountyCheck = true;
                }
            }
            foreach(var p in main.SpelledPlayer)
            {
                p.RpcMurderPlayer(p);
            }
            if (AmongUsClient.Instance.AmHost && main.isFixedCooldown)
            {
                main.RefixCooldownDelay = main.RealOptionsData.KillCooldown - 3f;
            }
            if (main.b_target.Data.IsDead)
            {
                var rand = new System.Random();
                main.BountyTargetPlayer = new List<PlayerControl>();
                foreach (var p in PlayerControl.AllPlayerControls)if(!p.Data.IsDead && p.Data.Role.Role != RoleTypes.Impostor)main.BountyTargetPlayer.Add(p);
                main.b_target = main.BountyTargetPlayer[rand.Next(0,main.BountyTargetPlayer.Count - 1)];
                main.BountyCheck = true;
            }
            main.CustomSyncAllSettings();
            main.NotifyRoles();
            main.witchMeeting = false;
        }
    }
}
