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
            IncreaseMeetingTime = FloatOptionItem.Create(Id + 10, "TimeManagerIncreaseMeetingTime", new(5f, 30f, 1f), 15f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.TimeManager])
                .SetValueFormat(OptionFormat.Seconds);
            MeetingTimeLimit = FloatOptionItem.Create(Id + 11, "TimeManagerLimitMeetingTime", new(200f, 900f, 10f), 300f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.TimeManager])
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
    }
}