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
        public static void ChangeTeam(PlayerControl player)
        {
            if (!ChangeTeamWhenExile.GetBool()) return;

            var rand = new System.Random();
            List<CustomRoles> Rand = new()
            {
                CustomRoles.CSchrodingerCat,
                CustomRoles.MSchrodingerCat
            };
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc.Is(CustomRoles.Egoist) && !pc.Data.IsDead && Rand.Contains(CustomRoles.EgoSchrodingerCat))
                    Rand.Add(CustomRoles.EgoSchrodingerCat);

                if (CustomRoles.Jackal.IsEnable() && pc.Is(CustomRoles.Jackal) && !pc.Data.IsDead && Rand.Contains(CustomRoles.JSchrodingerCat))
                    Rand.Add(CustomRoles.JSchrodingerCat);
            }
            var Role = Rand[rand.Next(Rand.Count)];
            player.RpcSetCustomRole(Role);
        }
    }
}