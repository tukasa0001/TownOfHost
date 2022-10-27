using System;
using System.Collections.Generic;
using System.Linq;
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

    public static class Options
    {
        // オプションId
        public const int PresetId = 0;

        // プリセット
        private static readonly string[] presets =
        {
            Main.Preset1.Value, Main.Preset2.Value, Main.Preset3.Value,
            Main.Preset4.Value, Main.Preset5.Value
        };

        // ゲームモード
        public static CustomOption GameMode;
        public static CustomGameMode CurrentGameMode
            => GameMode.Selection == 0 ? CustomGameMode.Standard : CustomGameMode.HideAndSeek;

        public static readonly string[] gameModes =
        {
            "Standard", "HideAndSeek",
        };

        // MapActive
        public static bool IsActiveSkeld => AddedTheSkeld.GetBool() || PlayerControl.GameOptions.MapId == 0;
        public static bool IsActiveMiraHQ => AddedMiraHQ.GetBool() || PlayerControl.GameOptions.MapId == 1;
        public static bool IsActivePolus => AddedPolus.GetBool() || PlayerControl.GameOptions.MapId == 2;
        public static bool IsActiveAirship => AddedTheAirShip.GetBool() || PlayerControl.GameOptions.MapId == 4;

        // 役職数・確率
        public static Dictionary<CustomRoles, int> roleCounts;
        public static Dictionary<CustomRoles, float> roleSpawnChances;
        public static Dictionary<CustomRoles, CustomOption> CustomRoleCounts;
        public static Dictionary<CustomRoles, CustomOption> CustomRoleSpawnChances;
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
        public static CustomOption EnableGM;
        public static CustomOption EnableLastImpostor;
        public static CustomOption LastImpostorKillCooldown;
        public static float DefaultKillCooldown = PlayerControl.GameOptions.KillCooldown;
        public static CustomOption VampireKillDelay;
        //public static CustomOption ShapeMasterShapeshiftDuration;
        public static CustomOption DefaultShapeshiftCooldown;
        public static CustomOption CanMakeMadmateCount;
        public static CustomOption MadGuardianCanSeeWhoTriedToKill;
        public static CustomOption MadSnitchCanVent;
        public static CustomOption MadSnitchCanAlsoBeExposedToImpostor;
        public static CustomOption MadmateCanFixLightsOut; // TODO:mii-47 マッド役職統一
        public static CustomOption MadmateCanFixComms;
        public static CustomOption MadmateHasImpostorVision;
        public static CustomOption MadmateCanSeeKillFlash;
        public static CustomOption MadmateCanSeeOtherVotes;
        public static CustomOption MadmateCanSeeDeathReason;
        public static CustomOption MadmateVentCooldown;
        public static CustomOption MadmateVentMaxTime;

        public static CustomOption EvilWatcherChance;
        public static CustomOption LighterTaskCompletedVision;
        public static CustomOption LighterTaskCompletedDisableLightOut;
        public static CustomOption MayorAdditionalVote;
        public static CustomOption MayorHasPortableButton;
        public static CustomOption MayorNumOfUseButton;
        public static CustomOption DoctorTaskCompletedBatteryCharge;
        public static CustomOption SnitchEnableTargetArrow;
        public static CustomOption SnitchCanGetArrowColor;
        public static CustomOption SnitchCanFindNeutralKiller;
        public static CustomOption SpeedBoosterUpSpeed; //加速値
        public static CustomOption SpeedBoosterTaskTrigger; //効果を発動するタスク完了数
        public static CustomOption TrapperBlockMoveTime;
        public static CustomOption CanTerroristSuicideWin;
        public static CustomOption ArsonistDouseTime;
        public static CustomOption ArsonistCooldown;
        public static CustomOption KillFlashDuration;

        // HideAndSeek
        public static CustomOption AllowCloseDoors;
        public static CustomOption KillDelay;
        public static CustomOption IgnoreCosmetics;
        public static CustomOption IgnoreVent;
        public static float HideAndSeekKillDelayTimer = 0f;

        // タスク無効化
        public static CustomOption DisableTasks;
        public static CustomOption DisableSwipeCard;
        public static CustomOption DisableSubmitScan;
        public static CustomOption DisableUnlockSafe;
        public static CustomOption DisableUploadData;
        public static CustomOption DisableStartReactor;
        public static CustomOption DisableResetBreaker;

        //デバイスブロック
        public static CustomOption DisableDevices;
        public static CustomOption DisableSkeldDevices;
        public static CustomOption DisableSkeldAdmin;
        public static CustomOption DisableSkeldCamera;
        public static CustomOption DisableMiraHQDevices;
        public static CustomOption DisableMiraHQAdmin;
        public static CustomOption DisableMiraHQDoorLog;
        public static CustomOption DisablePolusDevices;
        public static CustomOption DisablePolusAdmin;
        public static CustomOption DisablePolusCamera;
        public static CustomOption DisablePolusVital;
        public static CustomOption DisableAirshipDevices;
        public static CustomOption DisableAirshipCockpitAdmin;
        public static CustomOption DisableAirshipRecordsAdmin;
        public static CustomOption DisableAirshipCamera;
        public static CustomOption DisableAirshipVital;
        public static CustomOption DisableDevicesIgnoreConditions;
        public static CustomOption DisableDevicesIgnoreImpostors;
        public static CustomOption DisableDevicesIgnoreMadmates;
        public static CustomOption DisableDevicesIgnoreNeutrals;
        public static CustomOption DisableDevicesIgnoreCrewmates;
        public static CustomOption DisableDevicesIgnoreAfterAnyoneDied;

        // ランダムマップ
        public static CustomOption RandomMapsMode;
        public static CustomOption AddedTheSkeld;
        public static CustomOption AddedMiraHQ;
        public static CustomOption AddedPolus;
        public static CustomOption AddedTheAirShip;
        public static CustomOption AddedDleks;

        // ランダムスポーン
        public static CustomOption RandomSpawn;
        public static CustomOption AirshipAdditionalSpawn;

        // 投票モード
        public static CustomOption VoteMode;
        public static CustomOption WhenSkipVote;
        public static CustomOption WhenSkipVoteIgnoreFirstMeeting;
        public static CustomOption WhenSkipVoteIgnoreNoDeadBody;
        public static CustomOption WhenSkipVoteIgnoreEmergency;
        public static CustomOption WhenNonVote;
        public static CustomOption WhenTie;
        public static readonly string[] voteModes =
        {
            "Default", "Suicide", "SelfVote", "Skip"
        };
        public static readonly string[] tieModes =
        {
            "TieMode.Default", "TieMode.All", "TieMode.Random"
        };
        public static VoteMode GetWhenSkipVote() => (VoteMode)WhenSkipVote.GetSelection();
        public static VoteMode GetWhenNonVote() => (VoteMode)WhenNonVote.GetSelection();

        // ボタン回数
        public static CustomOption SyncButtonMode;
        public static CustomOption SyncedButtonCount;
        public static int UsedButtonCount = 0;

        // 全員生存時の会議時間
        public static CustomOption AllAliveMeeting;
        public static CustomOption AllAliveMeetingTime;

        // 追加の緊急ボタンクールダウン
        public static CustomOption AdditionalEmergencyCooldown;
        public static CustomOption AdditionalEmergencyCooldownThreshold;
        public static CustomOption AdditionalEmergencyCooldownTime;

        //転落死
        public static CustomOption LadderDeath;
        public static CustomOption LadderDeathChance;

        // 通常モードでかくれんぼ
        public static bool IsStandardHAS => StandardHAS.GetBool() && CurrentGameMode == CustomGameMode.Standard;
        public static CustomOption StandardHAS;
        public static CustomOption StandardHASWaitingTime;

        // リアクターの時間制御
        public static CustomOption SabotageTimeControl;
        public static CustomOption PolusReactorTimeLimit;
        public static CustomOption AirshipReactorTimeLimit;

        // 停電の特殊設定
        public static CustomOption LightsOutSpecialSettings;
        public static CustomOption DisableAirshipViewingDeckLightsPanel;
        public static CustomOption DisableAirshipGapRoomLightsPanel;
        public static CustomOption DisableAirshipCargoLightsPanel;

        // タスク上書き
        public static OverrideTasksData MadGuardianTasks;
        public static OverrideTasksData TerroristTasks;
        public static OverrideTasksData SnitchTasks;
        public static OverrideTasksData MadSnitchTasks;

        // その他
        public static CustomOption FixFirstKillCooldown;
        public static CustomOption DisableTaskWin;
        public static CustomOption GhostCanSeeOtherRoles;
        public static CustomOption GhostCanSeeOtherVotes;
        public static CustomOption GhostCanSeeDeathReason;
        public static CustomOption GhostIgnoreTasks;
        public static CustomOption CommsCamouflage;

        // プリセット対象外
        public static CustomOption NoGameEnd;
        public static CustomOption AutoDisplayLastResult;
        public static CustomOption SuffixMode;
        public static CustomOption HideGameSettings;
        public static CustomOption ColorNameMode;

        public static readonly string[] suffixModes =
        {
            "SuffixMode.None",
            "SuffixMode.Version",
            "SuffixMode.Streaming",
            "SuffixMode.Recording",
            "SuffixMode.RoomHost",
            "SuffixMode.OriginalName"
        };
        public static SuffixModes GetSuffixMode()
        {
            return (SuffixModes)SuffixMode.GetSelection();
        }



        public static int SnitchExposeTaskLeft = 1;


        public static bool IsEvilWatcher = false;
        public static void SetWatcherTeam(float EvilWatcherRate)
        {
            EvilWatcherRate = Options.EvilWatcherChance.GetFloat();
            IsEvilWatcher = UnityEngine.Random.Range(1, 100) < EvilWatcherRate;
        }
        private static bool IsLoaded = false;

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
                option.UpdateSelection(count - 1);
            }
        }

        public static int GetRoleCount(CustomRoles role)
        {
            var chance = CustomRoleSpawnChances.TryGetValue(role, out var sc) ? sc.GetChance() : 0;
            return chance == 0 ? 0 : CustomRoleCounts.TryGetValue(role, out var option) ? option.GetInt() : roleCounts[role];
        }

        public static float GetRoleChance(CustomRoles role)
        {
            return CustomRoleSpawnChances.TryGetValue(role, out var option) ? option.GetSelection()/* / 10f */ : roleSpawnChances[role];
        }
        public static void Load()
        {
            if (IsLoaded) return;
            // プリセット
            _ = CustomOption.Create(0, TabGroup.MainSettings, new Color32(204, 204, 0, 255), "Preset", presets, presets[0], null, true)
                .HiddenOnDisplay(true)
                .SetGameMode(CustomGameMode.All);

            // ゲームモード
            GameMode = CustomOption.Create(1, TabGroup.MainSettings, new Color32(204, 204, 0, 255), "GameMode", gameModes, gameModes[0], null, true)
                .SetGameMode(CustomGameMode.All);

            #region 役職・詳細設定
            CustomRoleCounts = new Dictionary<CustomRoles, CustomOption>();
            CustomRoleSpawnChances = new Dictionary<CustomRoles, CustomOption>();
            // GM
            EnableGM = CustomOption.Create(100, TabGroup.MainSettings, Utils.GetRoleColor(CustomRoles.GM), "GM", false, null, true)
                .SetGameMode(CustomGameMode.Standard);

            // Impostor
            BountyHunter.SetupCustomOption();
            SerialKiller.SetupCustomOption();
            // SetupRoleOptions(1200, CustomRoles.ShapeMaster);
            // ShapeMasterShapeshiftDuration = CustomOption.Create(1210, Color.white, "ShapeMasterShapeshiftDuration", 10, 1, 1000, 1, CustomRoleSpawnChances[CustomRoles.ShapeMaster]);
            SetupRoleOptions(1300, TabGroup.ImpostorRoles, CustomRoles.Vampire);
            VampireKillDelay = CustomOption.Create(1310, TabGroup.ImpostorRoles, Color.white, "VampireKillDelay", 10, 1, 1000, 1, CustomRoleSpawnChances[CustomRoles.Vampire], format: "Seconds");
            SetupRoleOptions(1400, TabGroup.ImpostorRoles, CustomRoles.Warlock);
            SetupRoleOptions(1500, TabGroup.ImpostorRoles, CustomRoles.Witch);
            SetupRoleOptions(1600, TabGroup.ImpostorRoles, CustomRoles.Mafia);
            FireWorks.SetupCustomOption();
            Sniper.SetupCustomOption();
            SetupRoleOptions(2000, TabGroup.ImpostorRoles, CustomRoles.Puppeteer);
            Mare.SetupCustomOption();
            TimeThief.SetupCustomOption();
            EvilTracker.SetupCustomOption();

            DefaultShapeshiftCooldown = CustomOption.Create(5011, TabGroup.ImpostorRoles, Color.white, "DefaultShapeshiftCooldown", 15, 5, 999, 5, null, true, format: "Seconds");
            CanMakeMadmateCount = CustomOption.Create(5012, TabGroup.ImpostorRoles, Utils.GetRoleColor(CustomRoles.Madmate), "CanMakeMadmateCount", 0, 0, 15, 1, null, true, format: "Times");

            // Madmate
            SetupRoleOptions(10000, TabGroup.ImpostorRoles, CustomRoles.Madmate);
            SetupRoleOptions(10100, TabGroup.ImpostorRoles, CustomRoles.MadGuardian);
            MadGuardianCanSeeWhoTriedToKill = CustomOption.Create(10110, TabGroup.ImpostorRoles, Color.white, "MadGuardianCanSeeWhoTriedToKill", false, CustomRoleSpawnChances[CustomRoles.MadGuardian]);
            //ID10120~10123を使用
            MadGuardianTasks = OverrideTasksData.Create(10120, TabGroup.ImpostorRoles, CustomRoles.MadGuardian);
            SetupRoleOptions(10200, TabGroup.ImpostorRoles, CustomRoles.MadSnitch);
            MadSnitchCanVent = CustomOption.Create(10210, TabGroup.ImpostorRoles, Color.white, "CanVent", false, CustomRoleSpawnChances[CustomRoles.MadSnitch]);
            MadSnitchCanAlsoBeExposedToImpostor = CustomOption.Create(10211, TabGroup.ImpostorRoles, Color.white, "MadSnitchCanAlsoBeExposedToImpostor", false, CustomRoleSpawnChances[CustomRoles.MadSnitch]);
            //ID10220~10223を使用
            MadSnitchTasks = OverrideTasksData.Create(10220, TabGroup.ImpostorRoles, CustomRoles.MadSnitch);
            // Madmate Common Options
            MadmateCanFixLightsOut = CustomOption.Create(15010, TabGroup.ImpostorRoles, Color.white, "MadmateCanFixLightsOut", false, null, true, false);
            MadmateCanFixComms = CustomOption.Create(15011, TabGroup.ImpostorRoles, Color.white, "MadmateCanFixComms", false);
            MadmateHasImpostorVision = CustomOption.Create(15012, TabGroup.ImpostorRoles, Color.white, "MadmateHasImpostorVision", false);
            MadmateCanSeeKillFlash = CustomOption.Create(15015, TabGroup.ImpostorRoles, Color.white, "MadmateCanSeeKillFlash", false);
            MadmateCanSeeOtherVotes = CustomOption.Create(15016, TabGroup.ImpostorRoles, Color.white, "MadmateCanSeeOtherVotes", false);
            MadmateCanSeeDeathReason = CustomOption.Create(15018, TabGroup.ImpostorRoles, Color.white, "MadmateCanSeeDeathReason", false);
            MadmateVentCooldown = CustomOption.Create(15213, TabGroup.ImpostorRoles, Color.white, "MadmateVentCooldown", 0f, 0f, 180f, 5f, format: "Seconds");
            MadmateVentMaxTime = CustomOption.Create(15214, TabGroup.ImpostorRoles, Color.white, "MadmateVentMaxTime", 0f, 0f, 180f, 5f, format: "Seconds");
            // Both
            SetupRoleOptions(30000, TabGroup.NeutralRoles, CustomRoles.Watcher);
            EvilWatcherChance = CustomOption.Create(30010, TabGroup.NeutralRoles, Color.white, "EvilWatcherChance", 0, 0, 100, 10, CustomRoleSpawnChances[CustomRoles.Watcher], format: "Percent");
            // Crewmate
            SetupRoleOptions(20000, TabGroup.CrewmateRoles, CustomRoles.Bait);
            SetupRoleOptions(20100, TabGroup.CrewmateRoles, CustomRoles.Lighter);
            LighterTaskCompletedVision = CustomOption.Create(20110, TabGroup.CrewmateRoles, Color.white, "LighterTaskCompletedVision", 2f, 0f, 5f, 0.25f, CustomRoleSpawnChances[CustomRoles.Lighter], format: "Multiplier");
            LighterTaskCompletedDisableLightOut = CustomOption.Create(20111, TabGroup.CrewmateRoles, Color.white, "LighterTaskCompletedDisableLightOut", true, CustomRoleSpawnChances[CustomRoles.Lighter]);
            SetupRoleOptions(20200, TabGroup.CrewmateRoles, CustomRoles.Mayor);
            MayorAdditionalVote = CustomOption.Create(20210, TabGroup.CrewmateRoles, Color.white, "MayorAdditionalVote", 1, 1, 99, 1, CustomRoleSpawnChances[CustomRoles.Mayor], format: "Votes");
            MayorHasPortableButton = CustomOption.Create(20211, TabGroup.CrewmateRoles, Color.white, "MayorHasPortableButton", false, CustomRoleSpawnChances[CustomRoles.Mayor]);
            MayorNumOfUseButton = CustomOption.Create(20212, TabGroup.CrewmateRoles, Color.white, "MayorNumOfUseButton", 1, 1, 99, 1, MayorHasPortableButton, format: "Times");
            SabotageMaster.SetupCustomOption();
            Sheriff.SetupCustomOption();
            SetupRoleOptions(20500, TabGroup.CrewmateRoles, CustomRoles.Snitch);
            SnitchEnableTargetArrow = CustomOption.Create(20510, TabGroup.CrewmateRoles, Color.white, "SnitchEnableTargetArrow", false, CustomRoleSpawnChances[CustomRoles.Snitch]);
            SnitchCanGetArrowColor = CustomOption.Create(20511, TabGroup.CrewmateRoles, Color.white, "SnitchCanGetArrowColor", false, CustomRoleSpawnChances[CustomRoles.Snitch]);
            SnitchCanFindNeutralKiller = CustomOption.Create(20512, TabGroup.CrewmateRoles, Color.white, "SnitchCanFindNeutralKiller", false, CustomRoleSpawnChances[CustomRoles.Snitch]);
            //20520~20523を使用
            SnitchTasks = OverrideTasksData.Create(20520, TabGroup.CrewmateRoles, CustomRoles.Snitch);
            SetupRoleOptions(20600, TabGroup.CrewmateRoles, CustomRoles.SpeedBooster);
            SpeedBoosterUpSpeed = CustomOption.Create(20610, TabGroup.CrewmateRoles, Color.white, "SpeedBoosterUpSpeed", 0.3f, 0.1f, 0.5f, 0.1f, CustomRoleSpawnChances[CustomRoles.SpeedBooster], format: "Multiplier");
            SpeedBoosterTaskTrigger = CustomOption.Create(20611, TabGroup.CrewmateRoles, Color.white, "SpeedBoosterTaskTrigger", 5f, 1f, 99f, 1f, CustomRoleSpawnChances[CustomRoles.SpeedBooster], format: "Pieces");
            SetupRoleOptions(20700, TabGroup.CrewmateRoles, CustomRoles.Doctor);
            DoctorTaskCompletedBatteryCharge = CustomOption.Create(20710, TabGroup.CrewmateRoles, Color.white, "DoctorTaskCompletedBatteryCharge", 5, 0, 10, 1, CustomRoleSpawnChances[CustomRoles.Doctor], format: "Seconds");
            SetupRoleOptions(20800, TabGroup.CrewmateRoles, CustomRoles.Trapper);
            TrapperBlockMoveTime = CustomOption.Create(20810, TabGroup.CrewmateRoles, Color.white, "TrapperBlockMoveTime", 5f, 1f, 180, 1, CustomRoleSpawnChances[CustomRoles.Trapper], format: "Seconds");
            SetupRoleOptions(20900, TabGroup.CrewmateRoles, CustomRoles.Dictator);
            SetupRoleOptions(21000, TabGroup.CrewmateRoles, CustomRoles.Seer);

            // Neutral
            SetupRoleOptions(50500, TabGroup.NeutralRoles, CustomRoles.Arsonist);
            ArsonistDouseTime = CustomOption.Create(50510, TabGroup.NeutralRoles, Color.white, "ArsonistDouseTime", 3, 1, 10, 1, CustomRoleSpawnChances[CustomRoles.Arsonist], format: "Seconds");
            ArsonistCooldown = CustomOption.Create(50511, TabGroup.NeutralRoles, Color.white, "Cooldown", 10, 5, 100, 1, CustomRoleSpawnChances[CustomRoles.Arsonist], format: "Seconds");
            SetupRoleOptions(50000, TabGroup.NeutralRoles, CustomRoles.Jester);
            SetupRoleOptions(50100, TabGroup.NeutralRoles, CustomRoles.Opportunist);
            SetupRoleOptions(50200, TabGroup.NeutralRoles, CustomRoles.Terrorist);
            CanTerroristSuicideWin = CustomOption.Create(50210, TabGroup.NeutralRoles, Color.white, "CanTerroristSuicideWin", false, CustomRoleSpawnChances[CustomRoles.Terrorist], false)
                .SetGameMode(CustomGameMode.Standard);
            //50220~50223を使用
            TerroristTasks = OverrideTasksData.Create(50220, TabGroup.NeutralRoles, CustomRoles.Terrorist);
            SetupLoversRoleOptionsToggle(50300);

            SchrodingerCat.SetupCustomOption();
            Egoist.SetupCustomOption();
            Executioner.SetupCustomOption();
            Jackal.SetupCustomOption();

            // Add-Ons
            EnableLastImpostor = CustomOption.Create(80000, TabGroup.Addons, Utils.GetRoleColor(CustomRoles.Impostor), "LastImpostor", false, null, true)
                .SetGameMode(CustomGameMode.Standard);
            LastImpostorKillCooldown = CustomOption.Create(80010, TabGroup.Addons, Color.white, "KillCooldown", 15, 0, 180, 1, EnableLastImpostor, format: "Seconds")
                .SetGameMode(CustomGameMode.Standard);
            #endregion

            KillFlashDuration = CustomOption.Create(90000, TabGroup.MainSettings, Color.white, "KillFlashDuration", 0.3f, 0.1f, 0.45f, 0.05f, null, true, format: "Seconds")
                .SetGameMode(CustomGameMode.Standard);

            // HideAndSeek
            SetupRoleOptions(100000, TabGroup.MainSettings, CustomRoles.HASFox, CustomGameMode.HideAndSeek);
            SetupRoleOptions(100100, TabGroup.MainSettings, CustomRoles.HASTroll, CustomGameMode.HideAndSeek);
            AllowCloseDoors = CustomOption.Create(101000, TabGroup.MainSettings, Color.white, "AllowCloseDoors", false, null, true)
                .SetGameMode(CustomGameMode.HideAndSeek);
            KillDelay = CustomOption.Create(101001, TabGroup.MainSettings, Color.white, "HideAndSeekWaitingTime", 10, 0, 180, 5, format: "Seconds")
                .SetGameMode(CustomGameMode.HideAndSeek);
            //IgnoreCosmetics = CustomOption.Create(101002, Color.white, "IgnoreCosmetics", false)
            //    .SetGameMode(CustomGameMode.HideAndSeek);
            IgnoreVent = CustomOption.Create(101003, TabGroup.MainSettings, Color.white, "IgnoreVent", false)
                .SetGameMode(CustomGameMode.HideAndSeek);

            // リアクターの時間制御
            SabotageTimeControl = CustomOption.Create(100800, TabGroup.MainSettings, Color.white, "SabotageTimeControl", false, null, true)
                .SetGameMode(CustomGameMode.Standard);
            PolusReactorTimeLimit = CustomOption.Create(100801, TabGroup.MainSettings, Color.white, "PolusReactorTimeLimit", 30, 1, 60, 1, SabotageTimeControl, format: "Seconds")
                .SetGameMode(CustomGameMode.Standard);
            AirshipReactorTimeLimit = CustomOption.Create(100802, TabGroup.MainSettings, Color.white, "AirshipReactorTimeLimit", 60, 1, 90, 1, SabotageTimeControl, format: "Seconds")
                .SetGameMode(CustomGameMode.Standard);

            // 停電の特殊設定
            LightsOutSpecialSettings = CustomOption.Create(101500, TabGroup.MainSettings, Color.white, "LightsOutSpecialSettings", false)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipViewingDeckLightsPanel = CustomOption.Create(101511, TabGroup.MainSettings, Color.white, "DisableAirshipViewingDeckLightsPanel", false, LightsOutSpecialSettings)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipGapRoomLightsPanel = CustomOption.Create(101512, TabGroup.MainSettings, Color.white, "DisableAirshipGapRoomLightsPanel", false, LightsOutSpecialSettings)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipCargoLightsPanel = CustomOption.Create(101513, TabGroup.MainSettings, Color.white, "DisableAirshipCargoLightsPanel", false, LightsOutSpecialSettings)
                .SetGameMode(CustomGameMode.Standard);

            // タスク無効化
            DisableTasks = CustomOption.Create(100300, TabGroup.MainSettings, Color.white, "DisableTasks", false, null, true)
                .SetGameMode(CustomGameMode.All);
            DisableSwipeCard = CustomOption.Create(100301, TabGroup.MainSettings, Color.white, "DisableSwipeCardTask", false, DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableSubmitScan = CustomOption.Create(100302, TabGroup.MainSettings, Color.white, "DisableSubmitScanTask", false, DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableUnlockSafe = CustomOption.Create(100303, TabGroup.MainSettings, Color.white, "DisableUnlockSafeTask", false, DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableUploadData = CustomOption.Create(100304, TabGroup.MainSettings, Color.white, "DisableUploadDataTask", false, DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableStartReactor = CustomOption.Create(100305, TabGroup.MainSettings, Color.white, "DisableStartReactorTask", false, DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableResetBreaker = CustomOption.Create(100306, TabGroup.MainSettings, Color.white, "DisableResetBreakerTask", false, DisableTasks)
                .SetGameMode(CustomGameMode.All);

            //デバイス無効化
            DisableDevices = CustomOption.Create(101200, TabGroup.MainSettings, Color.white, "DisableDevices", false, null, true)
                .SetGameMode(CustomGameMode.Standard);
            DisableSkeldDevices = CustomOption.Create(101210, TabGroup.MainSettings, Color.white, "DisableSkeldDevices", false, DisableDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableSkeldAdmin = CustomOption.Create(101211, TabGroup.MainSettings, Color.white, "DisableSkeldAdmin", false, DisableSkeldDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableSkeldCamera = CustomOption.Create(101212, TabGroup.MainSettings, Color.white, "DisableSkeldCamera", false, DisableSkeldDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableMiraHQDevices = CustomOption.Create(101220, TabGroup.MainSettings, Color.white, "DisableMiraHQDevices", false, DisableDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableMiraHQAdmin = CustomOption.Create(101221, TabGroup.MainSettings, Color.white, "DisableMiraHQAdmin", false, DisableMiraHQDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableMiraHQDoorLog = CustomOption.Create(101222, TabGroup.MainSettings, Color.white, "DisableMiraHQDoorLog", false, DisableMiraHQDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisablePolusDevices = CustomOption.Create(101230, TabGroup.MainSettings, Color.white, "DisablePolusDevices", false, DisableDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisablePolusAdmin = CustomOption.Create(101231, TabGroup.MainSettings, Color.white, "DisablePolusAdmin", false, DisablePolusDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisablePolusCamera = CustomOption.Create(101232, TabGroup.MainSettings, Color.white, "DisablePolusCamera", false, DisablePolusDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisablePolusVital = CustomOption.Create(101233, TabGroup.MainSettings, Color.white, "DisablePolusVital", false, DisablePolusDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipDevices = CustomOption.Create(101240, TabGroup.MainSettings, Color.white, "DisableAirshipDevices", false, DisableDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipCockpitAdmin = CustomOption.Create(101241, TabGroup.MainSettings, Color.white, "DisableAirshipCockpitAdmin", false, DisableAirshipDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipRecordsAdmin = CustomOption.Create(101242, TabGroup.MainSettings, Color.white, "DisableAirshipRecordsAdmin", false, DisableAirshipDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipCamera = CustomOption.Create(101243, TabGroup.MainSettings, Color.white, "DisableAirshipCamera", false, DisableAirshipDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipVital = CustomOption.Create(101244, TabGroup.MainSettings, Color.white, "DisableAirshipVital", false, DisableAirshipDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableDevicesIgnoreConditions = CustomOption.Create(101290, TabGroup.MainSettings, Color.white, "IgnoreConditions", false, DisableDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableDevicesIgnoreImpostors = CustomOption.Create(101291, TabGroup.MainSettings, Color.white, "IgnoreImpostors", false, DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.Standard);
            DisableDevicesIgnoreMadmates = CustomOption.Create(101292, TabGroup.MainSettings, Color.white, "IgnoreMadmates", false, DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.Standard);
            DisableDevicesIgnoreNeutrals = CustomOption.Create(101293, TabGroup.MainSettings, Color.white, "IgnoreNeutrals", false, DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.Standard);
            DisableDevicesIgnoreCrewmates = CustomOption.Create(101294, TabGroup.MainSettings, Color.white, "IgnoreCrewmates", false, DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.Standard);
            DisableDevicesIgnoreAfterAnyoneDied = CustomOption.Create(101295, TabGroup.MainSettings, Color.white, "IgnoreAfterAnyoneDied", false, DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.Standard);

            // ランダムマップ
            RandomMapsMode = CustomOption.Create(100400, TabGroup.MainSettings, Color.white, "RandomMapsMode", false, null, true)
                .SetGameMode(CustomGameMode.All);
            AddedTheSkeld = CustomOption.Create(100401, TabGroup.MainSettings, Color.white, "AddedTheSkeld", false, RandomMapsMode)
                .SetGameMode(CustomGameMode.All);
            AddedMiraHQ = CustomOption.Create(100402, TabGroup.MainSettings, Color.white, "AddedMIRAHQ", false, RandomMapsMode)
                .SetGameMode(CustomGameMode.All);
            AddedPolus = CustomOption.Create(100403, TabGroup.MainSettings, Color.white, "AddedPolus", false, RandomMapsMode)
                .SetGameMode(CustomGameMode.All);
            AddedTheAirShip = CustomOption.Create(100404, TabGroup.MainSettings, Color.white, "AddedTheAirShip", false, RandomMapsMode)
                .SetGameMode(CustomGameMode.All);
            // MapDleks = CustomOption.Create(100405, TabGroup.MainSettings, Color.white, "AddedDleks", false, RandomMapMode)
            //     .SetGameMode(CustomGameMode.All);

            // ランダムスポーン
            RandomSpawn = CustomOption.Create(101300, TabGroup.MainSettings, Color.white, "RandomSpawn", false, isHeader: true)
                .SetGameMode(CustomGameMode.All);
            AirshipAdditionalSpawn = CustomOption.Create(101301, TabGroup.MainSettings, Color.white, "AirshipAdditionalSpawn", false, RandomSpawn)
                .SetGameMode(CustomGameMode.All);

            // ボタン回数同期
            SyncButtonMode = CustomOption.Create(100200, TabGroup.MainSettings, Color.white, "SyncButtonMode", false, null, true)
                .SetGameMode(CustomGameMode.Standard);
            SyncedButtonCount = CustomOption.Create(100201, TabGroup.MainSettings, Color.white, "SyncedButtonCount", 10, 0, 100, 1, SyncButtonMode, format: "Times")
                .SetGameMode(CustomGameMode.Standard);

            // 投票モード
            VoteMode = CustomOption.Create(100500, TabGroup.MainSettings, Color.white, "VoteMode", false, null, true)
                .SetGameMode(CustomGameMode.Standard);
            WhenSkipVote = CustomOption.Create(100510, TabGroup.MainSettings, Color.white, "WhenSkipVote", voteModes[0..3], voteModes[0], VoteMode)
                .SetGameMode(CustomGameMode.Standard);
            WhenSkipVoteIgnoreFirstMeeting = CustomOption.Create(100511, TabGroup.MainSettings, Color.white, "WhenSkipVoteIgnoreFirstMeeting", false, WhenSkipVote)
                .SetGameMode(CustomGameMode.Standard);
            WhenSkipVoteIgnoreNoDeadBody = CustomOption.Create(100512, TabGroup.MainSettings, Color.white, "WhenSkipVoteIgnoreNoDeadBody", false, WhenSkipVote)
                .SetGameMode(CustomGameMode.Standard);
            WhenSkipVoteIgnoreEmergency = CustomOption.Create(100513, TabGroup.MainSettings, Color.white, "WhenSkipVoteIgnoreEmergency", false, WhenSkipVote)
                .SetGameMode(CustomGameMode.Standard);
            WhenNonVote = CustomOption.Create(100520, TabGroup.MainSettings, Color.white, "WhenNonVote", voteModes, voteModes[0], VoteMode)
                .SetGameMode(CustomGameMode.Standard);
            WhenTie = CustomOption.Create(100530, TabGroup.MainSettings, Color.white, "WhenTie", tieModes, tieModes[0], VoteMode)
                .SetGameMode(CustomGameMode.Standard);

            // 全員生存時の会議時間
            AllAliveMeeting = CustomOption.Create(100900, TabGroup.MainSettings, Color.white, "AllAliveMeeting", false);
            AllAliveMeetingTime = CustomOption.Create(100901, TabGroup.MainSettings, Color.white, "AllAliveMeetingTime", 10, 1, 300, 1, AllAliveMeeting, format: "Seconds");

            // 生存人数ごとの緊急会議
            AdditionalEmergencyCooldown = CustomOption.Create(101400, TabGroup.MainSettings, Color.white, "AdditionalEmergencyCooldown", false);
            AdditionalEmergencyCooldownThreshold = CustomOption.Create(101401, TabGroup.MainSettings, Color.white, "AdditionalEmergencyCooldownThreshold", 1, 1, 15, 1, AdditionalEmergencyCooldown, format: "Players");
            AdditionalEmergencyCooldownTime = CustomOption.Create(101402, TabGroup.MainSettings, Color.white, "AdditionalEmergencyCooldownTime", 1, 1, 60, 1, AdditionalEmergencyCooldown, format: "Seconds");

            // 転落死
            LadderDeath = CustomOption.Create(101100, TabGroup.MainSettings, Color.white, "LadderDeath", false, null, true);
            LadderDeathChance = CustomOption.Create(101110, TabGroup.MainSettings, Color.white, "LadderDeathChance", rates[1..], rates[2], LadderDeath);

            // 通常モードでかくれんぼ用
            StandardHAS = CustomOption.Create(100700, TabGroup.MainSettings, Color.white, "StandardHAS", false, null, true)
                .SetGameMode(CustomGameMode.Standard);
            StandardHASWaitingTime = CustomOption.Create(100701, TabGroup.MainSettings, Color.white, "StandardHASWaitingTime", 10f, 0f, 180f, 2.5f, StandardHAS, format: "Seconds")
                .SetGameMode(CustomGameMode.Standard);

            // その他
            FixFirstKillCooldown = CustomOption.Create(900_000, TabGroup.MainSettings, Color.white, "FixFirstKillCooldown", false, null, true)
                .SetGameMode(CustomGameMode.All);
            DisableTaskWin = CustomOption.Create(900_001, TabGroup.MainSettings, Color.white, "DisableTaskWin", false)
                .SetGameMode(CustomGameMode.All);
            NoGameEnd = CustomOption.Create(900_002, TabGroup.MainSettings, Color.white, "NoGameEnd", false)
                .SetGameMode(CustomGameMode.All);
            GhostCanSeeOtherRoles = CustomOption.Create(900_010, TabGroup.MainSettings, Color.white, "GhostCanSeeOtherRoles", true)
                .SetGameMode(CustomGameMode.All);
            GhostCanSeeOtherVotes = CustomOption.Create(900_011, TabGroup.MainSettings, Color.white, "GhostCanSeeOtherVotes", true)
                .SetGameMode(CustomGameMode.All);
            GhostCanSeeDeathReason = CustomOption.Create(900_014, TabGroup.MainSettings, Color.white, "GhostCanSeeDeathReason", false)
                .SetGameMode(CustomGameMode.All);
            GhostIgnoreTasks = CustomOption.Create(900_012, TabGroup.MainSettings, Color.white, "GhostIgnoreTasks", false)
                .SetGameMode(CustomGameMode.All);
            CommsCamouflage = CustomOption.Create(900_013, TabGroup.MainSettings, Color.white, "CommsCamouflage", false)
                .SetGameMode(CustomGameMode.All);

            // プリセット対象外
            AutoDisplayLastResult = CustomOption.Create(1_000_000, TabGroup.MainSettings, Color.white, "AutoDisplayLastResult", true, null, true)
                .SetGameMode(CustomGameMode.All);
            SuffixMode = CustomOption.Create(1_000_001, TabGroup.MainSettings, Color.white, "SuffixMode", suffixModes, suffixModes[0])
                .SetGameMode(CustomGameMode.All);
            HideGameSettings = CustomOption.Create(1_000_002, TabGroup.MainSettings, Color.white, "HideGameSettings", false)
                .SetGameMode(CustomGameMode.All);
            ColorNameMode = CustomOption.Create(1_000_003, TabGroup.MainSettings, Color.white, "ColorNameMode", false)
                .SetGameMode(CustomGameMode.All);

            IsLoaded = true;
        }

        public static void SetupRoleOptions(int id, TabGroup tab, CustomRoles role, CustomGameMode customGameMode = CustomGameMode.Standard)
        {
            var spawnOption = CustomOption.Create(id, tab, Utils.GetRoleColor(role), role.ToString(), ratesZeroOne, ratesZeroOne[0], null, true)
                .HiddenOnDisplay(true)
                .SetGameMode(customGameMode);
            var countOption = CustomOption.Create(id + 1, tab, Color.white, "Maximum", 1, 1, 15, 1, spawnOption, false, format: "Players")
                .HiddenOnDisplay(true)
                .SetGameMode(customGameMode);

            CustomRoleSpawnChances.Add(role, spawnOption);
            CustomRoleCounts.Add(role, countOption);
        }
        private static void SetupLoversRoleOptionsToggle(int id, CustomGameMode customGameMode = CustomGameMode.Standard)
        {
            var role = CustomRoles.Lovers;
            var spawnOption = CustomOption.Create(id, TabGroup.Addons, Utils.GetRoleColor(role), role.ToString(), ratesZeroOne, ratesZeroOne[0], null, true)
                .HiddenOnDisplay(true)
                .SetGameMode(customGameMode);

            var countOption = CustomOption.Create(id + 1, TabGroup.Addons, Color.white, "NumberOfLovers", 2, 1, 15, 1, spawnOption, false, true)
                .HiddenOnDisplay(false)
                .SetGameMode(customGameMode);

            CustomRoleSpawnChances.Add(role, spawnOption);
            CustomRoleCounts.Add(role, countOption);
        }
        public static void SetupSingleRoleOptions(int id, TabGroup tab, CustomRoles role, int count, CustomGameMode customGameMode = CustomGameMode.Standard)
        {
            var spawnOption = CustomOption.Create(id, tab, Utils.GetRoleColor(role), role.ToString(), ratesZeroOne, ratesZeroOne[0], null, true)
                .HiddenOnDisplay(true)
                .SetGameMode(customGameMode);
            // 初期値,最大値,最小値が同じで、stepが0のどうやっても変えることができない個数オプション
            var countOption = CustomOption.Create(id + 1, tab, Color.white, "Maximum", count, count, count, count, spawnOption, false, true)
                .HiddenOnDisplay(false)
                .SetGameMode(customGameMode);

            CustomRoleSpawnChances.Add(role, spawnOption);
            CustomRoleCounts.Add(role, countOption);
        }
        public class OverrideTasksData
        {
            public static Dictionary<CustomRoles, OverrideTasksData> AllData = new();
            public CustomRoles Role { get; private set; }
            public int IdStart { get; private set; }
            public CustomOption doOverride;
            public CustomOption assignCommonTasks;
            public CustomOption numLongTasks;
            public CustomOption numShortTasks;

            public OverrideTasksData(int idStart, TabGroup tab, CustomRoles role)
            {
                this.IdStart = idStart;
                this.Role = role;
                Dictionary<string, string> replacementDic = new() { { "%role%", Utils.GetRoleName(role) } };
                doOverride = CustomOption.Create(idStart++, tab, Color.white, "doOverride", false, CustomRoleSpawnChances[role], false, false, "", replacementDic);
                assignCommonTasks = CustomOption.Create(idStart++, tab, Color.white, "assignCommonTasks", true, doOverride, false, false, "", replacementDic);
                numLongTasks = CustomOption.Create(idStart++, tab, Color.white, "roleLongTasksNum", 3, 0, 99, 1, doOverride, false, false, "Pieces", replacementDic);
                numShortTasks = CustomOption.Create(idStart++, tab, Color.white, "roleShortTasksNum", 3, 0, 99, 1, doOverride, false, false, "Pieces", replacementDic);

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