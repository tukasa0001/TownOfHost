using System.Diagnostics;
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
        public static CustomRoles getCustomRole(this GameData.PlayerInfo player)
        {
            return main.getPlayerById(player.PlayerId).getCustomRole();
        }

        public static CustomRoles getCustomRole(this PlayerControl player) {
            var cRoleFound = main.AllPlayerCustomRoles.TryGetValue(player.PlayerId, out var cRole);
            if(!cRoleFound)
            {
                Logger.info($"{player.name}:{player.Data.Role.Role.ToString()}");
                switch(player.Data.Role.Role)
                {
                    case RoleTypes.Crewmate:
                        cRole = CustomRoles.Default;
                        break;
                    case RoleTypes.Engineer:
                        cRole = CustomRoles.Engineer;
                        break;
                    case RoleTypes.Scientist:
                        cRole = CustomRoles.Scientist;
                        break;
                    case RoleTypes.GuardianAngel:
                        cRole = CustomRoles.GuardianAngel;
                        break;
                    case RoleTypes.Impostor:
                        cRole = CustomRoles.Impostor;
                        break;
                    case RoleTypes.Shapeshifter:
                        cRole = CustomRoles.Shapeshifter;
                        break;
                    default:
                        cRole = CustomRoles.Default;
                        break;
                }
            }
            return cRole;
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
        public static bool isJester(this PlayerControl target){return target.getCustomRole() == CustomRoles.Jester;}
        public static bool isMadmate(this PlayerControl target){return target.getCustomRole() == CustomRoles.Madmate;}
        public static bool isBait(this PlayerControl target){return target.getCustomRole() == CustomRoles.Bait;}
        public static bool isTerrorist(this PlayerControl target){return target.getCustomRole() == CustomRoles.Terrorist;}
        public static bool isMafia(this PlayerControl target){return target.getCustomRole() == CustomRoles.Mafia;}
        public static bool isVampire(this PlayerControl target){return target.getCustomRole() == CustomRoles.Vampire;}
        public static bool isSabotageMaster(this PlayerControl target){return target.getCustomRole() == CustomRoles.SabotageMaster;}
        public static bool isMadGuardian(this PlayerControl target){return target.getCustomRole() == CustomRoles.MadGuardian;}
        public static bool isMayor(this PlayerControl target){return target.getCustomRole() == CustomRoles.Mayor;}
        public static bool isOpportunist(this PlayerControl target){return target.getCustomRole() == CustomRoles.Opportunist;}
        public static bool isSnitch(this PlayerControl target){return target.getCustomRole() == CustomRoles.Snitch;}
    }
}
