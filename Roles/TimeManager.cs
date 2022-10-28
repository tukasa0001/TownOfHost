using System.Collections.Generic;
using Hazel;
using UnityEngine;

namespace TownOfHost
{
    public static class TimeManager
    {
        static readonly int Id = 100000;
        static List<byte> playerIdList = new();
        public static Dictionary<byte, int> TimeManagerKillCount = new();
        public static CustomOption IncreaseMeetingTime;
        public static CustomOption MeetingTimeLimit;
        public static void SetupCustomOption()
        {
            IncreaseMeetingTime = CustomOption.Create(Id + 10, TabGroup.CrewmateRoles, Color.white, "TimeManagerIncreaseMeetingTime", 20, 0, 100, 1, Options.CustomRoleSpawnChances[CustomRoles.TimeManager]);
            MeetingTimeLimit = CustomOption.Create(Id + 11, TabGroup.CrewmateRoles, Color.white, "TimeManagerLimitMeetingTime", 400, 150, 600, 1, Options.CustomRoleSpawnChances[CustomRoles.TimeManager]);
        }
        public static void Init()
        {
            TimeManagerKillCount = new();
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            TimeManagerKillCount[playerId] = 0;
            Utils.GetPlayerById(playerId)?.RpcSetTimeManagerKillCount();
        }
        public static bool IsEnable() => playerIdList.Count > 0;
        public static void ReceiveRPC(MessageReader msg)
        {
            byte TimeManagerId = msg.ReadByte();
            int TimeManagerKillCount = msg.ReadInt32();
            if (TimeManager.TimeManagerKillCount.ContainsKey(TimeManagerId))
                TimeManager.TimeManagerKillCount[TimeManagerId] = TimeManagerKillCount;
            else
                TimeThief.TimeThiefKillCount.Add(TimeManagerId, 0);
            Logger.Info($"Player{TimeManagerId}:ReceiveRPC", "TimeThief");
        }
        public static void RpcSetTimeManagerKillCount(this PlayerControl player)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetTimeManagerKillCount, Hazel.SendOption.Reliable, -1);
            writer.Write(player.PlayerId);
            writer.Write(TimeManagerKillCount[player.PlayerId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void TimeManagerResetVotingTime(this PlayerControl timemanager)
        {
            for (var i = 0; i < TimeManagerKillCount[timemanager.PlayerId]; i++)
                Main.VotingTime += IncreaseMeetingTime.GetInt();
            TimeManagerKillCount[timemanager.PlayerId] = 0; //会議時間の初期化
        }
        public static void OnCheckMurder(PlayerControl killer)
        {
            TimeManagerKillCount[killer.PlayerId]++;
            killer.RpcSetKillCount();
            Main.DiscussionTime -= IncreaseMeetingTime.GetInt();
            if (Main.DiscussionTime < 0)
            {
                Main.VotingTime += Main.DiscussionTime;
                Main.DiscussionTime = 0;
            }
        }
    }
}