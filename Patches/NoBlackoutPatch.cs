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
using Hazel;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using InnerNet;

namespace TownOfHost {
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcMurderPlayer))]
    class RpcMurderPlayerPatch {
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target) {
            if(!AmongUsClient.Instance.AmHost) return false;
            if(target.getCustomRole() == CustomRoles.Sheriff) {
                foreach(var pc in PlayerControl.AllPlayerControls) {
                    if(pc.PlayerId == target.PlayerId) continue;
                    var clientId = pc.getClientId();
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.MurderPlayer, Hazel.SendOption.Reliable, clientId);
                    MessageExtensions.WriteNetObject(writer, target); //writer.WriteNetObject(player);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                }
                target.RpcBeKilled(__instance);
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcStartMeeting))]
    class StartMeetingRPCPatch {
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] GameData.PlayerInfo info) {
            if(!AmongUsClient.Instance.AmHost) return true;
            if (AmongUsClient.Instance.AmClient)
                __instance.StartCoroutine(__instance.CoStartMeeting(info));
            foreach(var pc in PlayerControl.AllPlayerControls) {
                if(pc.Data.IsDead && pc.isSheriff()) continue;
                int clientId = pc.getClientId();
                MessageWriter msg = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte) 14, SendOption.Reliable, clientId);
                msg.Write(info != null ? info.PlayerId : byte.MaxValue);
                AmongUsClient.Instance.FinishRpcImmediately(msg);
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.IsGameOverDueToDeath))]
    class DontBlackoutPatch {
        public static void Postfix(ShipStatus __instance, ref bool __result) {
            __result = false;
        }
    }
}