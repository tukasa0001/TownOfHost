using System.Collections.Generic;
using UnityEngine;

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
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            MediumUsed.Add(playerId, false);
            CanMedium.Add(playerId, true);
        }
        public static bool IsEnable() => playerIdList.Count > 0;
        public static void OnReportDeadBody()
        {
            foreach (var medium in PlayerControl.AllPlayerControls)
            {
                var rand = new System.Random();
                int Mode = rand.Next(5);
                if (!(medium.Is(CustomRoles.Medium) && medium.IsAlive())) continue;
                foreach (var target in PlayerControl.AllPlayerControls)
                {
                    string TargetDeathReason = "";
                    if (medium.Is(CustomRoles.Medium) && target.Data.IsDead)
                        TargetDeathReason = Utils.GetVitalText(target.PlayerId);
                    string TargetPlayerName = target.GetRealName(false);
                    string TargetName = $"{TargetPlayerName}";
                    Utils.SendMessage($"{TargetName}の死因は{TargetDeathReason}です。", medium.PlayerId);
                    break;
                }
            }
        }
    }
}