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
        public static CustomOption TimeThiefDecreaseMeetingTime;
        public static CustomOption TimeThiefLowerLimitVotingTime;
        public static CustomOption TimeThiefReturnStolenTimeUponDeath;
        public static Dictionary<byte, float> CurrentKillCooldown = new();
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, CustomRoles.TimeThief);
            KillCooldown = CustomOption.Create(Id + 10, Color.white, "TimeThiefKillCooldown", 30f, 2.5f, 180f, 2.5f, Options.CustomRoleSpawnChances[CustomRoles.TimeThief]);
            TimeThiefDecreaseMeetingTime = CustomOption.Create(Id + 11, Color.white, "TimeThiefDecreaseMeetingTime", 20, 0, 100, 1, Options.CustomRoleSpawnChances[CustomRoles.TimeThief]);
            TimeThiefLowerLimitVotingTime = CustomOption.Create(Id + 12, Color.white, "TimeThiefLowerLimitVotingTime", 10, 1, 300, 1, Options.CustomRoleSpawnChances[CustomRoles.TimeThief]);
            TimeThiefReturnStolenTimeUponDeath = CustomOption.Create(Id + 13, Color.white, "TimeThiefReturnStolenTimeUponDeath", true, Options.CustomRoleSpawnChances[CustomRoles.TimeThief]);
        }
        public static void Init()
        {
            TimeThiefKillCount = new();
            playerIdList = new();
        }
        public static void Add(PlayerControl pc, byte playerId)
        {
            playerIdList.Add(playerId);
            TimeThiefKillCount[playerId] = 0;
            pc.RpcSetTimeThiefKillCount();
        }
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
        public static void RpcSetTimeThiefKillCount(this PlayerControl player)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetTimeThiefKillCount, Hazel.SendOption.Reliable, -1);
            writer.Write(player.PlayerId);
            writer.Write(TimeThiefKillCount[player.PlayerId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ResetThiefVotingTime(this PlayerControl thief)
        {
            if (!TimeThiefReturnStolenTimeUponDeath.GetBool()) return;

            for (var i = 0; i < TimeThiefKillCount[thief.PlayerId]; i++)
                Main.VotingTime += TimeThiefDecreaseMeetingTime.GetInt();
            TimeThiefKillCount[thief.PlayerId] = 0; //初期化
        }
        public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CurrentKillCooldown[id];
        public static void OnCheckMurder(PlayerControl killer)
        {
            TimeThiefKillCount[killer.PlayerId]++;
            killer.RpcSetTimeThiefKillCount();
            Main.DiscussionTime -= TimeThiefDecreaseMeetingTime.GetInt();
            if (Main.DiscussionTime < 0)
            {
                Main.VotingTime += Main.DiscussionTime;
                Main.DiscussionTime = 0;
            }
            Utils.CustomSyncAllSettings();
        }
    }
}