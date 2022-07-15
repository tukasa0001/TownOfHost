using System.Collections.Generic;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public static class Egoist
    {
        private static readonly int Id = 50600;
        public static List<byte> playerIdList = new();

        private static CustomOption KillCooldown;

        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, CustomRoles.SerialKiller);
            KillCooldown = CustomOption.Create(Id + 10, Color.white, "EgoistKillCooldown", 20f, 2.5f, 180f, 2.5f, Options.CustomRoleSpawnChances[CustomRoles.Egoist]);
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
        public static bool IsEnable()
        {
            return playerIdList.Count > 0;
        }
        public static void ApplyKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
        public static void OverrideCustomWinner(bool noLivingImposter)
        {
            foreach (var id in playerIdList)
            {
                var egoist = Utils.GetPlayerById(id);
                if (egoist == null) return;
                if (Main.currentWinner == CustomWinner.Impostor && egoist.Is(CustomRoles.Egoist) && !PlayerState.isDead[id] && noLivingImposter)
                    Main.currentWinner = CustomWinner.Egoist;
            }
        }
    }
}