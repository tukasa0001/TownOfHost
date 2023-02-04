using System;
using System.Collections.Generic;
using System.Linq;

namespace TownOfHost
{
    public static class ChivalrousExpert
    {
        private static readonly int Id = 8021075;
        public static List<byte> playerIdList = new();
        public static Dictionary<byte, float> CurrentKillCooldown = new();
        //public static bool isKilled = false;
        public static List<byte> killed = new();

        public static void SetupCustomOption() {
            Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.ChivalrousExpert);
        }

        public static void Init()
        {
            playerIdList = new();
            CurrentKillCooldown = new();
            // isKilled = false;
        }

        public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CanUseKillButton(id) ? CurrentKillCooldown[id] : 255f;

        public static bool CanUseKillButton(byte playerId)
            => !Main.PlayerStates[playerId].IsDead
            && !isKilled(playerId);

        public static bool isKilled(byte playerId) {
            //return killed.Contains(playerId);
            return killed.Contains(playerId);
        }

        public static void Add(byte playerId) {
            playerIdList.Add(playerId);
            CurrentKillCooldown.Add(playerId, 1);

            if (!Main.ResetCamPlayerList.Contains(playerId)) {
                Main.ResetCamPlayerList.Add(playerId);
            }
        }
    }
}
