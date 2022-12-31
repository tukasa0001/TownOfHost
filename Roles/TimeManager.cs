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
    }
}