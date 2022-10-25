using System.Collections.Generic;
using Hazel;
using UnityEngine;

namespace TownOfHost
{
    public static class TimeThief
    {
        static readonly int Id = 2400;
        static List<byte> playerIdList = new();
        public static Dictionary<byte, int> TimeThiefKillCount = new();
        public static CustomOption KillCooldown;
        public static CustomOption DecreaseMeetingTime;
        public static CustomOption LowerLimitVotingTime;
        public static CustomOption ReturnStolenTimeUponDeath;
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.TimeThief);
            KillCooldown = CustomOption.Create(Id + 10, TabGroup.ImpostorRoles, Color.white, "KillCooldown", 30f, 2.5f, 180f, 2.5f, Options.CustomRoleSpawnChances[CustomRoles.TimeThief]);
            DecreaseMeetingTime = CustomOption.Create(Id + 11, TabGroup.ImpostorRoles, Color.white, "TimeThiefDecreaseMeetingTime", 20, 0, 100, 1, Options.CustomRoleSpawnChances[CustomRoles.TimeThief]);
            LowerLimitVotingTime = CustomOption.Create(Id + 12, TabGroup.ImpostorRoles, Color.white, "TimeThiefLowerLimitVotingTime", 10, 1, 300, 1, Options.CustomRoleSpawnChances[CustomRoles.TimeThief]);
            ReturnStolenTimeUponDeath = CustomOption.Create(Id + 13, TabGroup.ImpostorRoles, Color.white, "TimeThiefReturnStolenTimeUponDeath", true, Options.CustomRoleSpawnChances[CustomRoles.TimeThief]);
        }
        public static void Init()
        {
            TimeThiefKillCount = new();
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            TimeThiefKillCount[playerId] = 0;
            Utils.GetPlayerById(playerId)?.RpcSetKillCount();
        }
        public static bool IsEnable() => playerIdList.Count > 0;
        public static void ReceiveRPC(MessageReader msg)
        {
            byte TimeThiefId = msg.ReadByte();
            int TimeThiefKillCount = msg.ReadInt32();
            if (TimeThief.TimeThiefKillCount.ContainsKey(TimeThiefId))
                TimeThief.TimeThiefKillCount[TimeThiefId] = TimeThiefKillCount;
            else
                TimeThief.TimeThiefKillCount.Add(TimeThiefId, 0);
            Logger.Info($"Player{TimeThiefId}:ReceiveRPC", "TimeThief");
        }
        public static void RpcSetKillCount(this PlayerControl player)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetTimeThiefKillCount, Hazel.SendOption.Reliable, -1);
            writer.Write(player.PlayerId);
            writer.Write(TimeThiefKillCount[player.PlayerId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ResetVotingTime(this PlayerControl thief)
        {
            if (!ReturnStolenTimeUponDeath.GetBool()) return;

            for (var i = 0; i < TimeThiefKillCount[thief.PlayerId]; i++)
                Main.VotingTime += DecreaseMeetingTime.GetInt();
            TimeThiefKillCount[thief.PlayerId] = 0; //初期化
        }
        public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
        public static void OnCheckMurder(PlayerControl killer)
        {
            TimeThiefKillCount[killer.PlayerId]++;
            killer.RpcSetKillCount();
            Main.DiscussionTime -= DecreaseMeetingTime.GetInt();
            if (Main.DiscussionTime < 0)
            {
                Main.VotingTime += Main.DiscussionTime;
                Main.DiscussionTime = 0;
            }
            Utils.CustomSyncAllSettings();
        }
    }
}