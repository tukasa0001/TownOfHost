using System.Collections.Generic;

namespace TownOfHost
{
    public static class FortuneTeller
    {
        private static readonly int Id = 21100;
        public static List<byte> playerIdList = new();

        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.FortuneTeller);
        }
        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static bool IsEnable => playerIdList.Count > 0;
    }
}