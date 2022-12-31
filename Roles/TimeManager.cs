using System.Collections.Generic;
using Hazel;
using UnityEngine;

namespace TownOfHost
{
    public static class TimeManager
    {
        static readonly int Id = 4000;
        static List<byte> playerIdList = new();
        public static Dictionary<byte, int> TimeManagerTaskCount = new();
        public static OptionItem IncreaseMeetingTime;
        public static OptionItem MeetingTimeLimit;
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.TimeManager);
            IncreaseMeetingTime = FloatOptionItem.Create(Id + 10, "TimeManagerIncreaseMeetingTime", new(20f, 0f, 100f), 1f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.TimeManager])
                .SetValueFormat(OptionFormat.Seconds);
            MeetingTimeLimit = FloatOptionItem.Create(Id + 11, "TimeManagerLimitMeetingTime", new(300f, 150f, 500f), 1f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.TimeManager])
                .SetValueFormat(OptionFormat.Seconds);
        }
        public static void Init()
        {
            TimeManagerTaskCount = new();
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            TimeManagerTaskCount[playerId] = 0;
            Utils.GetPlayerById(playerId)?.RpcSetTimeManagerTaskCount();
        }
        public static bool IsEnable() => playerIdList.Count > 0;
        public static void ReceiveRPC(MessageReader msg)
        {
            byte TimeManagerId = msg.ReadByte();
            int TimeManagerTaskCount = msg.ReadInt32();
            if (TimeManager.TimeManagerTaskCount.ContainsKey(TimeManagerId))
                TimeManager.TimeManagerTaskCount[TimeManagerId] = TimeManagerTaskCount;
            else
                TimeManager.TimeManagerTaskCount.Add(TimeManagerId, 0);
            Logger.Info($"Player{TimeManagerId}:ReceiveRPC", "TimeManager");
        }
        public static void RpcSetTimeManagerTaskCount(this PlayerControl player)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetTimeManagerTaskCount, Hazel.SendOption.Reliable, -1);
            writer.Write(player.PlayerId);
            writer.Write(TimeManagerTaskCount[player.PlayerId]);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void TimeManagerResetVotingTime(this PlayerControl timemanager)
        {
            for (var i = 0; i < TimeManagerTaskCount[timemanager.PlayerId]; i++)
                Main.VotingTime -= IncreaseMeetingTime.GetInt();
            TimeManagerTaskCount[timemanager.PlayerId] = 0; //会議時間の初期化
        }
        public static void OnCheckCompleteTask(PlayerControl player)
        {
            TimeManagerTaskCount[player.PlayerId]++;
            player.RpcSetTimeManagerTaskCount();
            Main.DiscussionTime += IncreaseMeetingTime.GetInt();//会議時間に増える分の会議時間を加算
            if (Main.DiscussionTime > 0)
            {
                Main.VotingTime += Main.DiscussionTime;
                Main.DiscussionTime = 0;//投票時間+会議時間を投票時間とし、会議時間を0にする
            }
        }
    }
}