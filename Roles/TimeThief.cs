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
            KillCooldown = OptionItem.Create(Id + 10, TabGroup.ImpostorRoles, Color.white, "KillCooldown", 30f, 2.5f, 180f, 2.5f, Options.CustomRoleSpawnChances[CustomRoles.TimeThief], format: OptionFormat.Seconds);
            DecreaseMeetingTime = OptionItem.Create(Id + 11, TabGroup.ImpostorRoles, Color.white, "TimeThiefDecreaseMeetingTime", 20, 0, 100, 1, Options.CustomRoleSpawnChances[CustomRoles.TimeThief], format: OptionFormat.Seconds);
            LowerLimitVotingTime = OptionItem.Create(Id + 12, TabGroup.ImpostorRoles, Color.white, "TimeThiefLowerLimitVotingTime", 10, 1, 300, 1, Options.CustomRoleSpawnChances[CustomRoles.TimeThief], format: OptionFormat.Seconds);
            ReturnStolenTimeUponDeath = OptionItem.Create(Id + 13, TabGroup.ImpostorRoles, Color.white, "TimeThiefReturnStolenTimeUponDeath", true, Options.CustomRoleSpawnChances[CustomRoles.TimeThief]);
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