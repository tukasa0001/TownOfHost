using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace TownOfHost
{
    [Flags]
    public enum CustomGameMode
    {
        Standard = 0x01,
        HideAndSeek = 0x02,
        All = int.MaxValue
    }

    [HarmonyPatch]
    public static class Options
    {
        static Task taskOptionsLoad;
        [HarmonyPatch(typeof(TranslationController), nameof(TranslationController.Initialize)), HarmonyPostfix]
        public static void OptionsLoadStart()
        {
            Logger.Info("Options.Load Start", "Options");
            taskOptionsLoad = Task.Run(Load);
        }
        [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPostfix]
        public static void WaitOptionsLoad()
        {
            taskOptionsLoad.Wait();
            Logger.Info("Options.Load End", "Options");
        }
        // オプションId
        public const int PresetId = 0;

        // プリセット
        private static readonly string[] presets =
        {
            Main.Preset1.Value, Main.Preset2.Value, Main.Preset3.Value,
            Main.Preset4.Value, Main.Preset5.Value
        };

        // ゲームモード
        public static OptionItem GameMode;
        public static CustomGameMode CurrentGameMode
            => GameMode.CurrentValue == 0 ? CustomGameMode.Standard : CustomGameMode.HideAndSeek;

        public static readonly string[] gameModes =
        {
            "Standard", "HideAndSeek",
        };

        // MapActive
        public static bool IsActiveSkeld => AddedTheSkeld.GetBool() || Main.NormalOptions.MapId == 0;
        public static bool IsActiveMiraHQ => AddedMiraHQ.GetBool() || Main.NormalOptions.MapId == 1;
        public static bool IsActivePolus => AddedPolus.GetBool() || Main.NormalOptions.MapId == 2;
        public static bool IsActiveAirship => AddedTheAirShip.GetBool() || Main.NormalOptions.MapId == 4;

        // 役職数・確率
        public static Dictionary<CustomRoles, int> roleCounts;
        public static Dictionary<CustomRoles, float> roleSpawnChances;
        public static Dictionary<CustomRoles, OptionItem> CustomRoleCounts;
        public static Dictionary<CustomRoles, StringOptionItem> CustomRoleSpawnChances;
        public static readonly string[] rates =
        {
            "Rate0",  "Rate5",  "Rate10", "Rate20", "Rate30", "Rate40",
            "Rate50", "Rate60", "Rate70", "Rate80", "Rate90", "Rate100",
        };
        public static readonly string[] ratesZeroOne =
        {
            "Rate0", /*"Rate10", "Rate20", "Rate30", "Rate40", "Rate50",
            "Rate60", "Rate70", "Rate80", "Rate90", */"Rate100",
        };

        // 各役職の詳細設定
        public static OptionItem EnableGM;
        public static float DefaultKillCooldown = Main.NormalOptions?.KillCooldown ?? 20;
        public static OptionItem VampireKillDelay;
        public static OptionItem ShapeMasterShapeshiftDuration;
        public static OptionItem DefaultShapeshiftCooldown;
        public static OptionItem CanMakeMadmateCount;
        public static OptionItem MadGuardianCanSeeWhoTriedToKill;
        public static OptionItem MadSnitchCanVent;
        public static OptionItem MadSnitchCanAlsoBeExposedToImpostor;
        public static OptionItem MadmateCanFixLightsOut; // TODO:mii-47 マッド役職統一
        public static OptionItem MadmateCanFixComms;
        public static OptionItem MadmateHasImpostorVision;
        public static OptionItem MadmateCanSeeKillFlash;
        public static OptionItem MadmateCanSeeOtherVotes;
        public static OptionItem MadmateCanSeeDeathReason;
        public static OptionItem MadmateRevengeCrewmate;
        public static OptionItem MadmateVentCooldown;
        public static OptionItem MadmateVentMaxTime;

        public static OptionItem EvilWatcherChance;
        public static OptionItem LighterTaskCompletedVision;
        public static OptionItem LighterTaskCompletedDisableLightOut;
        public static OptionItem MayorAdditionalVote;
        public static OptionItem MayorHasPortableButton;
        public static OptionItem MayorNumOfUseButton;
        public static OptionItem DoctorTaskCompletedBatteryCharge;
        public static OptionItem SpeedBoosterUpSpeed; //加速値
        public static OptionItem SpeedBoosterTaskTrigger; //効果を発動するタスク完了数
        public static OptionItem TrapperBlockMoveTime;
        public static OptionItem CanTerroristSuicideWin;
        public static OptionItem ArsonistDouseTime;
        public static OptionItem ArsonistCooldown;
        public static OptionItem KillFlashDuration;

        // HideAndSeek
        public static OptionItem AllowCloseDoors;
        public static OptionItem KillDelay;
        public static OptionItem IgnoreCosmetics;
        public static OptionItem IgnoreVent;
        public static float HideAndSeekKillDelayTimer = 0f;

        // タスク無効化
        public static OptionItem DisableTasks;
        public static OptionItem DisableSwipeCard;
        public static OptionItem DisableSubmitScan;
        public static OptionItem DisableUnlockSafe;
        public static OptionItem DisableUploadData;
        public static OptionItem DisableStartReactor;
        public static OptionItem DisableResetBreaker;

        //デバイスブロック
        public static OptionItem DisableDevices;
        public static OptionItem DisableSkeldDevices;
        public static OptionItem DisableSkeldAdmin;
        public static OptionItem DisableSkeldCamera;
        public static OptionItem DisableMiraHQDevices;
        public static OptionItem DisableMiraHQAdmin;
        public static OptionItem DisableMiraHQDoorLog;
        public static OptionItem DisablePolusDevices;
        public static OptionItem DisablePolusAdmin;
        public static OptionItem DisablePolusCamera;
        public static OptionItem DisablePolusVital;
        public static OptionItem DisableAirshipDevices;
        public static OptionItem DisableAirshipCockpitAdmin;
        public static OptionItem DisableAirshipRecordsAdmin;
        public static OptionItem DisableAirshipCamera;
        public static OptionItem DisableAirshipVital;
        public static OptionItem DisableDevicesIgnoreConditions;
        public static OptionItem DisableDevicesIgnoreImpostors;
        public static OptionItem DisableDevicesIgnoreMadmates;
        public static OptionItem DisableDevicesIgnoreNeutrals;
        public static OptionItem DisableDevicesIgnoreCrewmates;
        public static OptionItem DisableDevicesIgnoreAfterAnyoneDied;

        // ランダムマップ
        public static OptionItem RandomMapsMode;
        public static OptionItem AddedTheSkeld;
        public static OptionItem AddedMiraHQ;
        public static OptionItem AddedPolus;
        public static OptionItem AddedTheAirShip;
        public static OptionItem AddedDleks;

        // ランダムスポーン
        public static OptionItem RandomSpawn;
        public static OptionItem AirshipAdditionalSpawn;

        // 投票モード
        public static OptionItem VoteMode;
        public static OptionItem WhenSkipVote;
        public static OptionItem WhenSkipVoteIgnoreFirstMeeting;
        public static OptionItem WhenSkipVoteIgnoreNoDeadBody;
        public static OptionItem WhenSkipVoteIgnoreEmergency;
        public static OptionItem WhenNonVote;
        public static OptionItem WhenTie;
        public static readonly string[] voteModes =
        {
            "Default", "Suicide", "SelfVote", "Skip"
        };
        public static readonly string[] tieModes =
        {
            "TieMode.Default", "TieMode.All", "TieMode.Random"
        };
        public static VoteMode GetWhenSkipVote() => (VoteMode)WhenSkipVote.GetValue();
        public static VoteMode GetWhenNonVote() => (VoteMode)WhenNonVote.GetValue();

        // ボタン回数
        public static OptionItem SyncButtonMode;
        public static OptionItem SyncedButtonCount;
        public static int UsedButtonCount = 0;

        // 全員生存時の会議時間
        public static OptionItem AllAliveMeeting;
        public static OptionItem AllAliveMeetingTime;

        // 追加の緊急ボタンクールダウン
        public static OptionItem AdditionalEmergencyCooldown;
        public static OptionItem AdditionalEmergencyCooldownThreshold;
        public static OptionItem AdditionalEmergencyCooldownTime;

        //転落死
        public static OptionItem LadderDeath;
        public static OptionItem LadderDeathChance;
        //エレキ構造変化
        public static OptionItem AirShipVariableElectrical;

        // 通常モードでかくれんぼ
        public static bool IsStandardHAS => StandardHAS.GetBool() && CurrentGameMode == CustomGameMode.Standard;
        public static OptionItem StandardHAS;
        public static OptionItem StandardHASWaitingTime;

        // リアクターの時間制御
        public static OptionItem SabotageTimeControl;
        public static OptionItem PolusReactorTimeLimit;
        public static OptionItem AirshipReactorTimeLimit;

        // 停電の特殊設定
        public static OptionItem LightsOutSpecialSettings;
        public static OptionItem DisableAirshipViewingDeckLightsPanel;
        public static OptionItem DisableAirshipGapRoomLightsPanel;
        public static OptionItem DisableAirshipCargoLightsPanel;

        // タスク上書き
        public static OverrideTasksData MadGuardianTasks;
        public static OverrideTasksData TerroristTasks;
        public static OverrideTasksData SnitchTasks;
        public static OverrideTasksData MadSnitchTasks;

        // その他
        public static OptionItem FixFirstKillCooldown;
        public static OptionItem DisableTaskWin;
        public static OptionItem GhostCanSeeOtherRoles;
        public static OptionItem GhostCanSeeOtherVotes;
        public static OptionItem GhostCanSeeDeathReason;
        public static OptionItem GhostIgnoreTasks;
        public static OptionItem CommsCamouflage;

        // プリセット対象外
        public static OptionItem NoGameEnd;
        public static OptionItem AutoDisplayLastResult;
        public static OptionItem AutoDisplayKillLog;
        public static OptionItem SuffixMode;
        public static OptionItem HideGameSettings;
        public static OptionItem ColorNameMode;
        public static OptionItem ChangeNameToRoleInfo;
        public static OptionItem RoleAssigningAlgorithm;

        public static OptionItem ApplyDenyNameList;
        public static OptionItem KickPlayerFriendCodeNotExist;
        public static OptionItem ApplyBanList;

        public static readonly string[] suffixModes =
        {
            "SuffixMode.None",
            "SuffixMode.Version",
            "SuffixMode.Streaming",
            "SuffixMode.Recording",
            "SuffixMode.RoomHost",
            "SuffixMode.OriginalName"
        };
        public static readonly string[] RoleAssigningAlgorithms =
        {
            "RoleAssigningAlgorithm.Default",
            "RoleAssigningAlgorithm.NetRandom",
            "RoleAssigningAlgorithm.HashRandom",
            "RoleAssigningAlgorithm.Xorshift",
        };
        public static SuffixModes GetSuffixMode()
        {
            return (SuffixModes)SuffixMode.GetValue();
        }



        public static int SnitchExposeTaskLeft = 1;


        public static bool IsEvilWatcher = false;
        public static void SetWatcherTeam(float EvilWatcherRate)
        {
            EvilWatcherRate = Options.EvilWatcherChance.GetFloat();
            IsEvilWatcher = UnityEngine.Random.Range(1, 100) < EvilWatcherRate;
        }
        public static bool IsLoaded = false;

        static Options()
        {
            ResetRoleCounts();
        }
        public static void ResetRoleCounts()
        {
            roleCounts = new Dictionary<CustomRoles, int>();
            roleSpawnChances = new Dictionary<CustomRoles, float>();

            foreach (var role in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>())
            {
                roleCounts.Add(role, 0);
                roleSpawnChances.Add(role, 0);
            }
        }

        public static void SetRoleCount(CustomRoles role, int count)
        {
            roleCounts[role] = count;

            if (CustomRoleCounts.TryGetValue(role, out var option))
            {
                option.SetValue(count - 1);
            }
        }

        public static int GetRoleCount(CustomRoles role)
        {
            var chance = CustomRoleSpawnChances.TryGetValue(role, out var sc) ? sc.GetChance() : 0;
            return chance == 0 ? 0 : CustomRoleCounts.TryGetValue(role, out var option) ? option.GetInt() : roleCounts[role];
        }

        public static float GetRoleChance(CustomRoles role)
        {
            return CustomRoleSpawnChances.TryGetValue(role, out var option) ? option.GetValue()/* / 10f */ : roleSpawnChances[role];
        }
        public static void Load()
        {
            if (IsLoaded) return;
            // プリセット
            _ = PresetOptionItem.Create(0, TabGroup.MainSettings)
                .SetColor(new Color32(204, 204, 0, 255))
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All);

            // ゲームモード
            GameMode = StringOptionItem.Create(1, "GameMode", gameModes, 0, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All);

            #region 役職・詳細設定
            CustomRoleCounts = new();
            CustomRoleSpawnChances = new();
            // GM
            EnableGM = BooleanOptionItem.Create(100, "GM", false, TabGroup.MainSettings, false)
                .SetColor(Utils.GetRoleColor(CustomRoles.GM))
                .SetHeader(true)
                .SetGameMode(CustomGameMode.Standard);

            // Impostor
            BountyHunter.SetupCustomOption();
            SerialKiller.SetupCustomOption();
            SetupRoleOptions(1200, TabGroup.ImpostorRoles, CustomRoles.ShapeMaster);
            ShapeMasterShapeshiftDuration = FloatOptionItem.Create(1210, "ShapeMasterShapeshiftDuration", new(1, 1000, 1), 10, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.ShapeMaster]);
            Vampire.SetupCustomOption();
            SetupRoleOptions(1400, TabGroup.ImpostorRoles, CustomRoles.Warlock);
            Witch.SetupCustomOption();
            SetupRoleOptions(1600, TabGroup.ImpostorRoles, CustomRoles.Mafia);
            FireWorks.SetupCustomOption();
            Sniper.SetupCustomOption();
            SetupRoleOptions(2000, TabGroup.ImpostorRoles, CustomRoles.Puppeteer);
            Mare.SetupCustomOption();
            TimeThief.SetupCustomOption();
            EvilTracker.SetupCustomOption();
            EvilHacker.SetupCustomOption();

            DefaultShapeshiftCooldown = FloatOptionItem.Create(5011, "DefaultShapeshiftCooldown", new(5f, 999f, 5f), 15f, TabGroup.ImpostorRoles, false)
                .SetHeader(true)
                .SetValueFormat(OptionFormat.Seconds);
            CanMakeMadmateCount = IntegerOptionItem.Create(5012, "CanMakeMadmateCount", new(0, 15, 1), 0, TabGroup.ImpostorRoles, false)
                .SetValueFormat(OptionFormat.Times);

            // Madmate
            SetupRoleOptions(10000, TabGroup.ImpostorRoles, CustomRoles.Madmate);
            SetupRoleOptions(10100, TabGroup.ImpostorRoles, CustomRoles.MadGuardian);
            MadGuardianCanSeeWhoTriedToKill = BooleanOptionItem.Create(10110, "MadGuardianCanSeeWhoTriedToKill", false, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.MadGuardian]);
            //ID10120~10123を使用
            MadGuardianTasks = OverrideTasksData.Create(10120, TabGroup.ImpostorRoles, CustomRoles.MadGuardian);
            SetupRoleOptions(10200, TabGroup.ImpostorRoles, CustomRoles.MadSnitch);
            MadSnitchCanVent = BooleanOptionItem.Create(10210, "CanVent", false, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.MadSnitch]);
            MadSnitchCanAlsoBeExposedToImpostor = BooleanOptionItem.Create(10211, "MadSnitchCanAlsoBeExposedToImpostor", false, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.MadSnitch]);
            //ID10220~10223を使用
            MadSnitchTasks = OverrideTasksData.Create(10220, TabGroup.ImpostorRoles, CustomRoles.MadSnitch);
            // Madmate Common Options
            MadmateCanFixLightsOut = BooleanOptionItem.Create(15010, "MadmateCanFixLightsOut", false, TabGroup.ImpostorRoles, false)
                .SetHeader(true);
            MadmateCanFixComms = BooleanOptionItem.Create(15011, "MadmateCanFixComms", false, TabGroup.ImpostorRoles, false);
            MadmateHasImpostorVision = BooleanOptionItem.Create(15012, "MadmateHasImpostorVision", false, TabGroup.ImpostorRoles, false);
            MadmateCanSeeKillFlash = BooleanOptionItem.Create(15015, "MadmateCanSeeKillFlash", false, TabGroup.ImpostorRoles, false);
            MadmateCanSeeOtherVotes = BooleanOptionItem.Create(15016, "MadmateCanSeeOtherVotes", false, TabGroup.ImpostorRoles, false);
            MadmateCanSeeDeathReason = BooleanOptionItem.Create(15017, "MadmateCanSeeDeathReason", false, TabGroup.ImpostorRoles, false);
            MadmateRevengeCrewmate = BooleanOptionItem.Create(15018, "MadmateExileCrewmate", false, TabGroup.ImpostorRoles, false);
            MadmateVentCooldown = FloatOptionItem.Create(15213, "MadmateVentCooldown", new(0f, 180f, 5f), 0f, TabGroup.ImpostorRoles, false)
                .SetValueFormat(OptionFormat.Seconds);
            MadmateVentMaxTime = FloatOptionItem.Create(15214, "MadmateVentMaxTime", new(0f, 180f, 5f), 0f, TabGroup.ImpostorRoles, false)
                .SetValueFormat(OptionFormat.Seconds);
            // Both
            SetupRoleOptions(30000, TabGroup.NeutralRoles, CustomRoles.Watcher);
            EvilWatcherChance = IntegerOptionItem.Create(30010, "EvilWatcherChance", new(0, 100, 10), 0, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Watcher])
                .SetValueFormat(OptionFormat.Percent);
            // Crewmate
            SetupRoleOptions(20000, TabGroup.CrewmateRoles, CustomRoles.Bait);
            SetupRoleOptions(20100, TabGroup.CrewmateRoles, CustomRoles.Lighter);
            LighterTaskCompletedVision = FloatOptionItem.Create(20110, "LighterTaskCompletedVision", new(0f, 5f, 0.25f), 2f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lighter])
                .SetValueFormat(OptionFormat.Multiplier);
            LighterTaskCompletedDisableLightOut = BooleanOptionItem.Create(20111, "LighterTaskCompletedDisableLightOut", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lighter]);
            SetupRoleOptions(20200, TabGroup.CrewmateRoles, CustomRoles.Mayor);
            MayorAdditionalVote = IntegerOptionItem.Create(20210, "MayorAdditionalVote", new(1, 99, 1), 1, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mayor])
                .SetValueFormat(OptionFormat.Votes);
            MayorHasPortableButton = BooleanOptionItem.Create(20211, "MayorHasPortableButton", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mayor]);
            MayorNumOfUseButton = IntegerOptionItem.Create(20212, "MayorNumOfUseButton", new(1, 99, 1), 1, TabGroup.CrewmateRoles, false).SetParent(MayorHasPortableButton)
                .SetValueFormat(OptionFormat.Times);
            SabotageMaster.SetupCustomOption();
            Sheriff.SetupCustomOption();
            Snitch.SetupCustomOption();
            SetupRoleOptions(20600, TabGroup.CrewmateRoles, CustomRoles.SpeedBooster);
            SpeedBoosterUpSpeed = FloatOptionItem.Create(20610, "SpeedBoosterUpSpeed", new(0.1f, 0.5f, 0.1f), 0.3f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SpeedBooster])
                .SetValueFormat(OptionFormat.Multiplier);
            SpeedBoosterTaskTrigger = IntegerOptionItem.Create(20611, "SpeedBoosterTaskTrigger", new(1, 99, 1), 5, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SpeedBooster])
                .SetValueFormat(OptionFormat.Pieces);
            SetupRoleOptions(20700, TabGroup.CrewmateRoles, CustomRoles.Doctor);
            DoctorTaskCompletedBatteryCharge = FloatOptionItem.Create(20710, "DoctorTaskCompletedBatteryCharge", new(0f, 10f, 1f), 5f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Doctor])
                .SetValueFormat(OptionFormat.Seconds);
            SetupRoleOptions(20800, TabGroup.CrewmateRoles, CustomRoles.Trapper);
            TrapperBlockMoveTime = FloatOptionItem.Create(20810, "TrapperBlockMoveTime", new(1f, 180f, 1f), 5f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Trapper])
                .SetValueFormat(OptionFormat.Seconds);
            SetupRoleOptions(20900, TabGroup.CrewmateRoles, CustomRoles.Dictator);
            SetupRoleOptions(21000, TabGroup.CrewmateRoles, CustomRoles.Seer);
            TimeManager.SetupCustomOption();

            // Neutral
            SetupRoleOptions(50500, TabGroup.NeutralRoles, CustomRoles.Arsonist);
            ArsonistDouseTime = FloatOptionItem.Create(50510, "ArsonistDouseTime", new(1f, 10f, 1f), 3f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Arsonist])
                .SetValueFormat(OptionFormat.Seconds);
            ArsonistCooldown = FloatOptionItem.Create(50511, "Cooldown", new(5f, 100f, 1f), 10f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Arsonist])
                .SetValueFormat(OptionFormat.Seconds);
            SetupRoleOptions(50000, TabGroup.NeutralRoles, CustomRoles.Jester);
            SetupRoleOptions(50100, TabGroup.NeutralRoles, CustomRoles.Opportunist);
            SetupRoleOptions(50200, TabGroup.NeutralRoles, CustomRoles.Terrorist);
            CanTerroristSuicideWin = BooleanOptionItem.Create(50210, "CanTerroristSuicideWin", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Terrorist])
                .SetGameMode(CustomGameMode.Standard);
            //50220~50223を使用
            TerroristTasks = OverrideTasksData.Create(50220, TabGroup.NeutralRoles, CustomRoles.Terrorist);
            SetupLoversRoleOptionsToggle(50300);

            SchrodingerCat.SetupCustomOption();
            Egoist.SetupCustomOption();
            Executioner.SetupCustomOption();
            Jackal.SetupCustomOption();

            // Add-Ons
            LastImpostor.SetupCustomOption();
            Workhorse.SetupCustomOption();
            #endregion

            KillFlashDuration = FloatOptionItem.Create(90000, "KillFlashDuration", new(0.1f, 0.45f, 0.05f), 0.3f, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);

            // HideAndSeek
            SetupRoleOptions(100000, TabGroup.MainSettings, CustomRoles.HASFox, CustomGameMode.HideAndSeek);
            SetupRoleOptions(100100, TabGroup.MainSettings, CustomRoles.HASTroll, CustomGameMode.HideAndSeek);
            AllowCloseDoors = BooleanOptionItem.Create(101000, "AllowCloseDoors", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.HideAndSeek);
            KillDelay = FloatOptionItem.Create(101001, "HideAndSeekWaitingTime", new(0f, 180f, 5f), 10f, TabGroup.MainSettings, false)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.HideAndSeek);
            //IgnoreCosmetics = CustomOption.Create(101002, Color.white, "IgnoreCosmetics", false)
            //    .SetGameMode(CustomGameMode.HideAndSeek);
            IgnoreVent = BooleanOptionItem.Create(101003, "IgnoreVent", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.HideAndSeek);

            // リアクターの時間制御
            SabotageTimeControl = BooleanOptionItem.Create(100800, "SabotageTimeControl", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.Standard);
            PolusReactorTimeLimit = FloatOptionItem.Create(100801, "PolusReactorTimeLimit", new(1f, 60f, 1f), 30f, TabGroup.MainSettings, false).SetParent(SabotageTimeControl)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);
            AirshipReactorTimeLimit = FloatOptionItem.Create(100802, "AirshipReactorTimeLimit", new(1f, 90f, 1f), 60f, TabGroup.MainSettings, false).SetParent(SabotageTimeControl)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);

            // 停電の特殊設定
            LightsOutSpecialSettings = BooleanOptionItem.Create(101500, "LightsOutSpecialSettings", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipViewingDeckLightsPanel = BooleanOptionItem.Create(101511, "DisableAirshipViewingDeckLightsPanel", false, TabGroup.MainSettings, false).SetParent(LightsOutSpecialSettings)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipGapRoomLightsPanel = BooleanOptionItem.Create(101512, "DisableAirshipGapRoomLightsPanel", false, TabGroup.MainSettings, false).SetParent(LightsOutSpecialSettings)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipCargoLightsPanel = BooleanOptionItem.Create(101513, "DisableAirshipCargoLightsPanel", false, TabGroup.MainSettings, false).SetParent(LightsOutSpecialSettings)
                .SetGameMode(CustomGameMode.Standard);

            // タスク無効化
            DisableTasks = BooleanOptionItem.Create(100300, "DisableTasks", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All);
            DisableSwipeCard = BooleanOptionItem.Create(100301, "DisableSwipeCardTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableSubmitScan = BooleanOptionItem.Create(100302, "DisableSubmitScanTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableUnlockSafe = BooleanOptionItem.Create(100303, "DisableUnlockSafeTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableUploadData = BooleanOptionItem.Create(100304, "DisableUploadDataTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableStartReactor = BooleanOptionItem.Create(100305, "DisableStartReactorTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableResetBreaker = BooleanOptionItem.Create(100306, "DisableResetBreakerTask", false, TabGroup.MainSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);

            //デバイス無効化
            DisableDevices = BooleanOptionItem.Create(101200, "DisableDevices", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.Standard);
            DisableSkeldDevices = BooleanOptionItem.Create(101210, "DisableSkeldDevices", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableSkeldAdmin = BooleanOptionItem.Create(101211, "DisableSkeldAdmin", false, TabGroup.MainSettings, false).SetParent(DisableSkeldDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableSkeldCamera = BooleanOptionItem.Create(101212, "DisableSkeldCamera", false, TabGroup.MainSettings, false).SetParent(DisableSkeldDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableMiraHQDevices = BooleanOptionItem.Create(101220, "DisableMiraHQDevices", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableMiraHQAdmin = BooleanOptionItem.Create(101221, "DisableMiraHQAdmin", false, TabGroup.MainSettings, false).SetParent(DisableMiraHQDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableMiraHQDoorLog = BooleanOptionItem.Create(101222, "DisableMiraHQDoorLog", false, TabGroup.MainSettings, false).SetParent(DisableMiraHQDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisablePolusDevices = BooleanOptionItem.Create(101230, "DisablePolusDevices", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisablePolusAdmin = BooleanOptionItem.Create(101231, "DisablePolusAdmin", false, TabGroup.MainSettings, false).SetParent(DisablePolusDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisablePolusCamera = BooleanOptionItem.Create(101232, "DisablePolusCamera", false, TabGroup.MainSettings, false).SetParent(DisablePolusDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisablePolusVital = BooleanOptionItem.Create(101233, "DisablePolusVital", false, TabGroup.MainSettings, false).SetParent(DisablePolusDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipDevices = BooleanOptionItem.Create(101240, "DisableAirshipDevices", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipCockpitAdmin = BooleanOptionItem.Create(101241, "DisableAirshipCockpitAdmin", false, TabGroup.MainSettings, false).SetParent(DisableAirshipDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipRecordsAdmin = BooleanOptionItem.Create(101242, "DisableAirshipRecordsAdmin", false, TabGroup.MainSettings, false).SetParent(DisableAirshipDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipCamera = BooleanOptionItem.Create(101243, "DisableAirshipCamera", false, TabGroup.MainSettings, false).SetParent(DisableAirshipDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipVital = BooleanOptionItem.Create(101244, "DisableAirshipVital", false, TabGroup.MainSettings, false).SetParent(DisableAirshipDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableDevicesIgnoreConditions = BooleanOptionItem.Create(101290, "IgnoreConditions", false, TabGroup.MainSettings, false).SetParent(DisableDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableDevicesIgnoreImpostors = BooleanOptionItem.Create(101291, "IgnoreImpostors", false, TabGroup.MainSettings, false).SetParent(DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.Standard);
            DisableDevicesIgnoreMadmates = BooleanOptionItem.Create(101292, "IgnoreMadmates", false, TabGroup.MainSettings, false).SetParent(DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.Standard);
            DisableDevicesIgnoreNeutrals = BooleanOptionItem.Create(101293, "IgnoreNeutrals", false, TabGroup.MainSettings, false).SetParent(DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.Standard);
            DisableDevicesIgnoreCrewmates = BooleanOptionItem.Create(101294, "IgnoreCrewmates", false, TabGroup.MainSettings, false).SetParent(DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.Standard);
            DisableDevicesIgnoreAfterAnyoneDied = BooleanOptionItem.Create(101295, "IgnoreAfterAnyoneDied", false, TabGroup.MainSettings, false).SetParent(DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.Standard);

            // ランダムマップ
            RandomMapsMode = BooleanOptionItem.Create(100400, "RandomMapsMode", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All);
            AddedTheSkeld = BooleanOptionItem.Create(100401, "AddedTheSkeld", false, TabGroup.MainSettings, false).SetParent(RandomMapsMode)
                .SetGameMode(CustomGameMode.All);
            AddedMiraHQ = BooleanOptionItem.Create(100402, "AddedMIRAHQ", false, TabGroup.MainSettings, false).SetParent(RandomMapsMode)
                .SetGameMode(CustomGameMode.All);
            AddedPolus = BooleanOptionItem.Create(100403, "AddedPolus", false, TabGroup.MainSettings, false).SetParent(RandomMapsMode)
                .SetGameMode(CustomGameMode.All);
            AddedTheAirShip = BooleanOptionItem.Create(100404, "AddedTheAirShip", false, TabGroup.MainSettings, false).SetParent(RandomMapsMode)
                .SetGameMode(CustomGameMode.All);
            // MapDleks = CustomOption.Create(100405, Color.white, "AddedDleks", false, RandomMapMode)
            //     .SetGameMode(CustomGameMode.All);

            // ランダムスポーン
            RandomSpawn = BooleanOptionItem.Create(101300, "RandomSpawn", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All);
            AirshipAdditionalSpawn = BooleanOptionItem.Create(101301, "AirshipAdditionalSpawn", false, TabGroup.MainSettings, false).SetParent(RandomSpawn)
                .SetGameMode(CustomGameMode.All);

            // ボタン回数同期
            SyncButtonMode = BooleanOptionItem.Create(100200, "SyncButtonMode", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.Standard);
            SyncedButtonCount = IntegerOptionItem.Create(100201, "SyncedButtonCount", new(0, 100, 1), 10, TabGroup.MainSettings, false).SetParent(SyncButtonMode)
                .SetValueFormat(OptionFormat.Times)
                .SetGameMode(CustomGameMode.Standard);

            // 投票モード
            VoteMode = BooleanOptionItem.Create(100500, "VoteMode", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.Standard);
            WhenSkipVote = StringOptionItem.Create(100510, "WhenSkipVote", voteModes[0..3], 0, TabGroup.MainSettings, false).SetParent(VoteMode)
                .SetGameMode(CustomGameMode.Standard);
            WhenSkipVoteIgnoreFirstMeeting = BooleanOptionItem.Create(100511, "WhenSkipVoteIgnoreFirstMeeting", false, TabGroup.MainSettings, false).SetParent(WhenSkipVote)
                .SetGameMode(CustomGameMode.Standard);
            WhenSkipVoteIgnoreNoDeadBody = BooleanOptionItem.Create(100512, "WhenSkipVoteIgnoreNoDeadBody", false, TabGroup.MainSettings, false).SetParent(WhenSkipVote)
                .SetGameMode(CustomGameMode.Standard);
            WhenSkipVoteIgnoreEmergency = BooleanOptionItem.Create(100513, "WhenSkipVoteIgnoreEmergency", false, TabGroup.MainSettings, false).SetParent(WhenSkipVote)
                .SetGameMode(CustomGameMode.Standard);
            WhenNonVote = StringOptionItem.Create(100520, "WhenNonVote", voteModes, 0, TabGroup.MainSettings, false).SetParent(VoteMode)
                .SetGameMode(CustomGameMode.Standard);
            WhenTie = StringOptionItem.Create(100530, "WhenTie", tieModes, 0, TabGroup.MainSettings, false).SetParent(VoteMode)
                .SetGameMode(CustomGameMode.Standard);

            // 全員生存時の会議時間
            AllAliveMeeting = BooleanOptionItem.Create(100900, "AllAliveMeeting", false, TabGroup.MainSettings, false);
            AllAliveMeetingTime = FloatOptionItem.Create(100901, "AllAliveMeetingTime", new(1f, 300f, 1f), 10f, TabGroup.MainSettings, false).SetParent(AllAliveMeeting)
                .SetValueFormat(OptionFormat.Seconds);

            // 生存人数ごとの緊急会議
            AdditionalEmergencyCooldown = BooleanOptionItem.Create(101400, "AdditionalEmergencyCooldown", false, TabGroup.MainSettings, false);
            AdditionalEmergencyCooldownThreshold = IntegerOptionItem.Create(101401, "AdditionalEmergencyCooldownThreshold", new(1, 15, 1), 1, TabGroup.MainSettings, false).SetParent(AdditionalEmergencyCooldown)
                .SetValueFormat(OptionFormat.Players);
            AdditionalEmergencyCooldownTime = FloatOptionItem.Create(101402, "AdditionalEmergencyCooldownTime", new(1f, 60f, 1f), 1f, TabGroup.MainSettings, false).SetParent(AdditionalEmergencyCooldown)
                .SetValueFormat(OptionFormat.Seconds);

            // 転落死
            LadderDeath = BooleanOptionItem.Create(101100, "LadderDeath", false, TabGroup.MainSettings, false)
                .SetHeader(true);
            LadderDeathChance = StringOptionItem.Create(101110, "LadderDeathChance", rates[1..], 0, TabGroup.MainSettings, false).SetParent(LadderDeath);

            //エレキ構造変化
            AirShipVariableElectrical = BooleanOptionItem.Create(101600, "AirShipVariableElectrical", false, TabGroup.MainSettings, false)
                .SetHeader(true);

            // 通常モードでかくれんぼ用
            StandardHAS = BooleanOptionItem.Create(100700, "StandardHAS", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.Standard);
            StandardHASWaitingTime = FloatOptionItem.Create(100701, "StandardHASWaitingTime", new(0f, 180f, 2.5f), 10f, TabGroup.MainSettings, false).SetParent(StandardHAS)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);

            // その他
            FixFirstKillCooldown = BooleanOptionItem.Create(900_000, "FixFirstKillCooldown", false, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All);
            DisableTaskWin = BooleanOptionItem.Create(900_001, "DisableTaskWin", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All);
            NoGameEnd = BooleanOptionItem.Create(900_002, "NoGameEnd", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All);
            GhostCanSeeOtherRoles = BooleanOptionItem.Create(900_010, "GhostCanSeeOtherRoles", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All);
            GhostCanSeeOtherVotes = BooleanOptionItem.Create(900_011, "GhostCanSeeOtherVotes", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All);
            GhostCanSeeDeathReason = BooleanOptionItem.Create(900_014, "GhostCanSeeDeathReason", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All);
            GhostIgnoreTasks = BooleanOptionItem.Create(900_012, "GhostIgnoreTasks", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All);
            CommsCamouflage = BooleanOptionItem.Create(900_013, "CommsCamouflage", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All);

            // プリセット対象外
            AutoDisplayLastResult = BooleanOptionItem.Create(1_000_000, "AutoDisplayLastResult", true, TabGroup.MainSettings, false)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All);
            AutoDisplayKillLog = BooleanOptionItem.Create(1_000_006, "AutoDisplayKillLog", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All);
            SuffixMode = StringOptionItem.Create(1_000_001, "SuffixMode", suffixModes, 0, TabGroup.MainSettings, true)
                .SetGameMode(CustomGameMode.All);
            HideGameSettings = BooleanOptionItem.Create(1_000_002, "HideGameSettings", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All);
            ColorNameMode = BooleanOptionItem.Create(1_000_003, "ColorNameMode", false, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All);
            ChangeNameToRoleInfo = BooleanOptionItem.Create(1_000_004, "ChangeNameToRoleInfo", true, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All);
            RoleAssigningAlgorithm = StringOptionItem.Create(1_000_005, "RoleAssigningAlgorithm", RoleAssigningAlgorithms, 0, TabGroup.MainSettings, true)
                .SetGameMode(CustomGameMode.All)
                .RegisterUpdateValueEvent(
                    (object obj, OptionItem.UpdateValueEventArgs args) => IRandom.SetInstanceById(args.CurrentValue)
                );

            ApplyDenyNameList = BooleanOptionItem.Create(1_000_100, "ApplyDenyNameList", true, TabGroup.MainSettings, true)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All);
            KickPlayerFriendCodeNotExist = BooleanOptionItem.Create(1_000_101, "KickPlayerFriendCodeNotExist", false, TabGroup.MainSettings, true)
                .SetGameMode(CustomGameMode.All);
            ApplyBanList = BooleanOptionItem.Create(1_000_110, "ApplyBanList", true, TabGroup.MainSettings, true)
                .SetGameMode(CustomGameMode.All);

            DebugModeManager.SetupCustomOption();

            IsLoaded = true;
        }

        public static void SetupRoleOptions(int id, TabGroup tab, CustomRoles role, CustomGameMode customGameMode = CustomGameMode.Standard)
        {
            var spawnOption = StringOptionItem.Create(id, role.ToString(), ratesZeroOne, 0, tab, false).SetColor(Utils.GetRoleColor(role))
                .SetHeader(true)
                .SetGameMode(customGameMode) as StringOptionItem;
            var countOption = IntegerOptionItem.Create(id + 1, "Maximum", new(1, 15, 1), 1, tab, false).SetParent(spawnOption)
                .SetValueFormat(OptionFormat.Players)
                .SetGameMode(customGameMode);

            CustomRoleSpawnChances.Add(role, spawnOption);
            CustomRoleCounts.Add(role, countOption);
        }
        private static void SetupLoversRoleOptionsToggle(int id, CustomGameMode customGameMode = CustomGameMode.Standard)
        {
            var role = CustomRoles.Lovers;
            var spawnOption = StringOptionItem.Create(id, role.ToString(), ratesZeroOne, 0, TabGroup.Addons, false).SetColor(Utils.GetRoleColor(role))
                .SetHeader(true)
                .SetGameMode(customGameMode) as StringOptionItem;

            var countOption = IntegerOptionItem.Create(id + 1, "NumberOfLovers", new(2, 2, 1), 2, TabGroup.Addons, false).SetParent(spawnOption)
                .SetHidden(true)
                .SetGameMode(customGameMode);

            CustomRoleSpawnChances.Add(role, spawnOption);
            CustomRoleCounts.Add(role, countOption);
        }
        public static void SetupSingleRoleOptions(int id, TabGroup tab, CustomRoles role, int count, CustomGameMode customGameMode = CustomGameMode.Standard)
        {
            var spawnOption = StringOptionItem.Create(id, role.ToString(), ratesZeroOne, 0, tab, false).SetColor(Utils.GetRoleColor(role))
                .SetHeader(true)
                .SetGameMode(customGameMode) as StringOptionItem;
            // 初期値,最大値,最小値が同じで、stepが0のどうやっても変えることができない個数オプション
            var countOption = IntegerOptionItem.Create(id + 1, "Maximum", new(count, count, count), count, tab, false).SetParent(spawnOption)
                .SetGameMode(customGameMode);

            CustomRoleSpawnChances.Add(role, spawnOption);
            CustomRoleCounts.Add(role, countOption);
        }
        public class OverrideTasksData
        {
            public static Dictionary<CustomRoles, OverrideTasksData> AllData = new();
            public CustomRoles Role { get; private set; }
            public int IdStart { get; private set; }
            public OptionItem doOverride;
            public OptionItem assignCommonTasks;
            public OptionItem numLongTasks;
            public OptionItem numShortTasks;

            public OverrideTasksData(int idStart, TabGroup tab, CustomRoles role)
            {
                this.IdStart = idStart;
                this.Role = role;
                Dictionary<string, string> replacementDic = new() { { "%role%", Utils.GetRoleName(role) } };
                doOverride = BooleanOptionItem.Create(idStart++, "doOverride", false, tab, false).SetParent(CustomRoleSpawnChances[role])
                    .SetValueFormat(OptionFormat.None);
                doOverride.ReplacementDictionary = replacementDic;
                assignCommonTasks = BooleanOptionItem.Create(idStart++, "assignCommonTasks", true, tab, false).SetParent(doOverride)
                    .SetValueFormat(OptionFormat.None);
                assignCommonTasks.ReplacementDictionary = replacementDic;
                numLongTasks = IntegerOptionItem.Create(idStart++, "roleLongTasksNum", new(0, 99, 1), 3, tab, false).SetParent(doOverride)
                    .SetValueFormat(OptionFormat.Pieces);
                numLongTasks.ReplacementDictionary = replacementDic;
                numShortTasks = IntegerOptionItem.Create(idStart++, "roleShortTasksNum", new(0, 99, 1), 3, tab, false).SetParent(doOverride)
                    .SetValueFormat(OptionFormat.Pieces);
                numShortTasks.ReplacementDictionary = replacementDic;

                if (!AllData.ContainsKey(role)) AllData.Add(role, this);
                else Logger.Warn("重複したCustomRolesを対象とするOverrideTasksDataが作成されました", "OverrideTasksData");
            }
            public static OverrideTasksData Create(int idStart, TabGroup tab, CustomRoles role)
            {
                return new OverrideTasksData(idStart, tab, role);
            }
        }
    }
}