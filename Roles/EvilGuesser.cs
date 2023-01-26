using System.Collections.Generic;

namespace TownOfHost
{
    public static class EvilGuesser
    {
        private static readonly int Id = 114514;
        public static List<byte> playerIdList = new();

        public static void Init()
        {
            playerIdList = new();
        }

        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }

        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.EvilGuesser);
        }
    }
}
