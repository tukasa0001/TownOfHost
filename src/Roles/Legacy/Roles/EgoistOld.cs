/*using System.Collections.Generic;
using UnityEngine;
using AmongUs.Data;
using TownOfHost.Extensions;
using TownOfHost.Roles;

namespace TownOfHost
{
    public static class EgoistOld
    {
        private static readonly int Id = 50600;
        public static List<byte> playerIdList = new();

        private static CustomOption KillCooldown;
        public static CustomOption ImpostorsKnowEgo;

        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, CustomRoles.Egoist, AmongUsExtensions.OptionType.Neutral);
            /*KillCooldown = CustomOption.Create(Id + 10, Color.white, "EgoistKillCooldown", AmongUsExtensions.OptionType.Neutral, 20f, 2.5f, 180f, 2.5f, Options.CustomRoleSpawnChances[CustomRoles.Egoist]);
            ImpostorsKnowEgo = CustomOption.Create(Id + 11, Color.white, "ImpostorsKnowEgo", AmongUsExtensions.OptionType.Neutral, false, Options.CustomRoleSpawnChances[CustomRoles.Egoist]);#1#
        }
        public static void Init()
        {
            playerIdList = new();
            TeamEgoist.EgoistWin = false;
            TeamEgoist.playerIdList = new();
        }
        public static void Add(byte ego)
        {
            playerIdList.Add(ego);
            TeamEgoist.Add(ego);
        }
        public static bool IsEnable => playerIdList.Count > 0;
        public static void ApplyKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
        public static void OverrideCustomWinner(int deathAmount)
        {
            int allimpdead = 0;
            foreach (var p in PlayerControl.AllPlayerControls)
            {
                if (p.GetCustomRole().IsImpostor() && PlayerStateOLD.isDead[p.PlayerId] | p.Data.IsDead)
                    allimpdead++;
            }
            foreach (var id in playerIdList)
                if (TeamEgoist.CompleteWinCondition(id) | deathAmount == allimpdead)
                {
                    Main.currentWinner = CustomWinner.Egoist;
                    TeamEgoist.EgoistWin = true;
                }
        }
    }
}*/