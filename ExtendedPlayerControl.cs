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
        public static void RpcSetCustomRole(this PlayerControl player, CustomRoles role) {
            main.AllPlayerCustomRoles[player.PlayerId] = role;
            if(AmongUsClient.Instance.AmHost) {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCustomRole, Hazel.SendOption.Reliable, -1);
                writer.Write(player.PlayerId);
                writer.Write((byte)role);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
        }
        public static void RpcSetCustomRole(byte PlayerId, CustomRoles role) {
            if(AmongUsClient.Instance.AmHost) {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCustomRole, Hazel.SendOption.Reliable, -1);
                writer.Write(PlayerId);
                writer.Write((byte)role);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
        }
        public static void SetCustomRole(this PlayerControl player, CustomRoles role) {
            main.AllPlayerCustomRoles[player.PlayerId] = role;
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

        public static RoleTypes getSyncedRoleTypes(this GameData.PlayerInfo player) {
            var isSynced = main.SyncedPlayerRoleTypes.TryGetValue(player.PlayerId, out var role);
            if(!isSynced) return player.Role.Role;
            else return role;
        }
        public static RoleTypes getSyncedRoleTypes(this PlayerControl player) => player.Data.getSyncedRoleTypes();
        public static void RpcSyncRoleTypes(this PlayerControl player) {
            if(!AmongUsClient.Instance.AmHost) return;
            var role = player.Data.Role.Role;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncRoleTypes, Hazel.SendOption.Reliable, -1);
            writer.Write(player.PlayerId);
            writer.Write((byte)role);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public static void RpcSetNamePrivate(this PlayerControl player, string name, bool DontShowOnModdedClient = false, PlayerControl seer = null) {
            //player: 名前の変更対象
            //seer: 上の変更を確認することができるプレイヤー

            if(player == null || name == null) return;
            if(seer == null) seer = player;
            var clientId = seer.getClientId();
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.SetName, Hazel.SendOption.Reliable, clientId);
            writer.Write(name);
            writer.Write(DontShowOnModdedClient);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void RpcSetRoleDesync(this PlayerControl player, RoleTypes role, PlayerControl seer = null) {
            //player: 名前の変更対象
            //seer: 上の変更を確認することができるプレイヤー

            if(player == null) return;
            if(seer == null) seer = player;
            var clientId = seer.getClientId();
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.SetRole, Hazel.SendOption.Reliable, clientId);
            writer.Write((ushort) role);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public static void RpcGuardAndKill(this PlayerControl killer, PlayerControl target = null) {
            if(target == null) target = killer;
            killer.RpcProtectPlayer(target, 0);
            new LateTask(() => {
                if(target.protectedByGuardian)
                    killer.RpcMurderPlayer(target);
            }, 0.2f, "GuardAndKill");
        }

        public static byte GetRoleCount(this Dictionary<CustomRoles, byte> dic, CustomRoles role) {
            if(!dic.ContainsKey(role))
                dic[role] = 0;
            return dic[role];
        }

        public static bool canBeKilledBySheriff(this PlayerControl player) {
            var cRole = player.getCustomRole();
            bool canBeKilled = false;
            switch(cRole) {
                case CustomRoles.Jester:
                case CustomRoles.MadGuardian:
                case CustomRoles.Madmate:
                case CustomRoles.Terrorist:
                case CustomRoles.Sidekick:
                case CustomRoles.Vampire:
                    canBeKilled = true;
                    break;
                case CustomRoles.Default:
                    canBeKilled = player.Data.Role.TeamType == RoleTeamTypes.Impostor;
                    break;
            }
            return canBeKilled;
        }
    }
}
