using System.Collections.Generic;
using Hazel;
using UnityEngine;

namespace TownOfHost
{
    public static class Cracker
    {
        static readonly int Id = 3000;
        public static List<byte> playerIdList = new();
        public static CustomOption PoweredLightsOut;
        public static CustomOption LightsOutMinimum;
        public static CustomOption PoweredComms;
        public static CustomOption PoweredReactor;
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, CustomRoles.Cracker);
            PoweredLightsOut = CustomOption.Create(Id + 10, Color.white, "PoweredLightsOut", true, Options.CustomRoleSpawnChances[CustomRoles.Cracker]);
            LightsOutMinimum = CustomOption.Create(Id + 11, Color.white, "LightsOutMinimum", 5, 0, 20, 1, Options.CustomRoleSpawnChances[CustomRoles.Cracker]);
            PoweredComms = CustomOption.Create(Id + 12, Color.white, "PoweredComms", true, Options.CustomRoleSpawnChances[CustomRoles.Cracker]);
            PoweredReactor = CustomOption.Create(Id + 13, Color.white, "PoweredReactor", true, Options.CustomRoleSpawnChances[CustomRoles.Cracker]);
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
        public static void PoweredSabotage(SystemTypes systemType, PlayerControl player)
        {
            Logger.Info("Powered Sabotage", "Cracker");
            bool HasImpVision = player.GetCustomRole().IsImpostor()
                            || (player.GetCustomRole().IsMadmate() && Options.MadmateHasImpostorVision.GetBool())
                            || player.Is(CustomRoles.EgoSchrodingerCat)
                            || (player.Is(CustomRoles.Lighter) && player.GetPlayerTaskState().IsTaskFinished
                            && Options.LighterTaskCompletedDisableLightOut.GetBool());
            int mapId = PlayerControl.GameOptions.MapId;

            switch (systemType)
            {
                case SystemTypes.Electrical:
                    if (!PoweredLightsOut.GetBool() && LightsOutMinimum.GetFloat() == 0) break;
                    Logger.Info("Powered Lights Out", "Cracker");
                    break;
                case SystemTypes.Comms:
                    if (!PoweredComms.GetBool()) break;
                    Logger.Info("Powered Comms", "Cracker");
                    break;
                case SystemTypes.Reactor:
                case SystemTypes.Laboratory:
                    if (!(systemType == SystemTypes.Laboratory && mapId == 2)
                        && !(systemType == SystemTypes.Reactor && mapId == 4)) break;
                    if (!PoweredReactor.GetBool()) break;
                    Logger.Info("Powered Reactor", "Cracker");
                    CheckAndCloseAllDoors(mapId);
                    break;
                default:
                    break;
            }
        }
        private static void CheckAndCloseAllDoors(int mapId)
        {
            if (mapId == 3) return;
            SystemTypes[] SkeldDoorRooms =
            {SystemTypes.Cafeteria,
            SystemTypes.Electrical,
            SystemTypes.LowerEngine,
            SystemTypes.MedBay,
            SystemTypes.Security,
            SystemTypes.Storage,
            SystemTypes.UpperEngine};

            SystemTypes[] PolusDoorRooms =
            {SystemTypes.Comms,
            SystemTypes.Electrical,
            SystemTypes.Laboratory,
            SystemTypes.LifeSupp,
            SystemTypes.Office,
            SystemTypes.Storage,
            SystemTypes.Weapons};

            SystemTypes[] AirShipDoorRooms =
            {SystemTypes.Brig,
            SystemTypes.Comms,
            SystemTypes.Kitchen,
            SystemTypes.MainHall,
            SystemTypes.Medical,
            SystemTypes.Records};

            SystemTypes[][] Doors = { SkeldDoorRooms, PolusDoorRooms, null, AirShipDoorRooms };
            foreach (var doorRoom in Doors[mapId - 1])
            {
                ShipStatus.Instance.CloseDoorsOfType(doorRoom);
            }
        }
    }
}