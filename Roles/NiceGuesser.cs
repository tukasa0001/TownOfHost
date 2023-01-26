using System.Collections.Generic;

namespace TownOfHost
{
    public static class NiceGuesser
    {
        private static readonly int Id = 1919810;
        public static List<byte> playerIdList = new();

        public static void Init() {
            playerIdList = new();
        }

        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }

        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.NiceGuesser);
        }
    }
}
