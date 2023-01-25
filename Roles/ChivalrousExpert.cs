using System.Collections.Generic;

namespace TownOfHost
{
    public static class ChivalrousExpert
    {
        private static readonly int Id = 114514;
        public static List<byte> playerIdList = new();
        public static Dictionary<byte, float> CurrentKillCooldown = new();
        public static bool isKilled = false;

        public static void SetupCustomOption() {
            Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.ChivalrousExpert);
        }

        public static void Init()
        {
            playerIdList = new();
            CurrentKillCooldown = new();
            isKilled = false;
        }
        public static void Add(byte playerId) {
            playerIdList.Add(playerId);
            CurrentKillCooldown.Add(playerId, 0);

            if (!Main.ResetCamPlayerList.Contains(playerId)) {
                Main.ResetCamPlayerList.Add(playerId);
            }

            // Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()} : 残り{ShotLimit[playerId]}発", "Sheriff");
            // Logger.Info(Utils.GetPlayerById(playerId)?.GetNameWithRole() + " is ChivalrousExpert");
        }
    }
}
