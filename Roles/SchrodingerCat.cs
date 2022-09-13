using System.Collections.Generic;
using UnityEngine;
using static TownOfHost.Options;

namespace TownOfHost
{
    public static class SchrodingerCat
    {
        private static readonly int Id = 50400;
        public static List<byte> playerIdList = new();

        public static CustomOption CanWinTheCrewmateBeforeChange;
        private static CustomOption ChangeTeamWhenExile;


        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.SchrodingerCat);
            CanWinTheCrewmateBeforeChange = CustomOption.Create(Id + 10, TabGroup.NeutralRoles, Color.white, "CanBeforeSchrodingerCatWinTheCrewmate", false, CustomRoleSpawnChances[CustomRoles.SchrodingerCat]);
            ChangeTeamWhenExile = CustomOption.Create(Id + 11, TabGroup.NeutralRoles, Color.white, "SchrodingerCatExiledTeamChanges", false, CustomRoleSpawnChances[CustomRoles.SchrodingerCat]);
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