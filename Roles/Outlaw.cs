using System;
using System.Collections.Generic;
using Hazel;
using static UnityEngine.GraphicsBuffer;
using HarmonyLib;
using UnityEngine;
using static TownOfHost.Options;
using Il2CppSystem.Net;

namespace TownOfHost
{
    internal class Outlaw
    {
        private static readonly int Id = 60000;
        public static List<byte> playerIdList = new();
        public static byte WinnerID;

        public static CustomOption ChangeRolesAfterSheriffKilled;
        public static CustomOption OutlawCanVent;
        public static CustomOption OutlawCanUseSabotage;
        public static CustomOption OutlawHasImpostorVision;

        public static Dictionary<byte, byte> Sheriff = new();
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
            Options.SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Outlaw, 1);
            //ChangeRolesAfterSheriffKilled = CustomOption.Create(Id + 10, TabGroup.NeutralRoles, Color.white, "OutlawChangeRolesAfterSheriffKilled", ChangeRoles, ChangeRoles[1], CustomRoleSpawnChances[CustomRoles.Outlaw]);
            OutlawCanVent = CustomOption.Create(Id + 11, TabGroup.NeutralRoles, Color.white, "CanVent", true, CustomRoleSpawnChances[CustomRoles.Outlaw]);
            OutlawCanUseSabotage = CustomOption.Create(Id + 12, TabGroup.NeutralRoles, Color.white, "CanUseSabotage", true, CustomRoleSpawnChances[CustomRoles.Outlaw]);
            OutlawHasImpostorVision = CustomOption.Create(Id + 13, TabGroup.NeutralRoles, Color.white, "ImpostorVision", true, CustomRoleSpawnChances[CustomRoles.Outlaw]);
        }
        public static void Init()
        {
            playerIdList = new();
            Sheriff = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);

            //assign sheriff
            if (AmongUsClient.Instance.AmHost)
            {
                List<PlayerControl> targetList = new();
                var rand = new System.Random();
                foreach (var target in PlayerControl.AllPlayerControls)
                {
                    if (playerId == target.PlayerId) continue;
                    if (target.Is(CustomRoles.GM)) continue;
                    if (target.Is(CustomRoles.Sheriff))

                    targetList.Add(target);
                }
                var SelectedTarget = targetList[rand.Next(targetList.Count)];
                Sheriff.Add(playerId, SelectedTarget.PlayerId);
                SendRPC(playerId, SelectedTarget.PlayerId, "SetTarget");
                Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()}:{SelectedTarget.GetNameWithRole()}", "Outlaw");
            }
        }

        public static bool IsEnable => playerIdList.Count > 0;
        public static void SendRPC(byte OutlawId, byte targetId = 0x75, string Progress = "")
        {
            MessageWriter writer;
            switch (Progress)
            {
                case "SetTarget":
                    writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetOutlawTarget, SendOption.Reliable);
                    writer.Write(OutlawId);
                    writer.Write(targetId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    break;
                case "":
                    if (!AmongUsClient.Instance.AmHost) return;
                    writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.RemoveOutlawTarget, SendOption.Reliable);
                    writer.Write(OutlawId);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    break;
                case "WinCheck":
                    if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) break; //まだ勝者が設定されていない場合
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Outlaw);
                    CustomWinnerHolder.WinnerIds.Add(OutlawId);
                    break;
            }
        }
        public static void ReceiveRPC(MessageReader reader, bool SetTarget)
        {
            if (SetTarget)
            {
                byte OutlawId = reader.ReadByte();
                byte TargetId = reader.ReadByte();
                Sheriff[OutlawId] = TargetId;
            }
            else
                Sheriff.Remove(reader.ReadByte());
        }
        public static void ChangeRoleByTarget(PlayerControl target)
        {
            byte Outlaw = 0x73;
            Sheriff.Do(x =>
            {
                if (x.Value == target.PlayerId)
                    Outlaw = x.Key;
            });
            Utils.GetPlayerById(Outlaw).RpcSetCustomRole(CRoleChangeRoles[ChangeRolesAfterSheriffKilled.GetSelection()]);
            Sheriff.Remove(Outlaw);
            SendRPC(Outlaw);
            Utils.NotifyRoles();
        }
        public static void ChangeRole(PlayerControl outlaw)
        {
            outlaw.RpcSetCustomRole(CRoleChangeRoles[ChangeRolesAfterSheriffKilled.GetSelection()]);
            Sheriff.Remove(outlaw.PlayerId);
            SendRPC(outlaw.PlayerId);
        }
    }
}
