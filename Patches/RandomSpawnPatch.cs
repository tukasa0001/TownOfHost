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
    public enum SpawnPoint
    {
        Cafeteria,
        Weapons,
        LifeSupp,
        Nav,
        Shields,
        Comms,
        Storage,
        Admin,
        Electrical,
        LowerEngine,
        UpperEngine,
        Security,
        Reactor,
        MedBay,
        Balcony,
        Junction,//StringNamesにない文言 string.csvに追加
        LockerRoom,
        Decontamination,
        Laboratory,
        Launchpad,
        Office,
        OfficeLeft,//StringNamesにない文言 string.csvに追加
        OfficeRight,//StringNamesにない文言 string.csvに追加
        Greenhouse,
        BoilerRoom,
        Dropship,
        Rocket,//StringNamesにない文言 string.csvに追加
        Toilet,//StringNamesにない文言 string.csvに追加
        Specimens,
        Brig,
        Engine,
        Kitchen,
        CargoBay,
        Records,
        MainHall,
        NapRoom,//StringNamesにない文言 string.csvに追加 AirShipメインホール左上の仮眠室
        MeetingRoom,
        GapRoom,
        VaultRoom,
        Cockpit,
        Armory,
        ViewingDeck,
        Medical,
        Showers,
        Beach,
        RecRoom,//SplashZoneのこと
        Bonfire,//StringNamesにない文言 string.csvに追加 Fungleの焚き火
        SleepingQuarters,//TheDorm 宿舎のこと
        JungleTop,//StringNamesにない文言 string.csvに追加
        JungleBottom,//StringNamesにない文言 string.csvに追加
        Lookout,
        MiningPit,
        Highlands,//Fungleの高地
        Precipice,//StringNamesにない文言 string.csvに追加
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
            if (!Options.EnableRandomSpawn.GetBool()) return false;
            switch (Main.NormalOptions.MapId)
            {
                case 0:
                    return Options.RandomSpawnSkeld.GetBool();
                case 1:
                    return Options.RandomSpawnMira.GetBool();
                case 2:
                    return Options.RandomSpawnPolus.GetBool();
                case 4:
                    return Options.RandomSpawnAirship.GetBool();
                case 5:
                    return Options.RandomSpawnFungle.GetBool();
                default:
                    Logger.Error($"MapIdFailed ID:{Main.NormalOptions.MapId}", "IsRandomSpawn");
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
            Options.RandomSpawnSkeld = BooleanOptionItem.Create(101310, StringNames.MapNameSkeld, false, TabGroup.MainSettings, false).SetParent(Options.EnableRandomSpawn).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldCafeteria = BooleanOptionItem.Create(101311, StringNames.Cafeteria, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldWeapons = BooleanOptionItem.Create(101312, StringNames.Weapons, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldShields = BooleanOptionItem.Create(101313, StringNames.Shields, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldStorage = BooleanOptionItem.Create(101314, StringNames.Storage, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldLowerEngine = BooleanOptionItem.Create(101315, StringNames.LowerEngine, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldUpperEngine = BooleanOptionItem.Create(101316, StringNames.UpperEngine, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldLifeSupp = BooleanOptionItem.Create(101317, StringNames.LifeSupp, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldNav = BooleanOptionItem.Create(101318, StringNames.Nav, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldComms = BooleanOptionItem.Create(101319, StringNames.Comms, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldAdmin = BooleanOptionItem.Create(101320, StringNames.Admin, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldElectrical = BooleanOptionItem.Create(101321, StringNames.Electrical, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldSecurity = BooleanOptionItem.Create(101322, StringNames.Security, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldReactor = BooleanOptionItem.Create(101323, StringNames.Reactor, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldMedBay = BooleanOptionItem.Create(101324, StringNames.MedBay, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            // Mira
            Options.RandomSpawnMira = BooleanOptionItem.Create(101341, StringNames.MapNameMira, false, TabGroup.MainSettings, false).SetParent(Options.EnableRandomSpawn).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraCafeteria = BooleanOptionItem.Create(101342, StringNames.Cafeteria, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraComms = BooleanOptionItem.Create(101343, StringNames.Comms, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraDecontamination = BooleanOptionItem.Create(101344, StringNames.Decontamination, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraReactor = BooleanOptionItem.Create(101345, StringNames.Reactor, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraLaunchpad = BooleanOptionItem.Create(101346, StringNames.Launchpad, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraAdmin = BooleanOptionItem.Create(101347, StringNames.Admin, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraBalcony = BooleanOptionItem.Create(101348, StringNames.Balcony, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraStorage = BooleanOptionItem.Create(101349, StringNames.Storage, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraJunction = BooleanOptionItem.Create(101350, SpawnPoint.Junction, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraMedBay = BooleanOptionItem.Create(101351, StringNames.MedBay, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraLockerRoom = BooleanOptionItem.Create(101352, StringNames.LockerRoom, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraLaboratory = BooleanOptionItem.Create(101353, StringNames.Laboratory, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraOffice = BooleanOptionItem.Create(101354, StringNames.Office, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraGreenhouse = BooleanOptionItem.Create(101355, StringNames.Greenhouse, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetGameMode(CustomGameMode.All);
            // Polus
            Options.RandomSpawnPolus = BooleanOptionItem.Create(101371, StringNames.MapNamePolus, false, TabGroup.MainSettings, false).SetParent(Options.EnableRandomSpawn).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusOfficeLeft = BooleanOptionItem.Create(101372, SpawnPoint.OfficeLeft, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusBoilerRoom = BooleanOptionItem.Create(101373, StringNames.BoilerRoom, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusSecurity = BooleanOptionItem.Create(101374, StringNames.Security, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusDropship = BooleanOptionItem.Create(101375, StringNames.Dropship, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusLaboratory = BooleanOptionItem.Create(101376, StringNames.Laboratory, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusSpecimens = BooleanOptionItem.Create(101377, StringNames.Specimens, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusOfficeRight = BooleanOptionItem.Create(101378, SpawnPoint.OfficeRight, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusAdmin = BooleanOptionItem.Create(101379, StringNames.Admin, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusComms = BooleanOptionItem.Create(1013780, StringNames.Comms, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusWeapons = BooleanOptionItem.Create(101381, StringNames.Weapons, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusLifeSupp = BooleanOptionItem.Create(101382, StringNames.LifeSupp, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusElectrical = BooleanOptionItem.Create(101383, StringNames.Electrical, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusStorage = BooleanOptionItem.Create(101384, StringNames.Storage, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusRocket = BooleanOptionItem.Create(101385, SpawnPoint.Rocket, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusToilet = BooleanOptionItem.Create(101386, SpawnPoint.Toilet, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            // Airship
            Options.RandomSpawnAirship = BooleanOptionItem.Create(101401, StringNames.MapNameAirship, false, TabGroup.MainSettings, false).SetParent(Options.EnableRandomSpawn).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipBrig = BooleanOptionItem.Create(101402, StringNames.Brig, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipEngine = BooleanOptionItem.Create(101403, StringNames.Engine, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipKitchen = BooleanOptionItem.Create(101404, StringNames.Kitchen, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipCargoBay = BooleanOptionItem.Create(101405, StringNames.CargoBay, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipRecords = BooleanOptionItem.Create(101406, StringNames.Records, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipMainHall = BooleanOptionItem.Create(101407, StringNames.MainHall, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipNapRoom = BooleanOptionItem.Create(101408, SpawnPoint.NapRoom, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipMeetingRoom = BooleanOptionItem.Create(101409, StringNames.MeetingRoom, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipGapRoom = BooleanOptionItem.Create(101410, StringNames.GapRoom, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipVaultRoom = BooleanOptionItem.Create(101411, StringNames.VaultRoom, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipComms = BooleanOptionItem.Create(101412, StringNames.Comms, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipCockpit = BooleanOptionItem.Create(101413, StringNames.Cockpit, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipArmory = BooleanOptionItem.Create(101414, StringNames.Armory, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipViewingDeck = BooleanOptionItem.Create(101415, StringNames.ViewingDeck, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipSecurity = BooleanOptionItem.Create(101416, StringNames.Security, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipElectrical = BooleanOptionItem.Create(101417, StringNames.Electrical, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipMedical = BooleanOptionItem.Create(101418, StringNames.Medical, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipToilet = BooleanOptionItem.Create(101419, SpawnPoint.Toilet, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipShowers = BooleanOptionItem.Create(101420, StringNames.Showers, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            // Fungle
            Options.RandomSpawnFungle = BooleanOptionItem.Create(101431, StringNames.MapNameFungle, false, TabGroup.MainSettings, false).SetParent(Options.EnableRandomSpawn).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleKitchen = BooleanOptionItem.Create(101432, StringNames.Kitchen, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleBeach = BooleanOptionItem.Create(101433, StringNames.Beach, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleBonfire = BooleanOptionItem.Create(101434, SpawnPoint.Bonfire, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleGreenhouse = BooleanOptionItem.Create(101435, StringNames.Greenhouse, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleComms = BooleanOptionItem.Create(101436, StringNames.Comms, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleHighlands = BooleanOptionItem.Create(101437, StringNames.Highlands, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleCafeteria = BooleanOptionItem.Create(101438, StringNames.Cafeteria, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleRecRoom = BooleanOptionItem.Create(101439, StringNames.RecRoom, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleDropship = BooleanOptionItem.Create(101440, StringNames.Dropship, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleStorage = BooleanOptionItem.Create(101441, StringNames.Storage, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleMeetingRoom = BooleanOptionItem.Create(101442, StringNames.MeetingRoom, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleSleepingQuarters = BooleanOptionItem.Create(101443, StringNames.SleepingQuarters, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleLaboratory = BooleanOptionItem.Create(101444, StringNames.Laboratory, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleReactor = BooleanOptionItem.Create(101445, StringNames.Reactor, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleJungleTop = BooleanOptionItem.Create(101446, SpawnPoint.JungleTop, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleJungleBottom = BooleanOptionItem.Create(101447, SpawnPoint.JungleBottom, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleLookout = BooleanOptionItem.Create(101448, StringNames.Lookout, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleMiningPit = BooleanOptionItem.Create(101449, StringNames.MiningPit, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleUpperEngine = BooleanOptionItem.Create(101450, StringNames.UpperEngine, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFunglePrecipice = BooleanOptionItem.Create(101451, SpawnPoint.Precipice, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
        }

        public abstract class SpawnMap
        {
            public abstract Dictionary<OptionItem, Vector2> Positions { get; }
            public virtual void RandomTeleport(PlayerControl player)
            {
                var location = GetLocation();
                Logger.Info($"{player.Data.PlayerName}:{location}", "RandomSpawn");
                TP(player.NetTransform, location);
            }
            public Vector2 GetLocation()
            {
                var locations =
                    Positions.ToArray().Where(o => o.Key.GetBool()).Any()
                    ? Positions.ToArray().Where(o => o.Key.GetBool())
                    : Positions.ToArray();
                var location = locations.OrderBy(_ => Guid.NewGuid()).Take(1).FirstOrDefault();
                return location.Value;
            }
        }

        public class SkeldSpawnMap : SpawnMap
        {
            public override Dictionary<OptionItem, Vector2> Positions { get; } = new()
            {
                [Options.RandomSpawnSkeldCafeteria] = new(-1.0f, 3.0f),
                [Options.RandomSpawnSkeldWeapons] = new(9.3f, 1.0f),
                [Options.RandomSpawnSkeldLifeSupp] = new(6.5f, -3.8f),
                [Options.RandomSpawnSkeldNav] = new(16.5f, -4.8f),
                [Options.RandomSpawnSkeldShields] = new(9.3f, -12.3f),
                [Options.RandomSpawnSkeldComms] = new(4.0f, -15.5f),
                [Options.RandomSpawnSkeldStorage] = new(-1.5f, -15.5f),
                [Options.RandomSpawnSkeldAdmin] = new(4.5f, -7.9f),
                [Options.RandomSpawnSkeldElectrical] = new(-7.5f, -8.8f),
                [Options.RandomSpawnSkeldLowerEngine] = new(-17.0f, -13.5f),
                [Options.RandomSpawnSkeldUpperEngine] = new(-17.0f, -1.3f),
                [Options.RandomSpawnSkeldSecurity] = new(-13.5f, -5.5f),
                [Options.RandomSpawnSkeldReactor] = new(-20.5f, -5.5f),
                [Options.RandomSpawnSkeldMedBay] = new(-9.0f, -4.0f)
            };
        }
        public class MiraHQSpawnMap : SpawnMap
        {
            public override Dictionary<OptionItem, Vector2> Positions { get; } = new()
            {
                [Options.RandomSpawnMiraCafeteria] = new(25.5f, 2.0f),
                [Options.RandomSpawnMiraBalcony] = new(24.0f, -2.0f),
                [Options.RandomSpawnMiraStorage] = new(19.5f, 4.0f),
                [Options.RandomSpawnMiraJunction] = new(17.8f, 11.5f),
                [Options.RandomSpawnMiraComms] = new(15.3f, 3.8f),
                [Options.RandomSpawnMiraMedBay] = new(15.5f, -0.5f),
                [Options.RandomSpawnMiraLockerRoom] = new(9.0f, 1.0f),
                [Options.RandomSpawnMiraDecontamination] = new(6.1f, 6.0f),
                [Options.RandomSpawnMiraLaboratory] = new(9.5f, 12.0f),
                [Options.RandomSpawnMiraReactor] = new(2.5f, 10.5f),
                [Options.RandomSpawnMiraLaunchpad] = new(-4.5f, 2.0f),
                [Options.RandomSpawnMiraAdmin] = new(21.0f, 17.5f),
                [Options.RandomSpawnMiraOffice] = new(15.0f, 19.0f),
                [Options.RandomSpawnMiraGreenhouse] = new(17.8f, 23.0f)
            };
        }
        public class PolusSpawnMap : SpawnMap
        {
            public override Dictionary<OptionItem, Vector2> Positions { get; } = new()
            {

                [Options.RandomSpawnPolusOfficeLeft] = new(19.5f, -18.0f),
                [Options.RandomSpawnPolusOfficeRight] = new(26.0f, -17.0f),
                [Options.RandomSpawnPolusAdmin] = new(24.0f, -22.5f),
                [Options.RandomSpawnPolusComms] = new(12.5f, -16.0f),
                [Options.RandomSpawnPolusWeapons] = new(12.0f, -23.5f),
                [Options.RandomSpawnPolusBoilerRoom] = new(2.3f, -24.0f),
                [Options.RandomSpawnPolusLifeSupp] = new(2.0f, -17.5f),
                [Options.RandomSpawnPolusElectrical] = new(9.5f, -12.5f),
                [Options.RandomSpawnPolusSecurity] = new(3.0f, -12.0f),
                [Options.RandomSpawnPolusDropship] = new(16.7f, -3.0f),
                [Options.RandomSpawnPolusStorage] = new(20.5f, -12.0f),
                [Options.RandomSpawnPolusRocket] = new(26.7f, -8.5f),
                [Options.RandomSpawnPolusLaboratory] = new(36.5f, -7.5f),
                [Options.RandomSpawnPolusToilet] = new(34.0f, -10.0f),
                [Options.RandomSpawnPolusSpecimens] = new(36.5f, -22.0f)
            };
        }
        public class AirshipSpawnMap : SpawnMap
        {
            public override Dictionary<OptionItem, Vector2> Positions { get; } = new()
            {
                [Options.RandomSpawnAirshipBrig] = new(-0.7f, 8.5f),
                [Options.RandomSpawnAirshipEngine] = new(-0.7f, -1.0f),
                [Options.RandomSpawnAirshipKitchen] = new(-7.0f, -11.5f),
                [Options.RandomSpawnAirshipCargoBay] = new(33.5f, -1.5f),
                [Options.RandomSpawnAirshipRecords] = new(20.0f, 10.5f),
                [Options.RandomSpawnAirshipMainHall] = new(15.5f, 0.0f),
                [Options.RandomSpawnAirshipNapRoom] = new(6.3f, 2.5f),
                [Options.RandomSpawnAirshipMeetingRoom] = new(17.1f, 14.9f),
                [Options.RandomSpawnAirshipGapRoom] = new(12.0f, 8.5f),
                [Options.RandomSpawnAirshipVaultRoom] = new(-8.9f, 12.2f),
                [Options.RandomSpawnAirshipComms] = new(-13.3f, 1.3f),
                [Options.RandomSpawnAirshipCockpit] = new(-23.5f, -1.6f),
                [Options.RandomSpawnAirshipArmory] = new(-10.3f, -5.9f),
                [Options.RandomSpawnAirshipViewingDeck] = new(-13.7f, -12.6f),
                [Options.RandomSpawnAirshipSecurity] = new(5.8f, -10.8f),
                [Options.RandomSpawnAirshipElectrical] = new(16.3f, -8.8f),
                [Options.RandomSpawnAirshipMedical] = new(29.0f, -6.2f),
                [Options.RandomSpawnAirshipToilet] = new(30.9f, 6.8f),
                [Options.RandomSpawnAirshipShowers] = new(21.2f, -0.8f)
            };
        }
        public class FungleSpawnMap : SpawnMap
        {
            public override Dictionary<OptionItem, Vector2> Positions { get; } = new()
            {
                [Options.RandomSpawnFungleKitchen] = new(-17.8f, -7.3f),
                [Options.RandomSpawnFungleBeach] = new(-21.3f, 3.0f),   //海岸
                [Options.RandomSpawnFungleCafeteria] = new(-16.9f, 5.5f),
                [Options.RandomSpawnFungleRecRoom] = new(-17.7f, 0.0f),
                [Options.RandomSpawnFungleBonfire] = new(-9.7f, 2.7f),  //焚き火
                [Options.RandomSpawnFungleDropship] = new(-7.6f, 10.4f),
                [Options.RandomSpawnFungleStorage] = new(2.3f, 4.3f),
                [Options.RandomSpawnFungleMeetingRoom] = new(-4.2f, -2.2f),
                [Options.RandomSpawnFungleSleepingQuarters] = new(1.7f, -1.4f),  //宿舎
                [Options.RandomSpawnFungleLaboratory] = new(-4.2f, -7.9f),
                [Options.RandomSpawnFungleGreenhouse] = new(9.2f, -11.8f),
                [Options.RandomSpawnFungleReactor] = new(21.8f, -7.2f),
                [Options.RandomSpawnFungleJungleTop] = new(4.2f, -5.3f),
                [Options.RandomSpawnFungleJungleBottom] = new(15.9f, -14.8f),
                [Options.RandomSpawnFungleLookout] = new(6.4f, 3.1f),
                [Options.RandomSpawnFungleMiningPit] = new(12.5f, 9.6f),
                [Options.RandomSpawnFungleHighlands] = new(15.5f, 3.9f),    //展望台右の高地
                [Options.RandomSpawnFungleUpperEngine] = new(21.9f, 3.2f),
                [Options.RandomSpawnFunglePrecipice] = new(19.8f, 7.3f),   //通信室下の崖
                [Options.RandomSpawnFungleComms] = new(20.9f, 13.4f),
            };
        }
    }
}