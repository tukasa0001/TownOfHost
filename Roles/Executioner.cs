/*
* Executioner.cs created on Wed Aug 31 2022
* This software is released under the GNU General Public License v3.0.
* Copyright (c) 2022 空き瓶/EmptyBottle
*/

using System.Collections.Generic;
using UnityEngine;
using HarmonyLib;
using Hazel;
using static TownOfHost.Options;

namespace TownOfHost
{
    public static class Executioner
    {
        private static readonly int Id = 50700;
        public static List<byte> playerIdList = new();
        public static byte WinnerID;

        private static CustomOption CanTargetImpostor;
        private static CustomOption CanTargetNeutralKiller;
        public static CustomOption ChangeRolesAfterTargetKilled;


        /// <summary>
        /// Key: エクスキューショナーのPlayerId, Value: ターゲットのPlayerId
        /// </summary>
        public static Dictionary<byte, byte> Target = new();
        public static readonly string[] ChangeRoles =
        {
            CustomRoles.Crewmate.ToString(), CustomRoles.Jester.ToString(), CustomRoles.Opportunist.ToString(),
        };
        public static readonly CustomRoles[] CRoleChangeRoles =
        {
            CustomRoles.Crewmate, CustomRoles.Jester, CustomRoles.Opportunist,
        };

        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, CustomRoles.Executioner);
            CanTargetImpostor = CustomOption.Create(Id + 10, Color.white, "ExecutionerCanTargetImpostor", false, CustomRoleSpawnChances[CustomRoles.Executioner]);
            CanTargetNeutralKiller = CustomOption.Create(Id + 12, Color.white, "ExecutionerCanTargetNeutralKiller", false, CustomRoleSpawnChances[CustomRoles.Executioner]);
            ChangeRolesAfterTargetKilled = CustomOption.Create(Id + 11, Color.white, "ExecutionerChangeRolesAfterTargetKilled", ChangeRoles, ChangeRoles[1], CustomRoleSpawnChances[CustomRoles.Executioner]);
        }
        public static void Init()
        {
            playerIdList = new();
            Target = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);

            List<PlayerControl> targetList = new();
            var rand = new System.Random();
            foreach (var target in PlayerControl.AllPlayerControls)
            {
                if (playerId == target.PlayerId) continue;
                else if (!CanTargetImpostor.GetBool() && target.Is(RoleType.Impostor)) continue;
                else if (!CanTargetNeutralKiller.GetBool() && target.IsNeutralKiller()) continue;
                if (target.Is(CustomRoles.GM)) continue;

                targetList.Add(target);
            }
            var SelectedTarget = targetList[rand.Next(targetList.Count)];
            Target.Add(playerId, SelectedTarget.PlayerId);
            SendRPC(playerId, SelectedTarget.PlayerId, "SetTarget");
            Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()}:{SelectedTarget.GetNameWithRole()}", "Executioner");
        }
        public static bool IsEnable() => playerIdList.Count > 0;
        public static void SendRPC(byte executionerId, byte targetId = 0x73, string Progress = "")
        {
            MessageWriter writer;
            switch (Progress)
            {
                case "SetTarget":
                    writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetExecutionerTarget, SendOption.Reliable);
                    writer.Write(executionerId);
                    writer.Write(targetId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    break;
                case "":
                    if (!AmongUsClient.Instance.AmHost) return;
                    writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RemoveExecutionerTarget, SendOption.Reliable);
                    writer.Write(executionerId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    break;
                case "WinCheck":
                    writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EndGame, SendOption.Reliable, -1);
                    writer.Write((byte)CustomWinner.Executioner);
                    writer.Write(executionerId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    RPC.ExecutionerWin(executionerId);
                    break;
            }
        }
        public static void ReceiveRPC(MessageReader reader, bool SetTarget)
        {
            if (SetTarget)
            {
                byte ExecutionerId = reader.ReadByte();
                byte TargetId = reader.ReadByte();
                Target[ExecutionerId] = TargetId;
            }
            else
                Target.Remove(reader.ReadByte());
        }
        public static void ChangeRoleByTarget(PlayerControl target)
        {
            byte Executioner = 0x73;
            Target.Do(x =>
            {
                if (x.Value == target.PlayerId)
                    Executioner = x.Key;
            });
            Utils.GetPlayerById(Executioner).RpcSetCustomRole(CRoleChangeRoles[ChangeRolesAfterTargetKilled.GetSelection()]);
            Target.Remove(Executioner);
            SendRPC(Executioner);
            Utils.NotifyRoles();
        }
        public static void ChangeRole(PlayerControl executioner)
        {
            executioner.RpcSetCustomRole(CRoleChangeRoles[ChangeRolesAfterTargetKilled.GetSelection()]);
            Target.Remove(executioner.PlayerId);
            SendRPC(executioner.PlayerId);
        }
        public static string TargetMark(PlayerControl seer, PlayerControl target)
        {
            if (!seer.Is(CustomRoles.Executioner)) return ""; //エクスキューショナー以外処理しない

            var GetValue = Target.TryGetValue(seer.PlayerId, out var targetId);
            return GetValue && targetId == target.PlayerId ? Helpers.ColorString(Utils.GetRoleColor(CustomRoles.Executioner), "♦") : "";
        }
        public static void CheckExileTarget(GameData.PlayerInfo exiled, bool DecidedWinner)
        {
            foreach (var kvp in Target)
            {
                var executioner = Utils.GetPlayerById(kvp.Key);
                if (executioner == null) continue;
                if (executioner.Data.IsDead || executioner.Data.Disconnected) continue; //Keyが死んでいたらor切断していたらこのforeach内の処理を全部スキップ
                if (kvp.Value == exiled.PlayerId && AmongUsClient.Instance.AmHost && !DecidedWinner)
                {
                    SendRPC(kvp.Key, Progress: "WinCheck");
                    break; //脱ループ
                }
            }
        }
    }
}