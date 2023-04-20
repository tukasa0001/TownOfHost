using System.Collections.Generic;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Crewmate
{
    public sealed class TimeManager : RoleBase
    {
        public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(TimeManager),
            player => new TimeManager(player),
            CustomRoles.TimeManager,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            21500,
            SetupOptionItem
        );
        public TimeManager(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
        {
            IncreaseMeetingTime = OptionIncreaseMeetingTime.GetInt();
            MeetingTimeLimit = OptionMeetingTimeLimit.GetInt();
        }
        private static OptionItem OptionIncreaseMeetingTime;
        private static OptionItem OptionMeetingTimeLimit;
        enum OptionName
        {
            TimeManagerIncreaseMeetingTime,
            TimeManagerLimitMeetingTime
        }
        public static int IncreaseMeetingTime;
        public static int MeetingTimeLimit;

        private static void SetupOptionItem()
        {
            OptionIncreaseMeetingTime = IntegerOptionItem.Create(RoleInfo, 10, OptionName.TimeManagerIncreaseMeetingTime, new(5, 50, 1), 20, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionMeetingTimeLimit = IntegerOptionItem.Create(RoleInfo, 11, OptionName.TimeManagerLimitMeetingTime, new(250, 500, 10), 300, false)
                .SetValueFormat(OptionFormat.Seconds);
        }

        public int CalculateMeetingTimeDelta()
        {
            var sec = IncreaseMeetingTime * Player.GetPlayerTaskState().CompletedTasksCount;
            return sec;
        }





        /*
                private static int AdditionalTime(PlayerControl player)
                {
                    if (player.Is(CustomRoles.TimeThief) && player.IsAlive())
                        return OptionIncreaseMeetingTime.GetInt() * player.GetPlayerTaskState().CompletedTasksCount;
                    return 0;
                }

                public static int TotalIncreasedMeetingTime()
                {
                    int sec = 0;
                    foreach (var timemanager in TimeManagers)
                        sec += AdditionalTime(playerId);
                    Logger.Info($"{sec}second", "TimeManager.TotalIncreasedMeetingTime");
                    return sec;
                }*/
    }
}