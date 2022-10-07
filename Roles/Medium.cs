using System.Collections.Generic;
using UnityEngine;

namespace TownOfHost
{
    public static class Medium
    {
        private static readonly int Id = 20300;
        public static List<byte> playerIdList = new();
        public static CustomOption MediumCooldown;
        public static CustomOption MediumOneTimeUse;
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Medium);
            MediumCooldown = CustomOption.Create(Id + 10, TabGroup.CrewmateRoles, Color.white, "MediumCooldown", 30f, 5f, 120f, 5f, Options.CustomRoleSpawnChances[CustomRoles.Medium]);
            MediumOneTimeUse = CustomOption.Create(Id + 11, TabGroup.CrewmateRoles, Color.white, "MediumOneTimeUse", false, Options.CustomRoleSpawnChances[CustomRoles.Medium]);
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
    }
}