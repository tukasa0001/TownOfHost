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
namespace TownOfHost {
    static class ExtendedPlayerControl {
        public static void RpcSetHideAndSeekRole(this PlayerControl player, HideAndSeekRoles role) {
            if(AmongUsClient.Instance.AmClient) {
                player.SetHideAndSeekRole(role);
            }
            if(AmongUsClient.Instance.AmHost) {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetHideAndSeekRole, Hazel.SendOption.Reliable, -1);
                writer.Write(player.PlayerId);
                writer.Write((byte)role);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
        }

        public static void RpcExile(this PlayerControl player) {
            main.ExileAsync(player);
        }
        public static InnerNet.ClientData getClient(this PlayerControl player) {
            var client = AmongUsClient.Instance.allClients.ToArray().Where(cd => cd.Character.PlayerId == player.PlayerId).FirstOrDefault();
            return client;
        }
        public static int getClientId(this PlayerControl player) {
            var client = player.getClient();
            if(client == null) return -1;
            return client.Id;
        }

        public static CustomRoles getCustomRole(this PlayerControl player) {
            var cRoleFound = main.AllPlayerCustomRoles.TryGetValue(player.PlayerId, out var cRole);
            if(cRoleFound) return cRole;
            else return CustomRoles.Default;
        }

        public static void RpcGuardAndKill(this PlayerControl killer, PlayerControl target = null) {
            if(target == null) target = killer;
            killer.RpcProtectPlayer(target, 0);
            new LateTask(() => {
                if(target.protectedByGuardian)
                    killer.RpcMurderPlayer(target);
            }, 0.2f, "GuardAndKill");
        }
    }
}
