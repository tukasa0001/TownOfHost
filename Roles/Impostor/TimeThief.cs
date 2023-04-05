using System.Collections.Generic;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Impostor
{
    public sealed class TimeThief : RoleBase
    {
        public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(TimeThief),
            player => new TimeThief(player),
            CustomRoles.TimeThief,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            2400,
            SetupOptionItem
        );
        public TimeThief(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
        {
            KillCooldown = OptionKillCooldown.GetFloat();
            DecreaseMeetingTime = OptionDecreaseMeetingTime.GetInt();
            LowerLimitVotingTime = OptionLowerLimitVotingTime.GetInt();
            ReturnStolenTimeUponDeath = OptionReturnStolenTimeUponDeath.GetBool();

            TimeThiefs.Add(this);
        }
        private static OptionItem OptionKillCooldown;
        private static OptionItem OptionDecreaseMeetingTime;
        private static OptionItem OptionLowerLimitVotingTime;
        private static OptionItem OptionReturnStolenTimeUponDeath;
        enum OptionName
        {
            KillCooldown,
            TimeThiefDecreaseMeetingTime,
            TimeThiefLowerLimitVotingTime,
            TimeThiefReturnStolenTimeUponDeath
        }
        public static float KillCooldown;
        public static int DecreaseMeetingTime;
        public static int LowerLimitVotingTime;
        public static bool ReturnStolenTimeUponDeath;

        public static HashSet<TimeThief> TimeThiefs = new(3);
        private static void SetupOptionItem()
        {
            OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.KillCooldown, new(2.5f, 180f, 2.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionDecreaseMeetingTime = IntegerOptionItem.Create(RoleInfo, 11, OptionName.TimeThiefDecreaseMeetingTime, new(0, 100, 1), 20, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionLowerLimitVotingTime = IntegerOptionItem.Create(RoleInfo, 12, OptionName.TimeThiefLowerLimitVotingTime, new(1, 300, 1), 10, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionReturnStolenTimeUponDeath = BooleanOptionItem.Create(RoleInfo, 13, OptionName.TimeThiefReturnStolenTimeUponDeath, true, false);
        }
        public override float SetKillCooldown() => KillCooldown;
        private static int StolenTime(byte playerId)
        {
            var player = Utils.GetPlayerById(playerId);
            if (player.Is(CustomRoles.TimeThief) && (player.IsAlive() || !ReturnStolenTimeUponDeath))
                return DecreaseMeetingTime * Main.PlayerStates[player.PlayerId].GetKillCount(true);
            return 0;
        }
        public static int TotalDecreasedMeetingTime()
        {
            int sec = 0;
            foreach (var timeThief in TimeThiefs)
                sec -= StolenTime(timeThief.Player.PlayerId);
            Logger.Info($"{sec}second", "TimeThief.TotalDecreasedMeetingTime");
            return sec;
        }
        public override string GetProgressText(bool comms = false)
        {
            var time = StolenTime(Player.PlayerId);
            return time > 0 ? Utils.ColorString(Palette.ImpostorRed.ShadeColor(0.5f), $"{-time}s") : "";
        }
    }
}