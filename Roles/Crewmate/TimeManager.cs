using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Crewmate
{
    public sealed class TimeManager : RoleBase, IMeetingTimeAlterable
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(TimeManager),
                player => new TimeManager(player),
                CustomRoles.TimeManager,
                () => RoleTypes.Crewmate,
                CustomRoleTypes.Crewmate,
                21500,
                SetupOptionItem,
                "tm",
                "#6495ed"
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

        public bool RevertOnDie => true;

        private static void SetupOptionItem()
        {
            OptionIncreaseMeetingTime = IntegerOptionItem.Create(RoleInfo, 10, OptionName.TimeManagerIncreaseMeetingTime, new(5, 30, 1), 15, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionMeetingTimeLimit = IntegerOptionItem.Create(RoleInfo, 11, OptionName.TimeManagerLimitMeetingTime, new(200, 900, 10), 300, false)
                .SetValueFormat(OptionFormat.Seconds);
        }

        public int CalculateMeetingTimeDelta()
        {
            var sec = IncreaseMeetingTime * MyTaskState.CompletedTasksCount;
            return sec;
        }
        public override string GetProgressText(bool comms = false)
        {
            var time = CalculateMeetingTimeDelta();
            return time > 0 ? Utils.ColorString(Utils.GetRoleColor(CustomRoles.TimeManager).ShadeColor(0.5f), $"+{time}s") : "";
        }
    }
}