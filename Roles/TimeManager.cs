using System.Collections.Generic;
using Hazel;
using UnityEngine;

namespace TownOfHost
{
    public static class TimeManager
    {
        static readonly int Id = 21500;
        static List<byte> playerIdList = new();
        public static OptionItem IncreaseMeetingTime;
        public static OptionItem MeetingTimeLimit;
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.TimeManager);
            IncreaseMeetingTime = IntegerOptionItem.Create(Id + 10, "TimeManagerIncreaseMeetingTime", new(5, 30, 1), 15, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.TimeManager])
                .SetValueFormat(OptionFormat.Seconds);
            MeetingTimeLimit = IntegerOptionItem.Create(Id + 11, "TimeManagerLimitMeetingTime", new(200, 900, 10), 300, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.TimeManager])
                .SetValueFormat(OptionFormat.Seconds);
        }
        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static bool IsEnable => playerIdList.Count > 0;
        private static int AdditionalTime(byte id)
        {
            var pc = Utils.GetPlayerById(id);
            if (playerIdList.Contains(id) && pc.IsAlive())
                return IncreaseMeetingTime.GetInt() * pc.GetPlayerTaskState().CompletedTasksCount;
            return 0;
        }
        public static int TotalIncreasedMeetingTime()
        {
            int sec = 0;
            foreach (var playerId in playerIdList)
                sec += AdditionalTime(playerId);
            Logger.Info($"{sec}second", "TimeManager.TotalIncreasedMeetingTime");
            return sec;
        }
    }
}