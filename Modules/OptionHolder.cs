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

        public static readonly string[] whichDisableAdmin =
        {
            "All", "Archive",
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
        public static int OptionsPage;

        // 各役職の詳細設定
        public static CustomOption EnableLastImpostor;
        public static CustomOption LastImpostorKillCooldown;
        public static CustomOption BountyTargetChangeTime;
        public static CustomOption BountySuccessKillCooldown;
        public static CustomOption BountyFailureKillCooldown;
        public static float DefaultKillCooldown = PlayerControl.GameOptions.KillCooldown;
        public static CustomOption VampireKillDelay;
        //public static CustomOption ShapeMasterShapeshiftDuration;
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
        public static CustomOption JackalKillCooldown;
        public static CustomOption JackalCanVent;
        public static CustomOption JackalCanUseSabotage;
        public static CustomOption JackalHasImpostorVision;

        // HideAndSeek
        public static CustomOption AllowCloseDoors;
        public static CustomOption KillDelay;
        public static CustomOption IgnoreCosmetics;
        public static CustomOption IgnoreVent;
        public static float HideAndSeekKillDelayTimer = 0f;

        //デバイスブロック
        public static CustomOption DisableDevices;
        public static CustomOption DisableAdmin;
        public static CustomOption WhichDisableAdmin;

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

        // タスク上書き
        public static OverrideTasksData MadGuardianTasks;
        public static OverrideTasksData TerroristTasks;
        public static OverrideTasksData SnitchTasks;
        public static OverrideTasksData MadSnitchTasks;

        // その他
        public static CustomOption NoGameEnd;
        public static CustomOption AutoDisplayLastResult;
        public static CustomOption SuffixMode;
        public static CustomOption ColorNameMode;
        public static CustomOption GhostCanSeeOtherRoles;
        public static CustomOption GhostCanSeeOtherVotes;
        public static CustomOption HideGameSettings;
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
            return CustomRoleSpawnChances.TryGetValue(role, out var option) ? option.GetSelection() / 10f : roleSpawnChances[role];
        }
        public static void Load()
        {
            if (IsLoaded) return;
            // プリセット
            _ = CustomOption.Create(0, CustomOption.CustomOptionType.General, new Color(204f / 255f, 204f / 255f, 0, 1f), "Preset", presets, presets[0], null, true)
                .HiddenOnDisplay(true)
                .SetGameMode(CustomGameMode.All);

            // ゲームモード
            GameMode = CustomOption.Create(1, CustomOption.CustomOptionType.General, new Color(204f / 255f, 204f / 255f, 0, 1f), "GameMode", gameModes, gameModes[0], null, true)
                .SetGameMode(CustomGameMode.All);

            #region 役職・詳細設定
            CustomRoleCounts = new Dictionary<CustomRoles, CustomOption>();
            CustomRoleSpawnChances = new Dictionary<CustomRoles, CustomOption>();
            // Impostor
            SetupRoleOptions(1000, CustomOption.CustomOptionType.Impostor, CustomRoles.BountyHunter);
            BountyTargetChangeTime = CustomOption.Create(1010, CustomOption.CustomOptionType.Impostor, Color.white, "BountyTargetChangeTime", 60f, 10f, 900f, 2.5f, CustomRoleSpawnChances[CustomRoles.BountyHunter]);
            BountySuccessKillCooldown = CustomOption.Create(1011, CustomOption.CustomOptionType.Impostor, Color.white, "BountySuccessKillCooldown", 2.5f, 0f, 180f, 2.5f, CustomRoleSpawnChances[CustomRoles.BountyHunter]);
            BountyFailureKillCooldown = CustomOption.Create(1012, CustomOption.CustomOptionType.Impostor, Color.white, "BountyFailureKillCooldown", 50f, 0f, 180f, 2.5f, CustomRoleSpawnChances[CustomRoles.BountyHunter]);
            SerialKiller.SetupCustomOption();
            // SetupRoleOptions(1200, CustomRoles.ShapeMaster);
            // ShapeMasterShapeshiftDuration = CustomOption.Create(1210, Color.white, "ShapeMasterShapeshiftDuration", 10, 1, 1000, 1, CustomRoleSpawnChances[CustomRoles.ShapeMaster]);
            SetupRoleOptions(1300, CustomOption.CustomOptionType.Impostor, CustomRoles.Vampire);
            VampireKillDelay = CustomOption.Create(1310, CustomOption.CustomOptionType.Impostor, Color.white, "VampireKillDelay", 10, 1, 1000, 1, CustomRoleSpawnChances[CustomRoles.Vampire]);
            SetupRoleOptions(1400, CustomOption.CustomOptionType.Impostor, CustomRoles.Warlock);
            SetupRoleOptions(1500, CustomOption.CustomOptionType.Impostor, CustomRoles.Witch);
            SetupRoleOptions(1600, CustomOption.CustomOptionType.Impostor, CustomRoles.Mafia);
            FireWorks.SetupCustomOption();
            Sniper.SetupCustomOption();
            SetupRoleOptions(2000, CustomOption.CustomOptionType.Impostor, CustomRoles.Puppeteer);
            Mare.SetupCustomOption();
            TimeThief.SetupCustomOption();

            DefaultShapeshiftCooldown = CustomOption.Create(5011, CustomOption.CustomOptionType.Impostor, Color.white, "DefaultShapeshiftCooldown", 15, 5, 999, 5, null, true);
            CanMakeMadmateCount = CustomOption.Create(5012, CustomOption.CustomOptionType.Impostor, Color.white, "CanMakeMadmateCount", 0, 0, 15, 1, null, true);

            // Madmate
            SetupRoleOptions(10000, CustomOption.CustomOptionType.Impostor, CustomRoles.Madmate);
            SetupRoleOptions(10100, CustomOption.CustomOptionType.Impostor, CustomRoles.MadGuardian);
            MadGuardianCanSeeWhoTriedToKill = CustomOption.Create(10110, CustomOption.CustomOptionType.Impostor, Color.white, "MadGuardianCanSeeWhoTriedToKill", false, CustomRoleSpawnChances[CustomRoles.MadGuardian]);
            //ID10120~10123を使用
            MadGuardianTasks = OverrideTasksData.Create(10120, CustomOption.CustomOptionType.Impostor, CustomRoles.MadGuardian);
            SetupRoleOptions(10200, CustomOption.CustomOptionType.Impostor, CustomRoles.MadSnitch);
            MadSnitchCanVent = CustomOption.Create(10210, CustomOption.CustomOptionType.Impostor, Color.white, "MadSnitchCanVent", false, CustomRoleSpawnChances[CustomRoles.MadSnitch]);
            //ID10220~10223を使用
            MadSnitchTasks = OverrideTasksData.Create(10220, CustomOption.CustomOptionType.Impostor, CustomRoles.MadSnitch);
            // Madmate Common Options
            MadmateCanFixLightsOut = CustomOption.Create(15010, CustomOption.CustomOptionType.Impostor, Color.white, "MadmateCanFixLightsOut", false, null, true, false);
            MadmateCanFixComms = CustomOption.Create(15011, CustomOption.CustomOptionType.Impostor, Color.white, "MadmateCanFixComms", false);
            MadmateHasImpostorVision = CustomOption.Create(15012, CustomOption.CustomOptionType.Impostor, Color.white, "MadmateHasImpostorVision", false);
            MadmateVentCooldown = CustomOption.Create(15213, CustomOption.CustomOptionType.Impostor, Color.white, "MadmateVentCooldown", 0f, 0f, 180f, 5f);
            MadmateVentMaxTime = CustomOption.Create(15214, CustomOption.CustomOptionType.Impostor, Color.white, "MadmateVentMaxTime", 0f, 0f, 180f, 5f);
            // Both
            SetupRoleOptions(30000, CustomOption.CustomOptionType.Neutral, CustomRoles.Watcher);
            EvilWatcherChance = CustomOption.Create(30010, CustomOption.CustomOptionType.Neutral, Color.white, "EvilWatcherChance", 0, 0, 100, 10, CustomRoleSpawnChances[CustomRoles.Watcher]);
            // Crewmate
            SetupRoleOptions(20000, CustomOption.CustomOptionType.Crewmate, CustomRoles.Bait);
            SetupRoleOptions(20100, CustomOption.CustomOptionType.Crewmate, CustomRoles.Lighter);
            LighterTaskCompletedVision = CustomOption.Create(20110, CustomOption.CustomOptionType.Crewmate, Color.white, "LighterTaskCompletedVision", 2f, 0f, 5f, 0.25f, CustomRoleSpawnChances[CustomRoles.Lighter]);
            LighterTaskCompletedDisableLightOut = CustomOption.Create(20111, CustomOption.CustomOptionType.Crewmate, Color.white, "LighterTaskCompletedDisableLightOut", true, CustomRoleSpawnChances[CustomRoles.Lighter]);
            SetupRoleOptions(20200, CustomOption.CustomOptionType.Crewmate, CustomRoles.Mayor);
            MayorAdditionalVote = CustomOption.Create(20210, CustomOption.CustomOptionType.Crewmate, Color.white, "MayorAdditionalVote", 1, 1, 99, 1, CustomRoleSpawnChances[CustomRoles.Mayor]);
            MayorHasPortableButton = CustomOption.Create(20211, CustomOption.CustomOptionType.Crewmate, Color.white, "MayorHasPortableButton", false, CustomRoleSpawnChances[CustomRoles.Mayor]);
            MayorNumOfUseButton = CustomOption.Create(20212, CustomOption.CustomOptionType.Crewmate, Color.white, "MayorNumOfUseButton", 1, 1, 99, 1, MayorHasPortableButton);
            SetupRoleOptions(20300, CustomOption.CustomOptionType.Crewmate, CustomRoles.SabotageMaster);
            SabotageMasterSkillLimit = CustomOption.Create(20310, CustomOption.CustomOptionType.Crewmate, Color.white, "SabotageMasterSkillLimit", 1, 0, 99, 1, CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
            SabotageMasterFixesDoors = CustomOption.Create(20311, CustomOption.CustomOptionType.Crewmate, Color.white, "SabotageMasterFixesDoors", false, CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
            SabotageMasterFixesReactors = CustomOption.Create(20312, CustomOption.CustomOptionType.Crewmate, Color.white, "SabotageMasterFixesReactors", false, CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
            SabotageMasterFixesOxygens = CustomOption.Create(20313, CustomOption.CustomOptionType.Crewmate, Color.white, "SabotageMasterFixesOxygens", false, CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
            SabotageMasterFixesComms = CustomOption.Create(20314, CustomOption.CustomOptionType.Crewmate, Color.white, "SabotageMasterFixesCommunications", false, CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
            SabotageMasterFixesElectrical = CustomOption.Create(20315, CustomOption.CustomOptionType.Crewmate, Color.white, "SabotageMasterFixesElectrical", false, CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
            Sheriff.SetupCustomOption();
            SetupRoleOptions(20500, CustomOption.CustomOptionType.Crewmate, CustomRoles.Snitch);
            SnitchEnableTargetArrow = CustomOption.Create(20510, CustomOption.CustomOptionType.Crewmate, Color.white, "SnitchEnableTargetArrow", false, CustomRoleSpawnChances[CustomRoles.Snitch]);
            SnitchCanGetArrowColor = CustomOption.Create(20511, CustomOption.CustomOptionType.Crewmate, Color.white, "SnitchCanGetArrowColor", false, CustomRoleSpawnChances[CustomRoles.Snitch]);
            SnitchCanFindNeutralKiller = CustomOption.Create(20512, CustomOption.CustomOptionType.Crewmate, Color.white, "SnitchCanFindNeutralKiller", false, CustomRoleSpawnChances[CustomRoles.Snitch]);
            //20520~20523を使用
            SnitchTasks = OverrideTasksData.Create(20520, CustomOption.CustomOptionType.Crewmate, CustomRoles.Snitch);
            SetupRoleOptions(20600, CustomOption.CustomOptionType.Crewmate, CustomRoles.SpeedBooster);
            SpeedBoosterUpSpeed = CustomOption.Create(20610, CustomOption.CustomOptionType.Crewmate, Color.white, "SpeedBoosterUpSpeed", 2f, 0.25f, 3f, 0.25f, CustomRoleSpawnChances[CustomRoles.SpeedBooster]);
            SetupRoleOptions(20700, CustomOption.CustomOptionType.Crewmate, CustomRoles.Doctor);
            DoctorTaskCompletedBatteryCharge = CustomOption.Create(20710, CustomOption.CustomOptionType.Crewmate, Color.white, "DoctorTaskCompletedBatteryCharge", 5, 0, 10, 1, CustomRoleSpawnChances[CustomRoles.Doctor]);
            SetupRoleOptions(20800, CustomOption.CustomOptionType.Crewmate, CustomRoles.Trapper);
            TrapperBlockMoveTime = CustomOption.Create(20810, CustomOption.CustomOptionType.Crewmate, Color.white, "TrapperBlockMoveTime", 5f, 1f, 180, 1, CustomRoleSpawnChances[CustomRoles.Trapper]);
            SetupRoleOptions(20900, CustomOption.CustomOptionType.Crewmate, CustomRoles.Dictator);
            // Neutral
            SetupRoleOptions(50500, CustomOption.CustomOptionType.Neutral, CustomRoles.Arsonist);
            ArsonistDouseTime = CustomOption.Create(50510, CustomOption.CustomOptionType.Neutral, Color.white, "ArsonistDouseTime", 3, 1, 10, 1, CustomRoleSpawnChances[CustomRoles.Arsonist]);
            ArsonistCooldown = CustomOption.Create(50511, CustomOption.CustomOptionType.Neutral, Color.white, "ArsonistCooldown", 10, 5, 100, 1, CustomRoleSpawnChances[CustomRoles.Arsonist]);
            SetupRoleOptions(50000, CustomOption.CustomOptionType.Neutral, CustomRoles.Jester);
            SetupRoleOptions(50100, CustomOption.CustomOptionType.Neutral, CustomRoles.Opportunist);
            SetupRoleOptions(50200, CustomOption.CustomOptionType.Neutral, CustomRoles.Terrorist);
            CanTerroristSuicideWin = CustomOption.Create(50210, CustomOption.CustomOptionType.Neutral, Color.white, "CanTerroristSuicideWin", false, CustomRoleSpawnChances[CustomRoles.Terrorist], false)
                .SetGameMode(CustomGameMode.Standard);
            //50220~50223を使用
            TerroristTasks = OverrideTasksData.Create(50220, CustomOption.CustomOptionType.Neutral, CustomRoles.Terrorist);
            SetupLoversRoleOptionsToggle(50300, CustomOption.CustomOptionType.Neutral);

            SetupRoleOptions(50400, CustomOption.CustomOptionType.Neutral, CustomRoles.SchrodingerCat);
            CanBeforeSchrodingerCatWinTheCrewmate = CustomOption.Create(50410, CustomOption.CustomOptionType.Neutral, Color.white, "CanBeforeSchrodingerCatWinTheCrewmate", false, CustomRoleSpawnChances[CustomRoles.SchrodingerCat]);
            SchrodingerCatExiledTeamChanges = CustomOption.Create(50411, CustomOption.CustomOptionType.Neutral, Color.white, "SchrodingerCatExiledTeamChanges", false, CustomRoleSpawnChances[CustomRoles.SchrodingerCat]);
            SetupRoleOptions(50600, CustomOption.CustomOptionType.Neutral, CustomRoles.Egoist);
            SetupRoleOptions(50700, CustomOption.CustomOptionType.Neutral, CustomRoles.Executioner);
            ExecutionerCanTargetImpostor = CustomOption.Create(50710, CustomOption.CustomOptionType.Neutral, Color.white, "ExecutionerCanTargetImpostor", false, CustomRoleSpawnChances[CustomRoles.Executioner]);
            ExecutionerChangeRolesAfterTargetKilled = CustomOption.Create(50711, CustomOption.CustomOptionType.Neutral, Color.white, "ExecutionerChangeRolesAfterTargetKilled", ExecutionerChangeRoles, ExecutionerChangeRoles[1], CustomRoleSpawnChances[CustomRoles.Executioner]);
            //Jackalは1人固定
            SetupSingleRoleOptions(50900, CustomOption.CustomOptionType.Neutral, CustomRoles.Jackal, 1);
            JackalKillCooldown = CustomOption.Create(50910, CustomOption.CustomOptionType.Neutral, Color.white, "JackalKillCooldown", 30, 2.5f, 180, 2.5f, CustomRoleSpawnChances[CustomRoles.Jackal]);
            JackalCanVent = CustomOption.Create(50911, CustomOption.CustomOptionType.Neutral, Color.white, "JackalCanVent", true, CustomRoleSpawnChances[CustomRoles.Jackal]);
            JackalCanUseSabotage = CustomOption.Create(50912, CustomOption.CustomOptionType.Neutral, Color.white, "JackalCanUseSabotage", false, CustomRoleSpawnChances[CustomRoles.Jackal]);
            JackalHasImpostorVision = CustomOption.Create(50913, CustomOption.CustomOptionType.Neutral, Color.white, "JackalHasImpostorVision", true, CustomRoleSpawnChances[CustomRoles.Jackal]);

            // Attribute
            EnableLastImpostor = CustomOption.Create(80000, CustomOption.CustomOptionType.Impostor, Utils.GetRoleColor(CustomRoles.Impostor), "LastImpostor", false, null, true)
                .SetGameMode(CustomGameMode.Standard);
            LastImpostorKillCooldown = CustomOption.Create(80010, CustomOption.CustomOptionType.Impostor, Color.white, "LastImpostorKillCooldown", 15, 0, 180, 1, EnableLastImpostor)
                .SetGameMode(CustomGameMode.Standard);
            #endregion

            // HideAndSeek
            SetupRoleOptions(100000, CustomOption.CustomOptionType.Neutral, CustomRoles.HASFox, CustomGameMode.HideAndSeek);
            SetupRoleOptions(100100, CustomOption.CustomOptionType.Neutral, CustomRoles.HASTroll, CustomGameMode.HideAndSeek);
            AllowCloseDoors = CustomOption.Create(101000, CustomOption.CustomOptionType.General, Color.white, "AllowCloseDoors", false, null, true)
                .SetGameMode(CustomGameMode.HideAndSeek);
            KillDelay = CustomOption.Create(101001, CustomOption.CustomOptionType.General, Color.white, "HideAndSeekWaitingTime", 10, 0, 180, 5)
                .SetGameMode(CustomGameMode.HideAndSeek);
            //IgnoreCosmetics = CustomOption.Create(101002, Color.white, "IgnoreCosmetics", false)
            //    .SetGameMode(CustomGameMode.HideAndSeek);
            IgnoreVent = CustomOption.Create(101003, CustomOption.CustomOptionType.General, Color.white, "IgnoreVent", false)
                .SetGameMode(CustomGameMode.HideAndSeek);

            //デバイス無効化
            DisableDevices = CustomOption.Create(100500, CustomOption.CustomOptionType.General, Color.white, "DisableDevices", false, null, true)
                .SetGameMode(CustomGameMode.Standard);
            DisableAdmin = CustomOption.Create(100510, CustomOption.CustomOptionType.General, Color.white, "DisableAdmin", false, DisableDevices)
                .SetGameMode(CustomGameMode.Standard);
            WhichDisableAdmin = CustomOption.Create(100511, CustomOption.CustomOptionType.General, Color.white, "WhichDisableAdmin", whichDisableAdmin, whichDisableAdmin[0], DisableAdmin)
                .SetGameMode(CustomGameMode.Standard);

            // ボタン回数同期
            SyncButtonMode = CustomOption.Create(100200, CustomOption.CustomOptionType.General, Color.white, "SyncButtonMode", false, null, true)
                .SetGameMode(CustomGameMode.Standard);
            SyncedButtonCount = CustomOption.Create(100201, CustomOption.CustomOptionType.General, Color.white, "SyncedButtonCount", 10, 0, 100, 1, SyncButtonMode)
                .SetGameMode(CustomGameMode.Standard);

            // リアクターの時間制御
            SabotageTimeControl = CustomOption.Create(100800, CustomOption.CustomOptionType.General, Color.white, "SabotageTimeControl", false, null, true)
                .SetGameMode(CustomGameMode.Standard);
            PolusReactorTimeLimit = CustomOption.Create(100801, CustomOption.CustomOptionType.General, Color.white, "PolusReactorTimeLimit", 30, 1, 60, 1, SabotageTimeControl)
                .SetGameMode(CustomGameMode.Standard);
            AirshipReactorTimeLimit = CustomOption.Create(100802, CustomOption.CustomOptionType.General, Color.white, "AirshipReactorTimeLimit", 60, 1, 90, 1, SabotageTimeControl)
                .SetGameMode(CustomGameMode.Standard);

            // タスク無効化
            DisableTasks = CustomOption.Create(100300, CustomOption.CustomOptionType.General, Color.white, "DisableTasks", false, null, true)
                .SetGameMode(CustomGameMode.All);
            DisableSwipeCard = CustomOption.Create(100301, CustomOption.CustomOptionType.General, Color.white, "DisableSwipeCardTask", false, DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableSubmitScan = CustomOption.Create(100302, CustomOption.CustomOptionType.General, Color.white, "DisableSubmitScanTask", false, DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableUnlockSafe = CustomOption.Create(100303, CustomOption.CustomOptionType.General, Color.white, "DisableUnlockSafeTask", false, DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableUploadData = CustomOption.Create(100304, CustomOption.CustomOptionType.General, Color.white, "DisableUploadDataTask", false, DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableStartReactor = CustomOption.Create(100305, CustomOption.CustomOptionType.General, Color.white, "DisableStartReactorTask", false, DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableResetBreaker = CustomOption.Create(100306, CustomOption.CustomOptionType.General, Color.white, "DisableResetBreakerTask", false, DisableTasks)
                .SetGameMode(CustomGameMode.All);

            // ランダムマップ
            RandomMapsMode = CustomOption.Create(100400, CustomOption.CustomOptionType.General, Color.white, "RandomMapsMode", false, null, true)
                .SetGameMode(CustomGameMode.All);
            AddedTheSkeld = CustomOption.Create(100401, CustomOption.CustomOptionType.General, Color.white, "AddedTheSkeld", false, RandomMapsMode)
                .SetGameMode(CustomGameMode.All);
            AddedMiraHQ = CustomOption.Create(100402, CustomOption.CustomOptionType.General, Color.white, "AddedMIRAHQ", false, RandomMapsMode)
                .SetGameMode(CustomGameMode.All);
            AddedPolus = CustomOption.Create(100403, CustomOption.CustomOptionType.General, Color.white, "AddedPolus", false, RandomMapsMode)
                .SetGameMode(CustomGameMode.All);
            AddedTheAirShip = CustomOption.Create(100404, CustomOption.CustomOptionType.General, Color.white, "AddedTheAirShip", false, RandomMapsMode)
                .SetGameMode(CustomGameMode.All);
            // MapDleks = CustomOption.Create(100405, Color.white, "AddedDleks", false, RandomMapMode)
            //     .SetGameMode(CustomGameMode.All);

            // 投票モード
            VoteMode = CustomOption.Create(100500, CustomOption.CustomOptionType.General, Color.white, "VoteMode", false, null, true)
                .SetGameMode(CustomGameMode.Standard);
            WhenSkipVote = CustomOption.Create(100501, CustomOption.CustomOptionType.General, Color.white, "WhenSkipVote", voteModes[0..3], voteModes[0], VoteMode)
                .SetGameMode(CustomGameMode.Standard);
            WhenNonVote = CustomOption.Create(100502, CustomOption.CustomOptionType.General, Color.white, "WhenNonVote", voteModes, voteModes[0], VoteMode)
                .SetGameMode(CustomGameMode.Standard);

            // 転落死
            LadderDeath = CustomOption.Create(101100, CustomOption.CustomOptionType.General, Color.white, "LadderDeath", false, null, true);
            LadderDeathChance = CustomOption.Create(101110, CustomOption.CustomOptionType.General, Color.white, "LadderDeathChance", rates[1..], rates[2], LadderDeath);

            // 通常モードでかくれんぼ用
            StandardHAS = CustomOption.Create(100700, CustomOption.CustomOptionType.General, Color.white, "StandardHAS", false, null, true)
                .SetGameMode(CustomGameMode.Standard);
            StandardHASWaitingTime = CustomOption.Create(100701, CustomOption.CustomOptionType.General, Color.white, "StandardHASWaitingTime", 10f, 0f, 180f, 2.5f, StandardHAS)
                .SetGameMode(CustomGameMode.Standard);

            // その他
            NoGameEnd = CustomOption.Create(100600, CustomOption.CustomOptionType.General, Color.white, "NoGameEnd", false, null, true)
                .SetGameMode(CustomGameMode.All);
            AutoDisplayLastResult = CustomOption.Create(100601, CustomOption.CustomOptionType.General, Color.white, "AutoDisplayLastResult", false)
                .SetGameMode(CustomGameMode.All);
            SuffixMode = CustomOption.Create(100602, CustomOption.CustomOptionType.General, Color.white, "SuffixMode", suffixModes, suffixModes[0])
                .SetGameMode(CustomGameMode.All);
            ColorNameMode = CustomOption.Create(100605, CustomOption.CustomOptionType.General, Color.white, "ColorNameMode", false)
                .SetGameMode(CustomGameMode.All);
            GhostCanSeeOtherRoles = CustomOption.Create(100603, CustomOption.CustomOptionType.General, Color.white, "GhostCanSeeOtherRoles", true)
                .SetGameMode(CustomGameMode.All);
            GhostCanSeeOtherVotes = CustomOption.Create(100604, CustomOption.CustomOptionType.General, Color.white, "GhostCanSeeOtherVotes", true)
                .SetGameMode(CustomGameMode.All);
            HideGameSettings = CustomOption.Create(100606, CustomOption.CustomOptionType.General, Color.white, "HideGameSettings", false)
                .SetGameMode(CustomGameMode.All);

            IsLoaded = true;
        }

        public static void SetupRoleOptions(int id, CustomOption.CustomOptionType type, CustomRoles role, CustomGameMode customGameMode = CustomGameMode.Standard)
        {
            var spawnOption = CustomOption.Create(id, type, Utils.GetRoleColor(role), role.ToString(), rates, rates[0], null, true)
                .HiddenOnDisplay(true)
                .SetGameMode(customGameMode);
            var countOption = CustomOption.Create(id + 1, type, Color.white, "Maximum", 1, 1, 15, 1, spawnOption, false)
                .HiddenOnDisplay(true)
                .SetGameMode(customGameMode);

            CustomRoleSpawnChances.Add(role, spawnOption);
            CustomRoleCounts.Add(role, countOption);
        }
        private static void SetupLoversRoleOptionsToggle(int id, CustomOption.CustomOptionType type, CustomGameMode customGameMode = CustomGameMode.Standard)
        {
            var role = CustomRoles.Lovers;
            var spawnOption = CustomOption.Create(id, type, Utils.GetRoleColor(role), role.ToString(), rates, rates[0], null, true)
                .HiddenOnDisplay(true)
                .SetGameMode(customGameMode);

            var countOption = CustomOption.Create(id + 1, type, Color.white, "NumberOfLovers", 2, 1, 15, 1, spawnOption, false, true)
                .HiddenOnDisplay(false)
                .SetGameMode(customGameMode);

            CustomRoleSpawnChances.Add(role, spawnOption);
            CustomRoleCounts.Add(role, countOption);
        }
        public static void SetupSingleRoleOptions(int id, CustomOption.CustomOptionType type, CustomRoles role, int count, CustomGameMode customGameMode = CustomGameMode.Standard)
        {
            var spawnOption = CustomOption.Create(id, type, Utils.GetRoleColor(role), role.ToString(), rates, rates[0], null, true)
                .HiddenOnDisplay(true)
                .SetGameMode(customGameMode);
            // 初期値,最大値,最小値が同じで、stepが0のどうやっても変えることができない個数オプション
            var countOption = CustomOption.Create(id + 1, type, Color.white, "Maximum", count, count, count, count, spawnOption, false, true)
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

            public OverrideTasksData(int idStart, CustomOption.CustomOptionType type, CustomRoles role)
            {
                this.IdStart = idStart;
                this.Role = role;
                Dictionary<string, string> replacementDic = new() { { "%role%", Utils.GetRoleName(role) } };
                doOverride = CustomOption.Create(idStart++, type, Color.white, "doOverride", false, CustomRoleSpawnChances[role], false, false, "", replacementDic);
                assignCommonTasks = CustomOption.Create(idStart++, type, Color.white, "assignCommonTasks", true, doOverride, false, false, "", replacementDic);
                numLongTasks = CustomOption.Create(idStart++, type, Color.white, "roleLongTasksNum", 3, 0, 99, 1, doOverride, false, false, "", replacementDic);
                numShortTasks = CustomOption.Create(idStart++, type, Color.white, "roleShortTasksNum", 3, 0, 99, 1, doOverride, false, false, "", replacementDic);

                if (!AllData.ContainsKey(role)) AllData.Add(role, this);
                else Logger.Warn("重複したCustomRolesを対象とするOverrideTasksDataが作成されました", "OverrideTasksData");
            }
            public static OverrideTasksData Create(int idStart, CustomOption.CustomOptionType type, CustomRoles role)
            {
                return new OverrideTasksData(idStart, type, role);
            }
        }
    }
}