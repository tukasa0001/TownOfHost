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
            "Preset_1", "Preset_2", "Preset_3",
            "Preset_4", "Preset_5"
        };

        // ゲームモード
        public static CustomOption GameMode;
        public static CustomGameMode CurrentGameMode
            => GameMode.Selection == 0 ? CustomGameMode.Standard : CustomGameMode.HideAndSeek;

        public static readonly string[] gameModes =
        {
            "Standard", "HideAndSeek",
        };

        // 役職数・確率
        public static Dictionary<CustomRoles, int> roleCounts;
        public static Dictionary<CustomRoles, float> roleSpawnChances;
        public static Dictionary<CustomRoles, CustomOption> CustomRoleCounts;
        public static Dictionary<CustomRoles, CustomOption> CustomRoleSpawnChances;
        public static readonly string[] rates =
        {
            "Rate0", "Rate10", "Rate20", "Rate30", "Rate40", "Rate50",
            "Rate60", "Rate70", "Rate80", "Rate90", "Rate100",
        };
        public static readonly string[] ExecutionerChangeRoles =
        {
            CustomRoles.Crewmate.ToString(), CustomRoles.Jester.ToString(), CustomRoles.Opportunist.ToString(),
        };
        public static readonly CustomRoles[] CRoleExecutionerChangeRoles =
        {
            CustomRoles.Crewmate, CustomRoles.Jester, CustomRoles.Opportunist,
        };

        // 各役職の詳細設定
        public static CustomOption EnableLastImpostor;
        public static CustomOption LastImpostorKillCooldown;
        public static CustomOption BountyTargetChangeTime;
        public static CustomOption BountySuccessKillCooldown;
        public static CustomOption BountyFailureKillCooldown;
        public static float DefaultKillCooldown;
        public static CustomOption SerialKillerCooldown;
        public static CustomOption SerialKillerLimit;
        public static CustomOption TimeThiefDecreaseMeetingTime;
        public static CustomOption TimeThiefLowerLimitVotingTime;
        public static CustomOption VampireKillDelay;
        public static CustomOption BlackOutMareSpeed;
        public static CustomOption ShapeMasterShapeshiftDuration;
        public static CustomOption DefaultShapeshiftCooldown;
        public static CustomOption CanMakeMadmateCount;
        public static CustomOption MadGuardianCanSeeWhoTriedToKill;
        public static CustomOption MadSnitchCanVent;
        public static CustomOption MadmateCanFixLightsOut; // TODO:mii-47 マッド役職統一
        public static CustomOption MadmateCanFixComms;
        public static CustomOption MadmateHasImpostorVision;
        public static CustomOption MadmateVentCooldown;
        public static CustomOption MadmateVentMaxTime;

        public static CustomOption EvilWatcherChance;
        public static CustomOption LighterTaskCompletedVision;
        public static CustomOption LighterTaskCompletedDisableLightOut;
        public static CustomOption MayorAdditionalVote;
        public static CustomOption MayorHasPortableButton;
        public static CustomOption MayorNumOfUseButton;
        public static CustomOption SabotageMasterSkillLimit;
        public static CustomOption SabotageMasterFixesDoors;
        public static CustomOption SabotageMasterFixesReactors;
        public static CustomOption SabotageMasterFixesOxygens;
        public static CustomOption SabotageMasterFixesComms;
        public static CustomOption SabotageMasterFixesElectrical;
        public static int SabotageMasterUsedSkillCount;
        public static CustomOption DoctorTaskCompletedBatteryCharge;
        public static CustomOption SheriffKillCooldown;
        public static CustomOption SheriffCanKillArsonist;
        public static CustomOption SheriffCanKillMadmate;
        public static CustomOption SheriffCanKillJester;
        public static CustomOption SheriffCanKillTerrorist;
        public static CustomOption SheriffCanKillOpportunist;
        public static CustomOption SheriffCanKillEgoist;
        public static CustomOption SheriffCanKillEgoShrodingerCat;
        public static CustomOption SheriffCanKillExecutioner;
        public static CustomOption SheriffCanKillCrewmatesAsIt;
        public static CustomOption SheriffShotLimit;
        public static CustomOption SnitchEnableTargetArrow;
        public static CustomOption SnitchCanGetArrowColor;
        public static CustomOption SnitchCanFindNeutralKiller;
        public static CustomOption SpeedBoosterUpSpeed;
        public static CustomOption TrapperBlockMoveTime;
        public static CustomOption CanTerroristSuicideWin;
        public static CustomOption ArsonistDouseTime;
        public static CustomOption ArsonistCooldown;
        public static CustomOption CanBeforeSchrodingerCatWinTheCrewmate;
        public static CustomOption SchrodingerCatExiledTeamChanges;
        public static CustomOption ExecutionerCanTargetImpostor;
        public static CustomOption ExecutionerChangeRolesAfterTargetKilled;

        // HideAndSeek
        public static CustomOption AllowCloseDoors;
        public static CustomOption KillDelay;
        public static CustomOption IgnoreCosmetics;
        public static CustomOption IgnoreVent;
        public static float HideAndSeekKillDelayTimer = 0f;
        public static float HideAndSeekImpVisionMin = 0.25f;

        // ボタン回数
        public static CustomOption SyncButtonMode;
        public static CustomOption SyncedButtonCount;
        public static int UsedButtonCount = 0;

        // タスク無効化
        public static CustomOption DisableTasks;
        public static CustomOption DisableSwipeCard;
        public static CustomOption DisableSubmitScan;
        public static CustomOption DisableUnlockSafe;
        public static CustomOption DisableUploadData;
        public static CustomOption DisableStartReactor;
        public static CustomOption DisableResetBreaker;

        // ランダムマップ
        public static CustomOption RandomMapsMode;
        public static CustomOption AddedTheSkeld;
        public static CustomOption AddedMiraHQ;
        public static CustomOption AddedPolus;
        public static CustomOption AddedTheAirShip;
        public static CustomOption AddedDleks;

        // 投票モード
        public static CustomOption VoteMode;
        public static CustomOption WhenSkipVote;
        public static CustomOption WhenNonVote;
        public static readonly string[] voteModes =
        {
            "Default", "Suicide", "SelfVote", "Skip"
        };
        public static VoteMode GetWhenSkipVote() => (VoteMode)WhenSkipVote.GetSelection();
        public static VoteMode GetWhenNonVote() => (VoteMode)WhenNonVote.GetSelection();

        // 通常モードでかくれんぼ
        public static CustomOption StandardHAS;

        // リアクターの時間制御
        public static CustomOption SabotageTimeControl;
        public static CustomOption PolusReactorTimeLimit;
        public static CustomOption AirshipReactorTimeLimit;

        // タスク上書き
        public static OverrideTasksData MadGuardianTasks;
        public static OverrideTasksData TerroristTasks;
        public static OverrideTasksData SnitchTasks;
        public static OverrideTasksData MadSnitchTasks;

        // その他
        public static CustomOption NoGameEnd;
        public static CustomOption AutoDisplayLastResult;
        public static CustomOption SuffixMode;
        public static CustomOption GhostCanSeeOtherRoles;
        public static CustomOption GhostCanSeeOtherVotes;
        public static readonly string[] suffixModes =
        {
            "SuffixMode.None",
            "SuffixMode.Version",
            "SuffixMode.Streaming",
            "SuffixMode.Recording"
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
            var chance = CustomRoleSpawnChances.TryGetValue(role, out var sc) ? sc.GetSelection() : 0;
            return chance == 0 ? 0 : CustomRoleCounts.TryGetValue(role, out var option) ? (int)option.GetFloat() : roleCounts[role];
        }

        public static float GetRoleChance(CustomRoles role)
        {
            return CustomRoleSpawnChances.TryGetValue(role, out var option) ? option.GetSelection() / 10 : roleSpawnChances[role];
        }
        public static void Load()
        {
            if (IsLoaded) return;
            // プリセット
            _ = CustomOption.Create(0, new Color(204f / 255f, 204f / 255f, 0, 1f), "Preset", presets, presets[0], null, true)
                .HiddenOnDisplay(true)
                .SetGameMode(CustomGameMode.All);

            // ゲームモード
            GameMode = CustomOption.Create(1, new Color(204f / 255f, 204f / 255f, 0, 1f), "GameMode", gameModes, gameModes[0], null, true)
                .SetGameMode(CustomGameMode.All);

            #region 役職・詳細設定
            CustomRoleCounts = new Dictionary<CustomRoles, CustomOption>();
            CustomRoleSpawnChances = new Dictionary<CustomRoles, CustomOption>();
            // Impostor
            SetupRoleOptions(1000, CustomRoles.BountyHunter);
            BountyTargetChangeTime = CustomOption.Create(1010, Color.white, "BountyTargetChangeTime", 60f, 10f, 900f, 2.5f, CustomRoleSpawnChances[CustomRoles.BountyHunter]);
            BountySuccessKillCooldown = CustomOption.Create(1011, Color.white, "BountySuccessKillCooldown", 2.5f, 0f, 180f, 2.5f, CustomRoleSpawnChances[CustomRoles.BountyHunter]);
            BountyFailureKillCooldown = CustomOption.Create(1012, Color.white, "BountyFailureKillCooldown", 50f, 0f, 180f, 2.5f, CustomRoleSpawnChances[CustomRoles.BountyHunter]);
            SetupRoleOptions(1100, CustomRoles.SerialKiller);
            SerialKillerCooldown = CustomOption.Create(1110, Color.white, "SerialKillerCooldown", 20f, 2.5f, 180f, 2.5f, CustomRoleSpawnChances[CustomRoles.SerialKiller]);
            SerialKillerLimit = CustomOption.Create(1111, Color.white, "SerialKillerLimit", 60f, 5f, 900f, 5f, CustomRoleSpawnChances[CustomRoles.SerialKiller]);
            SetupRoleOptions(1200, CustomRoles.ShapeMaster);
            ShapeMasterShapeshiftDuration = CustomOption.Create(1210, Color.white, "ShapeMasterShapeshiftDuration", 10, 1, 1000, 1, CustomRoleSpawnChances[CustomRoles.ShapeMaster]);
            SetupRoleOptions(1300, CustomRoles.Vampire);
            VampireKillDelay = CustomOption.Create(1310, Color.white, "VampireKillDelay", 10, 1, 1000, 1, CustomRoleSpawnChances[CustomRoles.Vampire]);
            SetupRoleOptions(1400, CustomRoles.Warlock);
            SetupRoleOptions(1500, CustomRoles.Witch);
            SetupRoleOptions(1600, CustomRoles.Mafia);
            FireWorks.SetupCustomOption();
            Sniper.SetupCustomOption();
            SetupRoleOptions(2000, CustomRoles.Puppeteer);
            SetupRoleOptions(2300, CustomRoles.Mare);
            BlackOutMareSpeed = CustomOption.Create(2310, Color.white, "BlackOutMareSpeed", 2f, 0.25f, 3f, 0.25f, CustomRoleSpawnChances[CustomRoles.Mare]);
            SetupRoleOptions(2400, CustomRoles.TimeThief);
            TimeThiefDecreaseMeetingTime = CustomOption.Create(2410, Color.white, "TimeThiefDecreaseMeetingTime", 20, 0, 100, 1, CustomRoleSpawnChances[CustomRoles.TimeThief]);
            TimeThiefLowerLimitVotingTime = CustomOption.Create(2411, Color.white, "TimeThiefLowerLimitVotingTime", 10, 1, 300, 1, CustomRoleSpawnChances[CustomRoles.TimeThief]);

            DefaultShapeshiftCooldown = CustomOption.Create(5011, Color.white, "DefaultShapeshiftCooldown", 15, 5, 999, 5, null, true);
            CanMakeMadmateCount = CustomOption.Create(5012, Color.white, "CanMakeMadmateCount", 0, 0, 15, 1, null, true);

            // Madmate
            SetupRoleOptions(10000, CustomRoles.Madmate);
            SetupRoleOptions(10100, CustomRoles.MadGuardian);
            MadGuardianCanSeeWhoTriedToKill = CustomOption.Create(10110, Color.white, "MadGuardianCanSeeWhoTriedToKill", false, CustomRoleSpawnChances[CustomRoles.MadGuardian]);
            MadGuardianTasks = OverrideTasksData.Create(10120, CustomRoles.MadGuardian);
            //ID10120~10123を使用
            SetupRoleOptions(10200, CustomRoles.MadSnitch);
            MadSnitchCanVent = CustomOption.Create(10210, Color.white, "MadSnitchCanVent", false, CustomRoleSpawnChances[CustomRoles.MadSnitch]);
            MadSnitchTasks = OverrideTasksData.Create(10220, CustomRoles.MadSnitch);
            //ID10220~10223を使用
            // Madmate Common Options
            MadmateCanFixLightsOut = CustomOption.Create(15010, Color.white, "MadmateCanFixLightsOut", false, null, true, false);
            MadmateCanFixComms = CustomOption.Create(15011, Color.white, "MadmateCanFixComms", false);
            MadmateHasImpostorVision = CustomOption.Create(15012, Color.white, "MadmateHasImpostorVision", false);
            MadmateVentCooldown = CustomOption.Create(15213, Color.white, "MadmateVentCooldown", 0f, 0f, 180f, 5f);
            MadmateVentMaxTime = CustomOption.Create(15214, Color.white, "MadmateVentMaxTime", 0f, 0f, 180f, 5f);
            // Both
            SetupRoleOptions(30000, CustomRoles.Watcher);
            EvilWatcherChance = CustomOption.Create(30010, Color.white, "EvilWatcherChance", 0, 0, 100, 10, CustomRoleSpawnChances[CustomRoles.Watcher]);
            // Crewmate
            SetupRoleOptions(20000, CustomRoles.Bait);
            SetupRoleOptions(20100, CustomRoles.Lighter);
            LighterTaskCompletedVision = CustomOption.Create(20110, Color.white, "LighterTaskCompletedVision", 2f, 0f, 5f, 0.25f, CustomRoleSpawnChances[CustomRoles.Lighter]);
            LighterTaskCompletedDisableLightOut = CustomOption.Create(20111, Color.white, "LighterTaskCompletedDisableLightOut", true, CustomRoleSpawnChances[CustomRoles.Lighter]);
            SetupRoleOptions(20200, CustomRoles.Mayor);
            MayorAdditionalVote = CustomOption.Create(20210, Color.white, "MayorAdditionalVote", 1, 1, 99, 1, CustomRoleSpawnChances[CustomRoles.Mayor]);
            MayorHasPortableButton = CustomOption.Create(20211, Color.white, "MayorHasPortableButton", false, CustomRoleSpawnChances[CustomRoles.Mayor]);
            MayorNumOfUseButton = CustomOption.Create(20212, Color.white, "MayorNumOfUseButton", 1, 1, 99, 1, MayorHasPortableButton);
            SetupRoleOptions(20300, CustomRoles.SabotageMaster);
            SabotageMasterSkillLimit = CustomOption.Create(20310, Color.white, "SabotageMasterSkillLimit", 1, 0, 99, 1, CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
            SabotageMasterFixesDoors = CustomOption.Create(20311, Color.white, "SabotageMasterFixesDoors", false, CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
            SabotageMasterFixesReactors = CustomOption.Create(20312, Color.white, "SabotageMasterFixesReactors", false, CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
            SabotageMasterFixesOxygens = CustomOption.Create(20313, Color.white, "SabotageMasterFixesOxygens", false, CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
            SabotageMasterFixesComms = CustomOption.Create(20314, Color.white, "SabotageMasterFixesCommunications", false, CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
            SabotageMasterFixesElectrical = CustomOption.Create(20315, Color.white, "SabotageMasterFixesElectrical", false, CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
            SetupRoleOptions(20400, CustomRoles.Sheriff);
            SheriffKillCooldown = CustomOption.Create(20410, Color.white, "SheriffKillCooldown", 30, 0, 990, 1, CustomRoleSpawnChances[CustomRoles.Sheriff]);
            SheriffCanKillArsonist = CustomOption.Create(20417, Color.white, "SheriffCanKillArsonist", true, CustomRoleSpawnChances[CustomRoles.Sheriff]);
            SheriffCanKillMadmate = CustomOption.Create(20411, Color.white, "SheriffCanKillMadmate", true, CustomRoleSpawnChances[CustomRoles.Sheriff]);
            SheriffCanKillJester = CustomOption.Create(20412, Color.white, "SheriffCanKillJester", true, CustomRoleSpawnChances[CustomRoles.Sheriff]);
            SheriffCanKillTerrorist = CustomOption.Create(20413, Color.white, "SheriffCanKillTerrorist", true, CustomRoleSpawnChances[CustomRoles.Sheriff]);
            SheriffCanKillOpportunist = CustomOption.Create(20414, Color.white, "SheriffCanKillOpportunist", true, CustomRoleSpawnChances[CustomRoles.Sheriff]);
            SheriffCanKillEgoist = CustomOption.Create(20418, Color.white, "SheriffCanKillEgoist", true, CustomRoleSpawnChances[CustomRoles.Sheriff]);
            SheriffCanKillEgoShrodingerCat = CustomOption.Create(20419, Color.white, "SheriffCanKillEgoShrodingerCat", true, CustomRoleSpawnChances[CustomRoles.Sheriff]);
            SheriffCanKillExecutioner = CustomOption.Create(20419, Color.white, "SheriffCanKillExecutioner", true, CustomRoleSpawnChances[CustomRoles.Sheriff]);
            SheriffCanKillCrewmatesAsIt = CustomOption.Create(20415, Color.white, "SheriffCanKillCrewmatesAsIt", false, CustomRoleSpawnChances[CustomRoles.Sheriff]);
            SheriffShotLimit = CustomOption.Create(20416, Color.white, "SheriffShotLimit", 15, 1, 15, 1, CustomRoleSpawnChances[CustomRoles.Sheriff]);
            SetupRoleOptions(20500, CustomRoles.Snitch);
            SnitchEnableTargetArrow = CustomOption.Create(20510, Color.white, "SnitchEnableTargetArrow", false, CustomRoleSpawnChances[CustomRoles.Snitch]);
            SnitchCanGetArrowColor = CustomOption.Create(20511, Color.white, "SnitchCanGetArrowColor", false, CustomRoleSpawnChances[CustomRoles.Snitch]);
            SnitchCanFindNeutralKiller = CustomOption.Create(20512, Color.white, "SnitchCanFindNeutralKiller", false, CustomRoleSpawnChances[CustomRoles.Snitch]);
            SnitchTasks = OverrideTasksData.Create(20520, CustomRoles.Snitch);
            //20520~20523を使用
            SetupRoleOptions(20600, CustomRoles.SpeedBooster);
            SpeedBoosterUpSpeed = CustomOption.Create(20610, Color.white, "SpeedBoosterUpSpeed", 2f, 0.25f, 3f, 0.25f, CustomRoleSpawnChances[CustomRoles.SpeedBooster]);
            SetupRoleOptions(20700, CustomRoles.Doctor);
            DoctorTaskCompletedBatteryCharge = CustomOption.Create(20710, Color.white, "DoctorTaskCompletedBatteryCharge", 5, 0, 10, 1, CustomRoleSpawnChances[CustomRoles.Doctor]);
            SetupRoleOptions(20800, CustomRoles.Trapper);
            TrapperBlockMoveTime = CustomOption.Create(20810, Color.white, "TrapperBlockMoveTime", 5f, 1f, 180, 1, CustomRoleSpawnChances[CustomRoles.Trapper]);
            SetupRoleOptions(20900, CustomRoles.Dictator);
            // Neutral
            SetupRoleOptions(50500, CustomRoles.Arsonist);
            ArsonistDouseTime = CustomOption.Create(50510, Color.white, "ArsonistDouseTime", 3, 1, 10, 1, CustomRoleSpawnChances[CustomRoles.Arsonist]);
            ArsonistCooldown = CustomOption.Create(50511, Color.white, "ArsonistCooldown", 10, 5, 100, 1, CustomRoleSpawnChances[CustomRoles.Arsonist]);
            SetupRoleOptions(50000, CustomRoles.Jester);
            SetupRoleOptions(50100, CustomRoles.Opportunist);
            SetupRoleOptions(50200, CustomRoles.Terrorist);
            CanTerroristSuicideWin = CustomOption.Create(50210, Color.white, "CanTerroristSuicideWin", false, CustomRoleSpawnChances[CustomRoles.Terrorist], false)
                .SetGameMode(CustomGameMode.Standard);
            SnitchTasks = OverrideTasksData.Create(50220, CustomRoles.Terrorist);
            //50220~50223を使用
            SetupLoversRoleOptionsToggle(50300);

            SetupRoleOptions(50400, CustomRoles.SchrodingerCat);
            CanBeforeSchrodingerCatWinTheCrewmate = CustomOption.Create(50410, Color.white, "CanBeforeSchrodingerCatWinTheCrewmate", false, CustomRoleSpawnChances[CustomRoles.SchrodingerCat]);
            SchrodingerCatExiledTeamChanges = CustomOption.Create(50411, Color.white, "SchrodingerCatExiledTeamChanges", false, CustomRoleSpawnChances[CustomRoles.SchrodingerCat]);
            SetupRoleOptions(50600, CustomRoles.Egoist);
            SetupRoleOptions(50700, CustomRoles.Executioner);
            ExecutionerCanTargetImpostor = CustomOption.Create(50710, Color.white, "ExecutionerCanTargetImpostor", false, CustomRoleSpawnChances[CustomRoles.Executioner]);
            ExecutionerChangeRolesAfterTargetKilled = CustomOption.Create(50711, Color.white, "ExecutionerChangeRolesAfterTargetKilled", ExecutionerChangeRoles, ExecutionerChangeRoles[1], CustomRoleSpawnChances[CustomRoles.Executioner]);

            // Attribute
            EnableLastImpostor = CustomOption.Create(80000, Utils.GetRoleColor(CustomRoles.Impostor), "LastImpostor", false, null, true)
                .SetGameMode(CustomGameMode.Standard);
            LastImpostorKillCooldown = CustomOption.Create(80010, Color.white, "LastImpostorKillCooldown", 15, 0, 180, 1, EnableLastImpostor)
                .SetGameMode(CustomGameMode.Standard);
            #endregion

            // HideAndSeek
            SetupRoleOptions(100000, CustomRoles.HASFox, CustomGameMode.HideAndSeek);
            SetupRoleOptions(100100, CustomRoles.HASTroll, CustomGameMode.HideAndSeek);
            AllowCloseDoors = CustomOption.Create(101000, Color.white, "AllowCloseDoors", false, null, true)
                .SetGameMode(CustomGameMode.HideAndSeek);
            KillDelay = CustomOption.Create(101001, Color.white, "HideAndSeekWaitingTime", 10, 0, 180, 5)
                .SetGameMode(CustomGameMode.HideAndSeek);
            //IgnoreCosmetics = CustomOption.Create(101002, Color.white, "IgnoreCosmetics", false)
            //    .SetGameMode(CustomGameMode.HideAndSeek);
            IgnoreVent = CustomOption.Create(101003, Color.white, "IgnoreVent", false)
                .SetGameMode(CustomGameMode.HideAndSeek);

            // ボタン回数同期
            SyncButtonMode = CustomOption.Create(100200, Color.white, "SyncButtonMode", false, null, true)
                .SetGameMode(CustomGameMode.Standard);
            SyncedButtonCount = CustomOption.Create(100201, Color.white, "SyncedButtonCount", 10, 0, 100, 1, SyncButtonMode)
                .SetGameMode(CustomGameMode.Standard);

            // リアクターの時間制御
            SabotageTimeControl = CustomOption.Create(100800, Color.white, "SabotageTimeControl", false, null, true)
                .SetGameMode(CustomGameMode.Standard);
            PolusReactorTimeLimit = CustomOption.Create(100801, Color.white, "PolusReactorTimeLimit", 30, 1, 60, 1, SabotageTimeControl)
                .SetGameMode(CustomGameMode.Standard);
            AirshipReactorTimeLimit = CustomOption.Create(100802, Color.white, "AirshipReactorTimeLimit", 60, 1, 90, 1, SabotageTimeControl)
                .SetGameMode(CustomGameMode.Standard);

            // タスク無効化
            DisableTasks = CustomOption.Create(100300, Color.white, "DisableTasks", false, null, true)
                .SetGameMode(CustomGameMode.All);
            DisableSwipeCard = CustomOption.Create(100301, Color.white, "DisableSwipeCardTask", false, DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableSubmitScan = CustomOption.Create(100302, Color.white, "DisableSubmitScanTask", false, DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableUnlockSafe = CustomOption.Create(100303, Color.white, "DisableUnlockSafeTask", false, DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableUploadData = CustomOption.Create(100304, Color.white, "DisableUploadDataTask", false, DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableStartReactor = CustomOption.Create(100305, Color.white, "DisableStartReactorTask", false, DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableResetBreaker = CustomOption.Create(100306, Color.white, "DisableResetBreakerTask", false, DisableTasks)
                .SetGameMode(CustomGameMode.All);

            // ランダムマップ
            RandomMapsMode = CustomOption.Create(100400, Color.white, "RandomMapsMode", false, null, true)
                .SetGameMode(CustomGameMode.All);
            AddedTheSkeld = CustomOption.Create(100401, Color.white, "AddedTheSkeld", false, RandomMapsMode)
                .SetGameMode(CustomGameMode.All);
            AddedMiraHQ = CustomOption.Create(100402, Color.white, "AddedMIRAHQ", false, RandomMapsMode)
                .SetGameMode(CustomGameMode.All);
            AddedPolus = CustomOption.Create(100403, Color.white, "AddedPolus", false, RandomMapsMode)
                .SetGameMode(CustomGameMode.All);
            AddedTheAirShip = CustomOption.Create(100404, Color.white, "AddedTheAirShip", false, RandomMapsMode)
                .SetGameMode(CustomGameMode.All);
            // MapDleks = CustomOption.Create(100405, Color.white, "AddedDleks", false, RandomMapMode)
            //     .SetGameMode(CustomGameMode.All);

            // 投票モード
            VoteMode = CustomOption.Create(100500, Color.white, "VoteMode", false, null, true)
                .SetGameMode(CustomGameMode.Standard);
            WhenSkipVote = CustomOption.Create(100501, Color.white, "WhenSkipVote", voteModes[0..3], voteModes[0], VoteMode)
                .SetGameMode(CustomGameMode.Standard);
            WhenNonVote = CustomOption.Create(100502, Color.white, "WhenNonVote", voteModes, voteModes[0], VoteMode)
                .SetGameMode(CustomGameMode.Standard);

            // 通常モードでかくれんぼ用
            StandardHAS = CustomOption.Create(100700, Color.white, "StandardHAS", false, null, true)
                .SetGameMode(CustomGameMode.Standard);

            // その他
            NoGameEnd = CustomOption.Create(100600, Color.white, "NoGameEnd", false, null, true)
                .SetGameMode(CustomGameMode.All);
            AutoDisplayLastResult = CustomOption.Create(100601, Color.white, "AutoDisplayLastResult", false)
                .SetGameMode(CustomGameMode.All);
            SuffixMode = CustomOption.Create(100602, Color.white, "SuffixMode", suffixModes, suffixModes[0])
                .SetGameMode(CustomGameMode.All);
            GhostCanSeeOtherRoles = CustomOption.Create(100603, Color.white, "GhostCanSeeOtherRoles", true)
                .SetGameMode(CustomGameMode.All);
            GhostCanSeeOtherVotes = CustomOption.Create(100604, Color.white, "GhostCanSeeOtherVotes", true)
                .SetGameMode(CustomGameMode.All);

            IsLoaded = true;
        }

        public static void SetupRoleOptions(int id, CustomRoles role, CustomGameMode customGameMode = CustomGameMode.Standard)
        {
            var spawnOption = CustomOption.Create(id, Utils.GetRoleColor(role), role.ToString(), rates, rates[0], null, true)
                .HiddenOnDisplay(true)
                .SetGameMode(customGameMode);
            var countOption = CustomOption.Create(id + 1, Color.white, "Maximum", 1, 1, 15, 1, spawnOption, false)
                .HiddenOnDisplay(true)
                .SetGameMode(customGameMode);

            CustomRoleSpawnChances.Add(role, spawnOption);
            CustomRoleCounts.Add(role, countOption);
        }
        private static void SetupLoversRoleOptionsToggle(int id, CustomGameMode customGameMode = CustomGameMode.Standard)
        {
            var role = CustomRoles.Lovers;
            var spawnOption = CustomOption.Create(id, Utils.GetRoleColor(role), role.ToString(), rates, rates[0], null, true)
                .HiddenOnDisplay(true)
                .SetGameMode(customGameMode);

            var countOption = CustomOption.Create(id + 1, Color.white, "NumberOfLovers", 2, 1, 15, 1, spawnOption, false, true)
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

            public OverrideTasksData(int idStart, CustomRoles role)
            {
                this.IdStart = idStart;
                this.Role = role;
                Dictionary<string, string> replacementDic = new() { { "%role%", Utils.GetRoleName(role) } };
                doOverride = CustomOption.Create(idStart++, Color.white, "doOverride", false, CustomRoleSpawnChances[role], false, false, "", replacementDic);
                assignCommonTasks = CustomOption.Create(idStart++, Color.white, "assignCommonTasks", true, doOverride, false, false, "", replacementDic);
                numLongTasks = CustomOption.Create(idStart++, Color.white, "roleLongTasksNum", 3, 0, 99, 1, doOverride, false, false, "", replacementDic);
                numShortTasks = CustomOption.Create(idStart++, Color.white, "roleShortTasksNum", 3, 0, 99, 1, doOverride, false, false, "", replacementDic);

                if (!AllData.ContainsKey(role)) AllData.Add(role, this);
                else Logger.Warn("重複したCustomRolesを対象とするOverrideTasksDataが作成されました", "OverrideTasksData");
            }
            public static OverrideTasksData Create(int idStart, CustomRoles role)
            {
                return new OverrideTasksData(idStart, role);
            }
        }
    }
}