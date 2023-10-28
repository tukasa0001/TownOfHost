using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using UnityEngine;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Impostor;

namespace TownOfHost
{
    public enum MapName
    {
        Skeld,
        MiraHQ,
        Polus,
        AirShip,
        Fungle,
    }
    public enum SpawnPoint
    {
        Cafeteria,
        Weapons,
        O2,
        Navigation,
        Shields,
        Communications,
        Storage,
        Admin,
        Electrical,
        LowerEngine,
        UpperEngine,
        Security,
        Reactor,
        MedBay,
        Balcony,
        ThreeWay,
        LockerRoom,
        Decontamination,
        Laboratory,
        Launchpad,
        Office,
        Office1,
        Office2,
        Greenhouse,
        BoilerRoom,
        Dropship,
        Rocket,
        Toilet,
        SpecimenRoom,
        Brig,
        Engine,
        Kitchen,
        CargoBay,
        Records,
        MainHall,
        NapRoom,
        MeetingRoom,
        GapRoom,
        Vault,
        Cockpit,
        Armory,
        ViewingDeck,
        Medical,
        Showers,
        Coast,
        SplashZone,
        Bonfire,
        TheDorm,
        JungleTop,
        JungleBottom,
        LookOut,
        MiningPit,
        Plateau,
        Cliff,
    }
    class RandomSpawn
    {
        [HarmonyPatch(typeof(CustomNetworkTransform), nameof(CustomNetworkTransform.SnapTo), typeof(Vector2), typeof(ushort))]
        public class CustomNetworkTransformPatch
        {
            public static Dictionary<byte, int> NumOfTP = new();
            public static void Postfix(CustomNetworkTransform __instance, [HarmonyArgument(0)] Vector2 position)
            {
                if (!AmongUsClient.Instance.AmHost) return;
                if (position == new Vector2(-25f, 40f)) return; //最初の湧き地点ならreturn
                if (GameStates.IsInTask)
                {
                    var player = Main.AllPlayerControls.Where(p => p.NetTransform == __instance).FirstOrDefault();
                    if (player == null)
                    {
                        Logger.Warn("プレイヤーがnullです", "RandomSpawn");
                        return;
                    }
                    if (player.Is(CustomRoles.GM)) return; //GMは対象外に

                    NumOfTP[player.PlayerId]++;

                    if (NumOfTP[player.PlayerId] == 1)
                    {
                        if (Main.NormalOptions.MapId != 4) return; //マップがエアシップじゃなかったらreturn
                        if (player.Is(CustomRoles.Penguin))
                        {
                            var penguin = player.GetRoleClass() as Penguin;
                            penguin?.OnSpawnAirship();
                        }
                        player.RpcResetAbilityCooldown();
                        if (Options.FixFirstKillCooldown.GetBool() && !MeetingStates.MeetingCalled) player.SetKillCooldown(Main.AllPlayerKillCooldown[player.PlayerId]);
                        if (!IsRandomSpawn()) return; //ランダムスポーンが無効ならreturn
                        new AirshipSpawnMap().RandomTeleport(player);
                    }
                }
            }
        }

        public static bool IsRandomSpawn()
        {
            if (!Options.DisableRandomSpawn.GetBool()) return false;
            switch (Main.NormalOptions.MapId)
            {
                case 0:
                    return Options.RandomSpawnSkeld.GetBool();
                case 1:
                    return Options.RandomSpawnMiraHQ.GetBool();
                case 2:
                    return Options.RandomSpawnPolus.GetBool();
                case 4:
                    return Options.RandomSpawnAirShip.GetBool();
                case 5:
                    return Options.RandomSpawnFungle.GetBool();
                default:
                    Logger.Error("MapIdFiled", "IsRandomSpan");
                    return false;
            }
        }

        public static void TP(CustomNetworkTransform nt, Vector2 location)
        {
            if (AmongUsClient.Instance.AmHost) nt.SnapTo(location);
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(nt.NetId, (byte)RpcCalls.SnapTo, SendOption.None);
            NetHelpers.WriteVector2(location, writer);
            writer.Write(nt.lastSequenceId);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public static void SetupCustomOption()
        {
            // Skeld
            Options.RandomSpawnSkeld = BooleanOptionItem.Create(101310, MapName.Skeld, false, TabGroup.MainSettings, false).SetParent(Options.DisableRandomSpawn).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldCafeteria = BooleanOptionItem.Create(101311, SpawnPoint.Cafeteria, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldWeapons = BooleanOptionItem.Create(101312, SpawnPoint.Weapons, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldO2 = BooleanOptionItem.Create(101313, SpawnPoint.O2, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldNavigation = BooleanOptionItem.Create(101314, SpawnPoint.Navigation, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldShields = BooleanOptionItem.Create(101315, SpawnPoint.Shields, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldCommunications = BooleanOptionItem.Create(101316, SpawnPoint.Communications, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldStorage = BooleanOptionItem.Create(101317, SpawnPoint.Storage, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldAdmin = BooleanOptionItem.Create(101318, SpawnPoint.Admin, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldElectrical = BooleanOptionItem.Create(101319, SpawnPoint.Electrical, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldLowerEngine = BooleanOptionItem.Create(101320, SpawnPoint.LowerEngine, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldUpperEngine = BooleanOptionItem.Create(101321, SpawnPoint.UpperEngine, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldSecurity = BooleanOptionItem.Create(101322, SpawnPoint.Security, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldReactor = BooleanOptionItem.Create(101323, SpawnPoint.Reactor, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldMedBay = BooleanOptionItem.Create(101324, SpawnPoint.MedBay, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            // Mira
            Options.RandomSpawnMiraHQ = BooleanOptionItem.Create(101341, MapName.MiraHQ, false, TabGroup.MainSettings, false).SetParent(Options.DisableRandomSpawn).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraHQCafeteria = BooleanOptionItem.Create(101342, SpawnPoint.Cafeteria, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMiraHQ).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraHQBalcony = BooleanOptionItem.Create(101343, SpawnPoint.Balcony, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMiraHQ).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraHQStorage = BooleanOptionItem.Create(101344, SpawnPoint.Storage, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMiraHQ).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraHQThreeWay = BooleanOptionItem.Create(101345, SpawnPoint.ThreeWay, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMiraHQ).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraHQCommunications = BooleanOptionItem.Create(101346, SpawnPoint.Communications, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMiraHQ).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraHQMedBay = BooleanOptionItem.Create(101347, SpawnPoint.MedBay, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMiraHQ).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraHQLockerRoom = BooleanOptionItem.Create(101348, SpawnPoint.LockerRoom, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMiraHQ).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraHQDecontamination = BooleanOptionItem.Create(101349, SpawnPoint.Decontamination, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMiraHQ).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraHQLaboratory = BooleanOptionItem.Create(101350, SpawnPoint.Laboratory, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMiraHQ).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraHQReactor = BooleanOptionItem.Create(101351, SpawnPoint.Reactor, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMiraHQ).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraHQLaunchpad = BooleanOptionItem.Create(101352, SpawnPoint.Launchpad, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMiraHQ).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraHQAdmin = BooleanOptionItem.Create(101353, SpawnPoint.Admin, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMiraHQ).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraHQOffice = BooleanOptionItem.Create(101354, SpawnPoint.Office, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMiraHQ).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraHQGreenhouse = BooleanOptionItem.Create(101355, SpawnPoint.Greenhouse, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMiraHQ).SetGameMode(CustomGameMode.All);
            // Polus
            Options.RandomSpawnPolus = BooleanOptionItem.Create(101371, MapName.Polus, false, TabGroup.MainSettings, false).SetParent(Options.DisableRandomSpawn).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusOffice1 = BooleanOptionItem.Create(101372, SpawnPoint.Office1, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusOffice2 = BooleanOptionItem.Create(101373, SpawnPoint.Office2, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusAdmin = BooleanOptionItem.Create(101374, SpawnPoint.Admin, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusCommunications = BooleanOptionItem.Create(101375, SpawnPoint.Communications, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusWeapons = BooleanOptionItem.Create(101376, SpawnPoint.Weapons, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusBoilerRoom = BooleanOptionItem.Create(101377, SpawnPoint.BoilerRoom, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusO2 = BooleanOptionItem.Create(101378, SpawnPoint.O2, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusElectrical = BooleanOptionItem.Create(101379, SpawnPoint.Electrical, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusSecurity = BooleanOptionItem.Create(101380, SpawnPoint.Security, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusDropship = BooleanOptionItem.Create(101381, SpawnPoint.Dropship, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusStorage = BooleanOptionItem.Create(101382, SpawnPoint.Storage, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusRocket = BooleanOptionItem.Create(101383, SpawnPoint.Rocket, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusLaboratory = BooleanOptionItem.Create(101384, SpawnPoint.Laboratory, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusToilet = BooleanOptionItem.Create(101385, SpawnPoint.Toilet, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusSpecimenRoom = BooleanOptionItem.Create(101386, SpawnPoint.SpecimenRoom, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            // AirShip
            Options.RandomSpawnAirShip = BooleanOptionItem.Create(101401, MapName.AirShip, false, TabGroup.MainSettings, false).SetParent(Options.DisableRandomSpawn).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirShipBrig = BooleanOptionItem.Create(101402, SpawnPoint.Brig, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirShip).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirShipEngine = BooleanOptionItem.Create(101403, SpawnPoint.Engine, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirShip).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirShipKitchen = BooleanOptionItem.Create(101404, SpawnPoint.Kitchen, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirShip).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirShipCargoBay = BooleanOptionItem.Create(101405, SpawnPoint.CargoBay, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirShip).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirShipRecords = BooleanOptionItem.Create(101406, SpawnPoint.Records, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirShip).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirShipMainHall = BooleanOptionItem.Create(101407, SpawnPoint.MainHall, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirShip).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirShipNapRoom = BooleanOptionItem.Create(101408, SpawnPoint.NapRoom, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirShip).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirShipMeetingRoom = BooleanOptionItem.Create(101409, SpawnPoint.MeetingRoom, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirShip).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirShipGapRoom = BooleanOptionItem.Create(101410, SpawnPoint.GapRoom, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirShip).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirShipVault = BooleanOptionItem.Create(101411, SpawnPoint.Vault, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirShip).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirShipCommunications = BooleanOptionItem.Create(101412, SpawnPoint.Communications, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirShip).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirShipCockpit = BooleanOptionItem.Create(101413, SpawnPoint.Cockpit, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirShip).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirShipArmory = BooleanOptionItem.Create(101414, SpawnPoint.Armory, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirShip).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirShipViewingDeck = BooleanOptionItem.Create(101415, SpawnPoint.ViewingDeck, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirShip).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirShipSecurity = BooleanOptionItem.Create(101416, SpawnPoint.Security, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirShip).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirShipElectrical = BooleanOptionItem.Create(101417, SpawnPoint.Electrical, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirShip).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirShipMedical = BooleanOptionItem.Create(101418, SpawnPoint.Medical, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirShip).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirShipToilet = BooleanOptionItem.Create(101419, SpawnPoint.Toilet, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirShip).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirShipShowers = BooleanOptionItem.Create(101420, SpawnPoint.Showers, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirShip).SetGameMode(CustomGameMode.All);
            // Fungle
            Options.RandomSpawnFungle = BooleanOptionItem.Create(101431, MapName.Fungle, false, TabGroup.MainSettings, false).SetParent(Options.DisableRandomSpawn).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleKitchen = BooleanOptionItem.Create(101432, SpawnPoint.Kitchen, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleCoast = BooleanOptionItem.Create(101433, SpawnPoint.Coast, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleCafeteria = BooleanOptionItem.Create(101434, SpawnPoint.Cafeteria, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleSplashZone = BooleanOptionItem.Create(101435, SpawnPoint.SplashZone, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleBonfire = BooleanOptionItem.Create(101436, SpawnPoint.Bonfire, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleDropship = BooleanOptionItem.Create(101437, SpawnPoint.Dropship, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleStorage = BooleanOptionItem.Create(101438, SpawnPoint.Storage, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleMeetingRoom = BooleanOptionItem.Create(101438, SpawnPoint.MeetingRoom, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleTheDorm = BooleanOptionItem.Create(101439, SpawnPoint.TheDorm, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleLaboratory = BooleanOptionItem.Create(101440, SpawnPoint.Laboratory, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleGreenhouse = BooleanOptionItem.Create(101441, SpawnPoint.Greenhouse, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleReactor = BooleanOptionItem.Create(101442, SpawnPoint.Reactor, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleJungleTop = BooleanOptionItem.Create(101443, SpawnPoint.JungleTop, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleJungleBottom = BooleanOptionItem.Create(101444, SpawnPoint.JungleBottom, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleLookOut = BooleanOptionItem.Create(101445, SpawnPoint.LookOut, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleMiningPit = BooleanOptionItem.Create(101446, SpawnPoint.MiningPit, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFunglePlateau = BooleanOptionItem.Create(101447, SpawnPoint.Plateau, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleUpperEngine = BooleanOptionItem.Create(101448, SpawnPoint.UpperEngine, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleCliff = BooleanOptionItem.Create(101449, SpawnPoint.Cliff, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleCommunications = BooleanOptionItem.Create(101450, SpawnPoint.Communications, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
        }

        public abstract class SpawnMap
        {
            public virtual void RandomTeleport(PlayerControl player)
            {
                var location = GetLocation();
                Logger.Info($"{player.Data.PlayerName}:{location}", "RandomSpawn");
                TP(player.NetTransform, location);
            }
            public abstract Vector2 GetLocation();
        }

        public class SkeldSpawnMap : SpawnMap
        {
            public Dictionary<string, Vector2> positions = new()
            {
                ["Cafeteria"] = new(-1.0f, 3.0f),
                ["Weapons"] = new(9.3f, 1.0f),
                ["O2"] = new(6.5f, -3.8f),
                ["Navigation"] = new(16.5f, -4.8f),
                ["Shields"] = new(9.3f, -12.3f),
                ["Communications"] = new(4.0f, -15.5f),
                ["Storage"] = new(-1.5f, -15.5f),
                ["Admin"] = new(4.5f, -7.9f),
                ["Electrical"] = new(-7.5f, -8.8f),
                ["LowerEngine"] = new(-17.0f, -13.5f),
                ["UpperEngine"] = new(-17.0f, -1.3f),
                ["Security"] = new(-13.5f, -5.5f),
                ["Reactor"] = new(-20.5f, -5.5f),
                ["MedBay"] = new(-9.0f, -4.0f)
            };
            public override Vector2 GetLocation()
            {
                return positions.ToArray().OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value;
            }
        }
        public class MiraHQSpawnMap : SpawnMap
        {
            public Dictionary<string, Vector2> positions = new()
            {
                ["Cafeteria"] = new(25.5f, 2.0f),
                ["Balcony"] = new(24.0f, -2.0f),
                ["Storage"] = new(19.5f, 4.0f),
                ["ThreeWay"] = new(17.8f, 11.5f),
                ["Communications"] = new(15.3f, 3.8f),
                ["MedBay"] = new(15.5f, -0.5f),
                ["LockerRoom"] = new(9.0f, 1.0f),
                ["Decontamination"] = new(6.1f, 6.0f),
                ["Laboratory"] = new(9.5f, 12.0f),
                ["Reactor"] = new(2.5f, 10.5f),
                ["Launchpad"] = new(-4.5f, 2.0f),
                ["Admin"] = new(21.0f, 17.5f),
                ["Office"] = new(15.0f, 19.0f),
                ["Greenhouse"] = new(17.8f, 23.0f)
            };
            public override Vector2 GetLocation()
            {
                return positions.ToArray().OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value;
            }
        }
        public class PolusSpawnMap : SpawnMap
        {
            public Dictionary<string, Vector2> positions = new()
            {
                ["Office1"] = new(19.5f, -18.0f),
                ["Office2"] = new(26.0f, -17.0f),
                ["Admin"] = new(24.0f, -22.5f),
                ["Communications"] = new(12.5f, -16.0f),
                ["Weapons"] = new(12.0f, -23.5f),
                ["BoilerRoom"] = new(2.3f, -24.0f),
                ["O2"] = new(2.0f, -17.5f),
                ["Electrical"] = new(9.5f, -12.5f),
                ["Security"] = new(3.0f, -12.0f),
                ["Dropship"] = new(16.7f, -3.0f),
                ["Storage"] = new(20.5f, -12.0f),
                ["Rocket"] = new(26.7f, -8.5f),
                ["Laboratory"] = new(36.5f, -7.5f),
                ["Toilet"] = new(34.0f, -10.0f),
                ["SpecimenRoom"] = new(36.5f, -22.0f)
            };
            public override Vector2 GetLocation()
            {
                return positions.ToArray().OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value;
            }
        }
        public class AirshipSpawnMap : SpawnMap
        {
            public Dictionary<string, Vector2> positions = new()
            {
                ["Brig"] = new(-0.7f, 8.5f),
                ["Engine"] = new(-0.7f, -1.0f),
                ["Kitchen"] = new(-7.0f, -11.5f),
                ["CargoBay"] = new(33.5f, -1.5f),
                ["Records"] = new(20.0f, 10.5f),
                ["MainHall"] = new(15.5f, 0.0f),
                ["NapRoom"] = new(6.3f, 2.5f),
                ["MeetingRoom"] = new(17.1f, 14.9f),
                ["GapRoom"] = new(12.0f, 8.5f),
                ["Vault"] = new(-8.9f, 12.2f),
                ["Communications"] = new(-13.3f, 1.3f),
                ["Cockpit"] = new(-23.5f, -1.6f),
                ["Armory"] = new(-10.3f, -5.9f),
                ["ViewingDeck"] = new(-13.7f, -12.6f),
                ["Security"] = new(5.8f, -10.8f),
                ["Electrical"] = new(16.3f, -8.8f),
                ["Medical"] = new(29.0f, -6.2f),
                ["Toilet"] = new(30.9f, 6.8f),
                ["Showers"] = new(21.2f, -0.8f)
            };
            public override Vector2 GetLocation()
            {
        public class FungleSpawnMap : SpawnMap
        {
            public Dictionary<OptionItem, Vector2> positions = new()
            {
                [Options.RandomSpawnFungleKitchen] = new(-17.8f, -7.3f),
                [Options.RandomSpawnFungleCoast] = new(-21.3f, 3.0f),   //海岸
                [Options.RandomSpawnFungleCafeteria] = new(-16.9f, 5.5f),
                [Options.RandomSpawnFungleSplashZone] = new(-17.7f, 0.0f),
                [Options.RandomSpawnFungleBonfire] = new(-9.7f, 2.7f),  //焚き火
                [Options.RandomSpawnFungleDropship] = new(-7.6f, 10.4f),
                [Options.RandomSpawnFungleStorage] = new(2.3f, 4.3f),
                [Options.RandomSpawnFungleMeetingRoom] = new(-4.2f, -2.2f),
                [Options.RandomSpawnFungleTheDorm] = new(1.7f, -1.4f),  //宿舎
                [Options.RandomSpawnFungleLaboratory] = new(-4.2f, -7.9f),
                [Options.RandomSpawnFungleGreenhouse] = new(9.2f, -11.8f),
                [Options.RandomSpawnFungleReactor] = new(21.8f, -7.2f),
                [Options.RandomSpawnFungleJungleTop] = new(4.2f, -5.3f),
                [Options.RandomSpawnFungleJungleBottom] = new(15.9f, -14.8f),
                [Options.RandomSpawnFungleLookOut] = new(6.4f, 3.1f),
                [Options.RandomSpawnFungleMiningPit] = new(12.5f, 9.6f),
                [Options.RandomSpawnFunglePlateau] = new(15.5f, 3.9f),    //展望台右の高原
                [Options.RandomSpawnFungleUpperEngine] = new(21.9f, 3.2f),
                [Options.RandomSpawnFungleCliff] = new(19.8f, 7.3f),   //通信室下の崖
                [Options.RandomSpawnFungleCommunications] = new(20.9f, 13.4f),
            };

            public override Vector2 GetLocation()
            {
                if (positions.ToArray().Where(o => o.Key.GetBool()).Count() > 0) return positions.ToArray().Where(o => o.Key.GetBool()).OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value;
                return positions.ToArray().OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault().Value;
            }
        }
    }
}