using System.Collections.Generic;
using Hazel;
using UnityEngine;

namespace TownOfHost
{
    public static class TimeThief
    {
        static readonly int Id = 2400;
        static List<byte> playerIdList = new();
        public static OptionItem KillCooldown;
        public static OptionItem DecreaseMeetingTime;
        public static OptionItem LowerLimitVotingTime;
        public static OptionItem ReturnStolenTimeUponDeath;
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.TimeThief);
            KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(2.5f, 180f, 2.5f), 30f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.TimeThief])
                .SetValueFormat(OptionFormat.Seconds);
            DecreaseMeetingTime = FloatOptionItem.Create(Id + 11, "TimeThiefDecreaseMeetingTime", new(0f, 100f, 1f), 20f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.TimeThief])
                .SetValueFormat(OptionFormat.Seconds);
            LowerLimitVotingTime = FloatOptionItem.Create(Id + 12, "TimeThiefLowerLimitVotingTime", new(1f, 300f, 1f), 10f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.TimeThief])
                .SetValueFormat(OptionFormat.Seconds);
            ReturnStolenTimeUponDeath = BooleanOptionItem.Create(Id + 13, "TimeThiefReturnStolenTimeUponDeath", true, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.TimeThief]);
        }
        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static bool IsEnable() => playerIdList.Count > 0;
        public static void ResetVotingTime(this PlayerControl thief)
        {
            if (!ReturnStolenTimeUponDeath.GetBool()) return;

            for (var i = 0; i < Main.PlayerStates[thief.PlayerId].GetKillCount(true); i++)
                Main.VotingTime += DecreaseMeetingTime.GetInt();
        }
        public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
        public static void OnCheckMurder(PlayerControl killer)
        {
            Main.DiscussionTime -= DecreaseMeetingTime.GetInt();
            if (Main.DiscussionTime < 0)
            {
                Main.VotingTime += Main.DiscussionTime;
                Main.DiscussionTime = 0;
            }
            Utils.CustomSyncAllSettings();
        }
    }
}