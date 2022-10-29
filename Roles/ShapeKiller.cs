using System.Collections.Generic;

namespace TownOfHost
{
    public static class ShapeKiller
    {
        private static readonly int Id = 3000;
        public static List<byte> playerIdList = new();

        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.ShapeKiller);
        }
    }
}