using System.Collections.Generic;
using Hazel;
using UnityEngine;
using static TownOfHost.Options;

namespace TownOfHost
{
    public static class Deputy
    {
        private static readonly int Id = 21200;
        public static List<byte> playerIdList = new();

        private static readonly string[] SettingSelection =
        {
            "SheriffKillCooldown", "SheriffShotLimit"
        };
        private static CustomOption ChangeOption;
        private static CustomOption DecreaseKillCooldown;
        private static CustomOption IncreaseShotLimit;

        private static Dictionary<byte, byte> ParentSheriff = new();

        private static List<PlayerControl> SheriffList = new();
        public static void SetupCustomOption()
        {
            var spawnOption = CustomOption.Create(Id, Utils.GetRoleColor(CustomRoles.Deputy), CustomRoles.Deputy.ToString(), rates, rates[0], CustomRoleSpawnChances[CustomRoles.Sheriff])
                .HiddenOnDisplay(true)
                .SetGameMode(CustomGameMode.Standard);
            CustomRoleSpawnChances.Add(CustomRoles.Deputy, spawnOption);
            CustomRoleCounts.Add(CustomRoles.Deputy, CustomRoleCounts.GetValueOrDefault(CustomRoles.Sheriff));

            ChangeOption = CustomOption.Create(Id + 10, Color.white, "DeputyChangeOption", SettingSelection, SettingSelection[0], CustomRoleSpawnChances[CustomRoles.Deputy]);
            DecreaseKillCooldown = CustomOption.Create(Id + 11, Color.white, "DeputyDecreaseKillCooldown", 2f, 1f, 5f, 1f, CustomRoleSpawnChances[CustomRoles.Deputy]);
            IncreaseShotLimit = CustomOption.Create(Id + 12, Color.white, "DeputyIncreaseShotLimit", 1f, 1f, 2f, 1f, CustomRoleSpawnChances[CustomRoles.Deputy]);
        }
        public static void Init()
        {
            playerIdList = new();
            ParentSheriff = new();
            foreach (var pc in PlayerControl.AllPlayerControls)
                SheriffList.Add(pc);
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);

            SheriffList.RemoveAll(x => !x.Is(CustomRoles.Sheriff));
            var rand = new System.Random();

            var parent = SheriffList[rand.Next(0, SheriffList.Count)];
            SheriffList.Remove(parent);
            ParentSheriff.Add(playerId, parent.PlayerId);
        }
        public static bool IsEnable => playerIdList.Count > 0;
        public static void SendRPC()
        {
        }

        public static void ReceiveRPC(MessageReader reader)
        {
        }
    }
}