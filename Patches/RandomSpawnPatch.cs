using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
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
        public static Dictionary<byte, bool> FirstTP = new();
        public static Dictionary<PlayerControl, Vector2> FastSpawnPosition = new();
        public static bool hostReady;
        [HarmonyPatch(typeof(CustomNetworkTransform), nameof(CustomNetworkTransform.RpcSnapTo))]
        public class RpcSnapToPatch
        {
            public static void Postfix(CustomNetworkTransform __instance, Vector2 position)
            {
                var player = __instance.myPlayer;
                //Logger.Info($"RpcSnapToPost:{player.name} pos:{position}", "RandomSpawn");
                if (!AmongUsClient.Instance.AmHost) return;
                if (Main.NormalOptions.MapId != 4) return;//AirShip以外無効
                if (FirstTP.TryGetValue(player.PlayerId, out var first) && first)
                {
                    hostReady = true;
                    //ホスト用処理
                    //他視点へRPCを最初に送るのはスポーン位置選択後のため
                    //クライアントへRPCを発行するときにはすでにクライアントの初期配置は終わっている。
                    AirshipSpawn(player);
                }
            }
        }
        [HarmonyPatch(typeof(CustomNetworkTransform), nameof(CustomNetworkTransform.HandleRpc))]
        public class HandleRpcPatch
        {
            public static void Postfix(CustomNetworkTransform __instance)
            {
                var player = __instance.myPlayer;
                //Logger.Info($"HandleRpcPost:{player.name}", "RandomSpawn");

                if (!AmongUsClient.Instance.AmHost) return;
                if (Main.NormalOptions.MapId != 4) return;//AirShip以外無効

                if (FirstTP.TryGetValue(player.PlayerId, out var first) && first)
                {
                    //クライアント用処理
                    //他視点へRPCを最初に送るのはスポーン位置選択後のため
                    //ランダムスポーン発生
                    AirshipSpawn(player);
                }
            }
        }
        [HarmonyPatch(typeof(CustomNetworkTransform), nameof(CustomNetworkTransform.SnapTo), typeof(Vector2), typeof(ushort))]
        public class SnapToPatch
        {
            public static void Postfix(CustomNetworkTransform __instance, Vector2 position, ushort minSid)
            {
                var player = __instance.myPlayer;
                //Logger.Info($"SnapTo:{player.name} pos:{position} minSid={minSid}", "RandomSpawn");
            }
        }
        [HarmonyPatch(typeof(SpawnInMinigame), nameof(SpawnInMinigame.Begin))]
        public class SpawnInMinigamePatch
        {
            public static void Postfix()
            {
                Logger.Info($"BeginPost", "SpawnInMinigame");
                if (!AmongUsClient.Instance.AmHost) return;
                hostReady = true;
            }
        }
        public static void AirshipSpawn(PlayerControl player)
        {
            FirstTP[player.PlayerId] = false;
            if (player.Is(CustomRoles.Penguin))
            {
                var penguin = player.GetRoleClass() as Penguin;
                penguin?.OnSpawnAirship();
            }
            player.RpcResetAbilityCooldown();
            if (Options.FixFirstKillCooldown.GetBool() && !MeetingStates.MeetingCalled) player.SetKillCooldown(Main.AllPlayerKillCooldown[player.PlayerId]);
            if (IsRandomSpawn())
            {
                new AirshipSpawnMap().RandomTeleport(player);
            }
            else if (player.Is(CustomRoles.GM))
            {
                new AirshipSpawnMap().FirstTeleport(player);
            }
            foreach (var (sp, pos) in FastSpawnPosition)
            {
                //早湧きした人を船外から初期位置に戻す
                sp.RpcSnapToDesync(player, pos);
            }
            if (!hostReady)
            {
                //ホストのSpawnMiniGame開始までに湧いたプレイヤーを記録
                FastSpawnPosition[player] = player.transform.position;
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
        public static void SetupCustomOption()
        {
            // Skeld
            Options.RandomSpawnSkeld = BooleanOptionItem.Create(103000, StringNames.MapNameSkeld, false, TabGroup.MainSettings, false).SetParent(Options.EnableRandomSpawn).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldCafeteria = BooleanOptionItem.Create(103001, StringNames.Cafeteria, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldWeapons = BooleanOptionItem.Create(103002, StringNames.Weapons, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldShields = BooleanOptionItem.Create(103003, StringNames.Shields, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldStorage = BooleanOptionItem.Create(103004, StringNames.Storage, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldLowerEngine = BooleanOptionItem.Create(103005, StringNames.LowerEngine, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldUpperEngine = BooleanOptionItem.Create(103006, StringNames.UpperEngine, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldLifeSupp = BooleanOptionItem.Create(103007, StringNames.LifeSupp, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldNav = BooleanOptionItem.Create(103008, StringNames.Nav, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldComms = BooleanOptionItem.Create(103009, StringNames.Comms, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldAdmin = BooleanOptionItem.Create(103010, StringNames.Admin, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldElectrical = BooleanOptionItem.Create(103011, StringNames.Electrical, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldSecurity = BooleanOptionItem.Create(103012, StringNames.Security, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldReactor = BooleanOptionItem.Create(103013, StringNames.Reactor, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnSkeldMedBay = BooleanOptionItem.Create(103014, StringNames.MedBay, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnSkeld).SetGameMode(CustomGameMode.All);
            // Mira
            Options.RandomSpawnMira = BooleanOptionItem.Create(103100, StringNames.MapNameMira, false, TabGroup.MainSettings, false).SetParent(Options.EnableRandomSpawn).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraCafeteria = BooleanOptionItem.Create(103101, StringNames.Cafeteria, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraComms = BooleanOptionItem.Create(103102, StringNames.Comms, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraDecontamination = BooleanOptionItem.Create(103103, StringNames.Decontamination, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraReactor = BooleanOptionItem.Create(103104, StringNames.Reactor, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraLaunchpad = BooleanOptionItem.Create(103105, StringNames.Launchpad, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraAdmin = BooleanOptionItem.Create(103106, StringNames.Admin, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraBalcony = BooleanOptionItem.Create(103107, StringNames.Balcony, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraStorage = BooleanOptionItem.Create(103108, StringNames.Storage, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraJunction = BooleanOptionItem.Create(103109, SpawnPoint.Junction, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraMedBay = BooleanOptionItem.Create(103110, StringNames.MedBay, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraLockerRoom = BooleanOptionItem.Create(103111, StringNames.LockerRoom, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraLaboratory = BooleanOptionItem.Create(103112, StringNames.Laboratory, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraOffice = BooleanOptionItem.Create(103113, StringNames.Office, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnMiraGreenhouse = BooleanOptionItem.Create(103114, StringNames.Greenhouse, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnMira).SetGameMode(CustomGameMode.All);
            // Polus
            Options.RandomSpawnPolus = BooleanOptionItem.Create(103200, StringNames.MapNamePolus, false, TabGroup.MainSettings, false).SetParent(Options.EnableRandomSpawn).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusOfficeLeft = BooleanOptionItem.Create(103201, SpawnPoint.OfficeLeft, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusBoilerRoom = BooleanOptionItem.Create(103202, StringNames.BoilerRoom, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusSecurity = BooleanOptionItem.Create(103203, StringNames.Security, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusDropship = BooleanOptionItem.Create(103204, StringNames.Dropship, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusLaboratory = BooleanOptionItem.Create(103205, StringNames.Laboratory, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusSpecimens = BooleanOptionItem.Create(103206, StringNames.Specimens, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusOfficeRight = BooleanOptionItem.Create(103207, SpawnPoint.OfficeRight, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusAdmin = BooleanOptionItem.Create(103208, StringNames.Admin, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusComms = BooleanOptionItem.Create(103209, StringNames.Comms, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusWeapons = BooleanOptionItem.Create(103210, StringNames.Weapons, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusLifeSupp = BooleanOptionItem.Create(103211, StringNames.LifeSupp, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusElectrical = BooleanOptionItem.Create(103212, StringNames.Electrical, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusStorage = BooleanOptionItem.Create(103213, StringNames.Storage, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusRocket = BooleanOptionItem.Create(103214, SpawnPoint.Rocket, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnPolusToilet = BooleanOptionItem.Create(103215, SpawnPoint.Toilet, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnPolus).SetGameMode(CustomGameMode.All);
            // Airship
            Options.RandomSpawnAirship = BooleanOptionItem.Create(103400, StringNames.MapNameAirship, false, TabGroup.MainSettings, false).SetParent(Options.EnableRandomSpawn).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipBrig = BooleanOptionItem.Create(103401, StringNames.Brig, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipEngine = BooleanOptionItem.Create(103402, StringNames.Engine, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipKitchen = BooleanOptionItem.Create(103403, StringNames.Kitchen, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipCargoBay = BooleanOptionItem.Create(103404, StringNames.CargoBay, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipRecords = BooleanOptionItem.Create(103405, StringNames.Records, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipMainHall = BooleanOptionItem.Create(103406, StringNames.MainHall, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipNapRoom = BooleanOptionItem.Create(103407, SpawnPoint.NapRoom, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipMeetingRoom = BooleanOptionItem.Create(103408, StringNames.MeetingRoom, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipGapRoom = BooleanOptionItem.Create(103409, StringNames.GapRoom, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipVaultRoom = BooleanOptionItem.Create(103410, StringNames.VaultRoom, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipComms = BooleanOptionItem.Create(103411, StringNames.Comms, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipCockpit = BooleanOptionItem.Create(103412, StringNames.Cockpit, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipArmory = BooleanOptionItem.Create(103413, StringNames.Armory, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipViewingDeck = BooleanOptionItem.Create(103414, StringNames.ViewingDeck, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipSecurity = BooleanOptionItem.Create(103415, StringNames.Security, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipElectrical = BooleanOptionItem.Create(103416, StringNames.Electrical, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipMedical = BooleanOptionItem.Create(103417, StringNames.Medical, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipToilet = BooleanOptionItem.Create(103418, SpawnPoint.Toilet, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnAirshipShowers = BooleanOptionItem.Create(103419, StringNames.Showers, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnAirship).SetGameMode(CustomGameMode.All);
            // Fungle
            Options.RandomSpawnFungle = BooleanOptionItem.Create(103500, StringNames.MapNameFungle, false, TabGroup.MainSettings, false).SetParent(Options.EnableRandomSpawn).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleKitchen = BooleanOptionItem.Create(103501, StringNames.Kitchen, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleBeach = BooleanOptionItem.Create(103502, StringNames.Beach, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleBonfire = BooleanOptionItem.Create(103503, SpawnPoint.Bonfire, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleGreenhouse = BooleanOptionItem.Create(103504, StringNames.Greenhouse, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleComms = BooleanOptionItem.Create(103505, StringNames.Comms, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleHighlands = BooleanOptionItem.Create(103506, StringNames.Highlands, true, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleCafeteria = BooleanOptionItem.Create(103507, StringNames.Cafeteria, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleRecRoom = BooleanOptionItem.Create(103508, StringNames.RecRoom, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleDropship = BooleanOptionItem.Create(103509, StringNames.Dropship, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleStorage = BooleanOptionItem.Create(103510, StringNames.Storage, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleMeetingRoom = BooleanOptionItem.Create(103511, StringNames.MeetingRoom, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleSleepingQuarters = BooleanOptionItem.Create(103512, StringNames.SleepingQuarters, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleLaboratory = BooleanOptionItem.Create(103513, StringNames.Laboratory, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleReactor = BooleanOptionItem.Create(103514, StringNames.Reactor, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleJungleTop = BooleanOptionItem.Create(103515, SpawnPoint.JungleTop, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleJungleBottom = BooleanOptionItem.Create(103516, SpawnPoint.JungleBottom, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleLookout = BooleanOptionItem.Create(103517, StringNames.Lookout, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleMiningPit = BooleanOptionItem.Create(103518, StringNames.MiningPit, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFungleUpperEngine = BooleanOptionItem.Create(103519, StringNames.UpperEngine, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
            Options.RandomSpawnFunglePrecipice = BooleanOptionItem.Create(103520, SpawnPoint.Precipice, false, TabGroup.MainSettings, false).SetParent(Options.RandomSpawnFungle).SetGameMode(CustomGameMode.All);
        }

        public abstract class SpawnMap
        {
            public abstract Dictionary<OptionItem, Vector2> Positions { get; }
            public virtual void RandomTeleport(PlayerControl player)
            {
                Teleport(player, true);
            }
            public virtual void FirstTeleport(PlayerControl player)
            {
                Teleport(player, false);
            }

            private void Teleport(PlayerControl player, bool isRadndom)
            {
                var location = GetLocation(!isRadndom);
                Logger.Info($"{player.Data.PlayerName}:{location}", "RandomSpawn");
                player.RpcSnapTo(location);
            }

            public Vector2 GetLocation(Boolean first = false)
            {
                var EnableLocations = Positions.Where(o => o.Key.GetBool()).ToArray();
                var locations = EnableLocations.Length != 0 ? EnableLocations : Positions.ToArray();
                if (first) return locations[0].Value;
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