using System.Collections.Generic;
using Hazel;
using UnityEngine;

namespace TownOfHost
{
    public static class TimeManager
    {
        static readonly int Id = 21500;
        static List<byte> playerIdList = new();
        public static Dictionary<byte, int> TimeManagerTaskCount = new();
        public static OptionItem IncreaseMeetingTime;
        public static OptionItem MeetingTimeLimit;
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.TimeManager);
            IncreaseMeetingTime = FloatOptionItem.Create(Id + 10, "TimeManagerIncreaseMeetingTime", new(5f, 30f, 1f), 15f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.TimeManager])
                .SetValueFormat(OptionFormat.Seconds);
            MeetingTimeLimit = FloatOptionItem.Create(Id + 11, "TimeManagerLimitMeetingTime", new(200f, 900f, 10f), 300f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.TimeManager])
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
        }
        public static bool IsEnable() => playerIdList.Count > 0;
        public static void TimeManagerResetVotingTime(this PlayerControl timemanager)
        {
            for (var i = 0; i < TimeManagerTaskCount[timemanager.PlayerId]; i++)
                Main.VotingTime -= IncreaseMeetingTime.GetInt() * TimeManagerTaskCount[timemanager.PlayerId];
            TimeManagerTaskCount[timemanager.PlayerId] = 0; //会議時間の初期化
        }
        public static void OnCheckCompleteTask(PlayerControl player)
        {
            if (player.Is(CustomRoles.TimeManager))
            {
                TimeManagerTaskCount[player.PlayerId]++;
                Main.VotingTime += IncreaseMeetingTime.GetInt();//会議時間に増える分の会議時間を加算
                if (Main.DiscussionTime > 0)
                {
                    Main.VotingTime += Main.DiscussionTime;
                    Main.DiscussionTime = 0;//投票時間+会議時間を投票時間とし、会議時間を0にする
                }
            }
        }
    }
}