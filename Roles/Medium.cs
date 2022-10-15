using Hazel;
using System.Collections.Generic;
using UnityEngine;
using static TownOfHost.Translator;
using System.Timers;

namespace TownOfHost
{
    public static class Medium
    {
        private static readonly int Id = 5000000;
        public static List<byte> playerIdList = new();
        public static CustomOption MediumCooldown;
        public static CustomOption MediumOneTimeUse;
        public static Dictionary<byte, float> Cooldown = new();
        public static Dictionary<byte, bool> MediumUsed = new();
        public static Dictionary<byte, bool> CanMedium = new();
        public static Dictionary<byte, int> DeadTimer = new();
        public static Dictionary<byte, byte> Killer = new();
        public static List<byte> Target = new();
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Medium);
            MediumCooldown = CustomOption.Create(Id + 10, TabGroup.CrewmateRoles, Color.white, "MediumCooldown", 30f, 5f, 120f, 5f, Options.CustomRoleSpawnChances[CustomRoles.Medium]);
            MediumOneTimeUse = CustomOption.Create(Id + 11, TabGroup.CrewmateRoles, Color.white, "MediumOneTimeUse", false, Options.CustomRoleSpawnChances[CustomRoles.Medium]);
        }
        public static void Init()
        {
            playerIdList = new();
            Cooldown = new();
            MediumUsed = new();
            CanMedium = new();
            DeadTimer = new();
            Killer = new();
            Target = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            MediumUsed.Add(playerId, false);
            CanMedium.Add(playerId, false);
        }
        public static bool IsEnable() => playerIdList.Count > 0;
        public static void ApplyGameOptions(GameOptionsData opt, byte playerId)
        {
            opt.RoleOptions.ScientistCooldown = MediumCooldown.GetFloat();
            opt.RoleOptions.ScientistBatteryCharge = 0.1f;
        }
        public static void FixedUpdate(PlayerControl target)
        {
            if (GameStates.IsInTask && Target.Contains(target.PlayerId))
            {
                DeadTimer[target.PlayerId]++;
            }
        }
        public static PlayerControl GetKiller(byte targetId)
        {
            var target = Utils.GetPlayerById(targetId);
            if (target == null) return null;
            Killer.TryGetValue(targetId, out var killerId);
            var killer = Utils.GetPlayerById(killerId);
            return killer;
        }

    }
}