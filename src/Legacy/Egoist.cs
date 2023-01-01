using System.Collections.Generic;
using UnityEngine;

namespace TownOfHost
{
    public static class Egoist
    {
        private static readonly int Id = 50600;
        public static List<byte> playerIdList = new();

        private static OptionItem KillCooldown;
        public static OptionItem CanCreateMadmate;

        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Egoist);
            KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(2.5f, 180f, 2.5f), 20f, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Egoist])
                .SetValueFormat(OptionFormat.Seconds);
            CanCreateMadmate = BooleanOptionItem.Create(Id + 11, "CanCreateMadmate", false, TabGroup.NeutralRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Egoist]);
        }
        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte ego)
        {
            playerIdList.Add(ego);
            TeamEgoist.Add(ego);
        }
        public static bool IsEnable => playerIdList.Count > 0;
        public static void ApplyKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
        public static void OverrideCustomWinner()
        {
            foreach (var id in playerIdList)
                if (TeamEgoist.CompleteWinCondition(id))
                    CustomWinnerHolder.WinnerTeam = CustomWinner.Egoist;
        }
    }
}