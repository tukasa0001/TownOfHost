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
        public static OptionItem GameMode;
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
        public static Dictionary<CustomRoles, OptionItem> CustomRoleCounts;
        public static Dictionary<CustomRoles, OptionItem> CustomRoleSpawnChances;
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
        public static float DefaultKillCooldown = PlayerControl.GameOptions.KillCooldown;
        public static OptionItem VampireKillDelay;
        //public static CustomOption ShapeMasterShapeshiftDuration;
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
        public static OptionItem MadmateVentCooldown;
        public static OptionItem MadmateVentMaxTime;

        public static OptionItem EvilWatcherChance;
        public static OptionItem LighterTaskCompletedVision;
        public static OptionItem LighterTaskCompletedDisableLightOut;
        public static OptionItem MayorAdditionalVote;
        public static OptionItem MayorHasPortableButton;
        public static OptionItem MayorNumOfUseButton;
        public static OptionItem DoctorTaskCompletedBatteryCharge;
        public static OptionItem SnitchEnableTargetArrow;
        public static OptionItem SnitchCanGetArrowColor;
        public static OptionItem SnitchCanFindNeutralKiller;
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
        public static VoteMode GetWhenSkipVote() => (VoteMode)WhenSkipVote.GetSelection();
        public static VoteMode GetWhenNonVote() => (VoteMode)WhenNonVote.GetSelection();

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
        public static OptionItem SuffixMode;
        public static OptionItem HideGameSettings;
        public static OptionItem ColorNameMode;
        public static OptionItem ChangeNameToRoleInfo;
        public static OptionItem RoleAssigningAlgorithm;

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
            _ = PresetOptionItem.Create(0, TabGroup.MainSettings)
                .SetColor(new Color32(204, 204, 0, 255))
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All);

            // ゲームモード
            GameMode = StringOptionItem.Create(1, "GameMode", gameModes, 0, TabGroup.MainSettings, false)
                .SetGameMode(CustomGameMode.All);

            #region 役職・詳細設定
            CustomRoleCounts = new Dictionary<CustomRoles, OptionItem>();
            CustomRoleSpawnChances = new Dictionary<CustomRoles, OptionItem>();
            // GM
            EnableGM = OptionItem.Create(100, TabGroup.MainSettings, Utils.GetRoleColor(CustomRoles.GM), "GM", false, null, true)
                .SetGameMode(CustomGameMode.Standard);

            // Impostor
            BountyHunter.SetupCustomOption();
            SerialKiller.SetupCustomOption();
            // SetupRoleOptions(1200, CustomRoles.ShapeMaster);
            // ShapeMasterShapeshiftDuration = CustomOption.Create(1210, Color.white, "ShapeMasterShapeshiftDuration", 10, 1, 1000, 1, CustomRoleSpawnChances[CustomRoles.ShapeMaster]);
            SetupRoleOptions(1300, TabGroup.ImpostorRoles, CustomRoles.Vampire);
            VampireKillDelay = FloatOptionItem.Create(1310, "VampireKillDelay", new(1f, 1000f, 1f), 10f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Vampire])
                .SetValueFormat(OptionFormat.Seconds);
            SetupRoleOptions(1400, TabGroup.ImpostorRoles, CustomRoles.Warlock);
            Witch.SetupCustomOption();
            SetupRoleOptions(1600, TabGroup.ImpostorRoles, CustomRoles.Mafia);
            FireWorks.SetupCustomOption();
            Sniper.SetupCustomOption();
            SetupRoleOptions(2000, TabGroup.ImpostorRoles, CustomRoles.Puppeteer);
            Mare.SetupCustomOption();
            TimeThief.SetupCustomOption();
            EvilTracker.SetupCustomOption();

            DefaultShapeshiftCooldown = FloatOptionItem.Create(5011, "DefaultShapeshiftCooldown", new(5f, 999f, 5f), 15f, TabGroup.ImpostorRoles, false)
                .SetValueFormat(OptionFormat.Seconds);
            CanMakeMadmateCount = OptionItem.Create(5012, TabGroup.ImpostorRoles, Utils.GetRoleColor(CustomRoles.Madmate), "CanMakeMadmateCount", 0, 0, 15, 1, null, true, format: OptionFormat.Times);

            // Madmate
            SetupRoleOptions(10000, TabGroup.ImpostorRoles, CustomRoles.Madmate);
            SetupRoleOptions(10100, TabGroup.ImpostorRoles, CustomRoles.MadGuardian);
            MadGuardianCanSeeWhoTriedToKill = OptionItem.Create(10110, TabGroup.ImpostorRoles, Color.white, "MadGuardianCanSeeWhoTriedToKill", false, CustomRoleSpawnChances[CustomRoles.MadGuardian]);
            //ID10120~10123を使用
            MadGuardianTasks = OverrideTasksData.Create(10120, TabGroup.ImpostorRoles, CustomRoles.MadGuardian);
            SetupRoleOptions(10200, TabGroup.ImpostorRoles, CustomRoles.MadSnitch);
            MadSnitchCanVent = OptionItem.Create(10210, TabGroup.ImpostorRoles, Color.white, "CanVent", false, CustomRoleSpawnChances[CustomRoles.MadSnitch]);
            MadSnitchCanAlsoBeExposedToImpostor = OptionItem.Create(10211, TabGroup.ImpostorRoles, Color.white, "MadSnitchCanAlsoBeExposedToImpostor", false, CustomRoleSpawnChances[CustomRoles.MadSnitch]);
            //ID10220~10223を使用
            MadSnitchTasks = OverrideTasksData.Create(10220, TabGroup.ImpostorRoles, CustomRoles.MadSnitch);
            // Madmate Common Options
            MadmateCanFixLightsOut = OptionItem.Create(15010, TabGroup.ImpostorRoles, Color.white, "MadmateCanFixLightsOut", false, null, true, false);
            MadmateCanFixComms = OptionItem.Create(15011, TabGroup.ImpostorRoles, Color.white, "MadmateCanFixComms", false);
            MadmateHasImpostorVision = OptionItem.Create(15012, TabGroup.ImpostorRoles, Color.white, "MadmateHasImpostorVision", false);
            MadmateCanSeeKillFlash = OptionItem.Create(15015, TabGroup.ImpostorRoles, Color.white, "MadmateCanSeeKillFlash", false);
            MadmateCanSeeOtherVotes = OptionItem.Create(15016, TabGroup.ImpostorRoles, Color.white, "MadmateCanSeeOtherVotes", false);
            MadmateCanSeeDeathReason = OptionItem.Create(15018, TabGroup.ImpostorRoles, Color.white, "MadmateCanSeeDeathReason", false);
            MadmateVentCooldown = FloatOptionItem.Create(15213, "MadmateVentCooldown", new(0f, 180f, 5f), 0f, TabGroup.ImpostorRoles, false)
                .SetValueFormat(OptionFormat.Seconds);
            MadmateVentMaxTime = FloatOptionItem.Create(15214, "MadmateVentMaxTime", new(0f, 180f, 5f), 0f, TabGroup.ImpostorRoles, false)
                .SetValueFormat(OptionFormat.Seconds);
            // Both
            SetupRoleOptions(30000, TabGroup.NeutralRoles, CustomRoles.Watcher);
            EvilWatcherChance = OptionItem.Create(30010, TabGroup.NeutralRoles, Color.white, "EvilWatcherChance", 0, 0, 100, 10, CustomRoleSpawnChances[CustomRoles.Watcher], format: OptionFormat.Percent);
            // Crewmate
            SetupRoleOptions(20000, TabGroup.CrewmateRoles, CustomRoles.Bait);
            SetupRoleOptions(20100, TabGroup.CrewmateRoles, CustomRoles.Lighter);
            LighterTaskCompletedVision = FloatOptionItem.Create(20110, "LighterTaskCompletedVision", new(0f, 5f, 0.25f), 2f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Lighter])
                .SetValueFormat(OptionFormat.Multiplier);
            LighterTaskCompletedDisableLightOut = OptionItem.Create(20111, TabGroup.CrewmateRoles, Color.white, "LighterTaskCompletedDisableLightOut", true, CustomRoleSpawnChances[CustomRoles.Lighter]);
            SetupRoleOptions(20200, TabGroup.CrewmateRoles, CustomRoles.Mayor);
            MayorAdditionalVote = OptionItem.Create(20210, TabGroup.CrewmateRoles, Color.white, "MayorAdditionalVote", 1, 1, 99, 1, CustomRoleSpawnChances[CustomRoles.Mayor], format: OptionFormat.Votes);
            MayorHasPortableButton = OptionItem.Create(20211, TabGroup.CrewmateRoles, Color.white, "MayorHasPortableButton", false, CustomRoleSpawnChances[CustomRoles.Mayor]);
            MayorNumOfUseButton = OptionItem.Create(20212, TabGroup.CrewmateRoles, Color.white, "MayorNumOfUseButton", 1, 1, 99, 1, MayorHasPortableButton, format: OptionFormat.Times);
            SabotageMaster.SetupCustomOption();
            Sheriff.SetupCustomOption();
            SetupRoleOptions(20500, TabGroup.CrewmateRoles, CustomRoles.Snitch);
            SnitchEnableTargetArrow = OptionItem.Create(20510, TabGroup.CrewmateRoles, Color.white, "SnitchEnableTargetArrow", false, CustomRoleSpawnChances[CustomRoles.Snitch]);
            SnitchCanGetArrowColor = OptionItem.Create(20511, TabGroup.CrewmateRoles, Color.white, "SnitchCanGetArrowColor", false, CustomRoleSpawnChances[CustomRoles.Snitch]);
            SnitchCanFindNeutralKiller = OptionItem.Create(20512, TabGroup.CrewmateRoles, Color.white, "SnitchCanFindNeutralKiller", false, CustomRoleSpawnChances[CustomRoles.Snitch]);
            //20520~20523を使用
            SnitchTasks = OverrideTasksData.Create(20520, TabGroup.CrewmateRoles, CustomRoles.Snitch);
            SetupRoleOptions(20600, TabGroup.CrewmateRoles, CustomRoles.SpeedBooster);
            SpeedBoosterUpSpeed = FloatOptionItem.Create(20610, "SpeedBoosterUpSpeed", new(0.1f, 0.5f, 0.1f), 0.3f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SpeedBooster])
                .SetValueFormat(OptionFormat.Multiplier);
            SpeedBoosterTaskTrigger = OptionItem.Create(20611, TabGroup.CrewmateRoles, Color.white, "SpeedBoosterTaskTrigger", 5f, 1f, 99f, 1f, CustomRoleSpawnChances[CustomRoles.SpeedBooster], format: OptionFormat.Pieces);
            SetupRoleOptions(20700, TabGroup.CrewmateRoles, CustomRoles.Doctor);
            DoctorTaskCompletedBatteryCharge = FloatOptionItem.Create(20710, "DoctorTaskCompletedBatteryCharge", new(0f, 10f, 1f), 5f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Doctor])
                .SetValueFormat(OptionFormat.Seconds);
            SetupRoleOptions(20800, TabGroup.CrewmateRoles, CustomRoles.Trapper);
            TrapperBlockMoveTime = FloatOptionItem.Create(20810, "TrapperBlockMoveTime", new(1f, 180f, 1f), 5f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Trapper])
                .SetValueFormat(OptionFormat.Seconds);
            SetupRoleOptions(20900, TabGroup.CrewmateRoles, CustomRoles.Dictator);
            SetupRoleOptions(21000, TabGroup.CrewmateRoles, CustomRoles.Seer);

            // Neutral
            SetupRoleOptions(50500, TabGroup.NeutralRoles, CustomRoles.Arsonist);
            ArsonistDouseTime = FloatOptionItem.Create(50510, "ArsonistDouseTime", new(1f, 10f, 1f), 3f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Arsonist])
                .SetValueFormat(OptionFormat.Seconds);
            ArsonistCooldown = FloatOptionItem.Create(50511, "Cooldown", new(5f, 100f, 1f), 10f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Arsonist])
                .SetValueFormat(OptionFormat.Seconds);
            SetupRoleOptions(50000, TabGroup.NeutralRoles, CustomRoles.Jester);
            SetupRoleOptions(50100, TabGroup.NeutralRoles, CustomRoles.Opportunist);
            SetupRoleOptions(50200, TabGroup.NeutralRoles, CustomRoles.Terrorist);
            CanTerroristSuicideWin = OptionItem.Create(50210, TabGroup.NeutralRoles, Color.white, "CanTerroristSuicideWin", false, CustomRoleSpawnChances[CustomRoles.Terrorist], false)
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
            #endregion

            KillFlashDuration = FloatOptionItem.Create(90000, "KillFlashDuration", new(0.1f, 0.45f, 0.05f), 0.3f, TabGroup.MainSettings, false)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);

            // HideAndSeek
            SetupRoleOptions(100000, TabGroup.MainSettings, CustomRoles.HASFox, CustomGameMode.HideAndSeek);
            SetupRoleOptions(100100, TabGroup.MainSettings, CustomRoles.HASTroll, CustomGameMode.HideAndSeek);
            AllowCloseDoors = OptionItem.Create(101000, TabGroup.MainSettings, Color.white, "AllowCloseDoors", false, null, true)
                .SetGameMode(CustomGameMode.HideAndSeek);
            KillDelay = FloatOptionItem.Create(101001, "HideAndSeekWaitingTime", new(0f, 180f, 5f), 10f, TabGroup.MainSettings, false)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.HideAndSeek);
            //IgnoreCosmetics = CustomOption.Create(101002, Color.white, "IgnoreCosmetics", false)
            //    .SetGameMode(CustomGameMode.HideAndSeek);
            IgnoreVent = OptionItem.Create(101003, TabGroup.MainSettings, Color.white, "IgnoreVent", false)
                .SetGameMode(CustomGameMode.HideAndSeek);

            // リアクターの時間制御
            SabotageTimeControl = OptionItem.Create(100800, TabGroup.MainSettings, Color.white, "SabotageTimeControl", false, null, true)
                .SetGameMode(CustomGameMode.Standard);
            PolusReactorTimeLimit = FloatOptionItem.Create(100801, "PolusReactorTimeLimit", new(1f, 60f, 1f), 30f, TabGroup.MainSettings, false).SetParent(SabotageTimeControl)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);
            AirshipReactorTimeLimit = FloatOptionItem.Create(100802, "AirshipReactorTimeLimit", new(1f, 90f, 1f), 60f, TabGroup.MainSettings, false).SetParent(SabotageTimeControl)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);

            // 停電の特殊設定
            LightsOutSpecialSettings = OptionItem.Create(101500, TabGroup.MainSettings, Color.white, "LightsOutSpecialSettings", false)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipViewingDeckLightsPanel = OptionItem.Create(101511, TabGroup.MainSettings, Color.white, "DisableAirshipViewingDeckLightsPanel", false, LightsOutSpecialSettings)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipGapRoomLightsPanel = OptionItem.Create(101512, TabGroup.MainSettings, Color.white, "DisableAirshipGapRoomLightsPanel", false, LightsOutSpecialSettings)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipCargoLightsPanel = OptionItem.Create(101513, TabGroup.MainSettings, Color.white, "DisableAirshipCargoLightsPanel", false, LightsOutSpecialSettings)
                .SetGameMode(CustomGameMode.Standard);

            // タスク無効化
            DisableTasks = OptionItem.Create(100300, TabGroup.MainSettings, Color.white, "DisableTasks", false, null, true)
                .SetGameMode(CustomGameMode.All);
            DisableSwipeCard = OptionItem.Create(100301, TabGroup.MainSettings, Color.white, "DisableSwipeCardTask", false, DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableSubmitScan = OptionItem.Create(100302, TabGroup.MainSettings, Color.white, "DisableSubmitScanTask", false, DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableUnlockSafe = OptionItem.Create(100303, TabGroup.MainSettings, Color.white, "DisableUnlockSafeTask", false, DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableUploadData = OptionItem.Create(100304, TabGroup.MainSettings, Color.white, "DisableUploadDataTask", false, DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableStartReactor = OptionItem.Create(100305, TabGroup.MainSettings, Color.white, "DisableStartReactorTask", false, DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableResetBreaker = OptionItem.Create(100306, TabGroup.MainSettings, Color.white, "DisableResetBreakerTask", false, DisableTasks)
                .SetGameMode(CustomGameMode.All);

            //デバイス無効化
            DisableDevices = OptionItem.Create(101200, TabGroup.MainSettings, Color.white, "DisableDevices", false, null, true)
                .SetGameMode(CustomGameMode.Standard);
            DisableSkeldDevices = OptionItem.Create(101210, TabGroup.MainSettings, Color.white, "DisableSkeldDevices", false, DisableDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableSkeldAdmin = OptionItem.Create(101211, TabGroup.MainSettings, Color.white, "DisableSkeldAdmin", false, DisableSkeldDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableSkeldCamera = OptionItem.Create(101212, TabGroup.MainSettings, Color.white, "DisableSkeldCamera", false, DisableSkeldDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableMiraHQDevices = OptionItem.Create(101220, TabGroup.MainSettings, Color.white, "DisableMiraHQDevices", false, DisableDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableMiraHQAdmin = OptionItem.Create(101221, TabGroup.MainSettings, Color.white, "DisableMiraHQAdmin", false, DisableMiraHQDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableMiraHQDoorLog = OptionItem.Create(101222, TabGroup.MainSettings, Color.white, "DisableMiraHQDoorLog", false, DisableMiraHQDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisablePolusDevices = OptionItem.Create(101230, TabGroup.MainSettings, Color.white, "DisablePolusDevices", false, DisableDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisablePolusAdmin = OptionItem.Create(101231, TabGroup.MainSettings, Color.white, "DisablePolusAdmin", false, DisablePolusDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisablePolusCamera = OptionItem.Create(101232, TabGroup.MainSettings, Color.white, "DisablePolusCamera", false, DisablePolusDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisablePolusVital = OptionItem.Create(101233, TabGroup.MainSettings, Color.white, "DisablePolusVital", false, DisablePolusDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipDevices = OptionItem.Create(101240, TabGroup.MainSettings, Color.white, "DisableAirshipDevices", false, DisableDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipCockpitAdmin = OptionItem.Create(101241, TabGroup.MainSettings, Color.white, "DisableAirshipCockpitAdmin", false, DisableAirshipDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipRecordsAdmin = OptionItem.Create(101242, TabGroup.MainSettings, Color.white, "DisableAirshipRecordsAdmin", false, DisableAirshipDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipCamera = OptionItem.Create(101243, TabGroup.MainSettings, Color.white, "DisableAirshipCamera", false, DisableAirshipDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipVital = OptionItem.Create(101244, TabGroup.MainSettings, Color.white, "DisableAirshipVital", false, DisableAirshipDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableDevicesIgnoreConditions = OptionItem.Create(101290, TabGroup.MainSettings, Color.white, "IgnoreConditions", false, DisableDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableDevicesIgnoreImpostors = OptionItem.Create(101291, TabGroup.MainSettings, Color.white, "IgnoreImpostors", false, DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.Standard);
            DisableDevicesIgnoreMadmates = OptionItem.Create(101292, TabGroup.MainSettings, Color.white, "IgnoreMadmates", false, DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.Standard);
            DisableDevicesIgnoreNeutrals = OptionItem.Create(101293, TabGroup.MainSettings, Color.white, "IgnoreNeutrals", false, DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.Standard);
            DisableDevicesIgnoreCrewmates = OptionItem.Create(101294, TabGroup.MainSettings, Color.white, "IgnoreCrewmates", false, DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.Standard);
            DisableDevicesIgnoreAfterAnyoneDied = OptionItem.Create(101295, TabGroup.MainSettings, Color.white, "IgnoreAfterAnyoneDied", false, DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.Standard);

            // ランダムマップ
            RandomMapsMode = OptionItem.Create(100400, TabGroup.MainSettings, Color.white, "RandomMapsMode", false, null, true)
                .SetGameMode(CustomGameMode.All);
            AddedTheSkeld = OptionItem.Create(100401, TabGroup.MainSettings, Color.white, "AddedTheSkeld", false, RandomMapsMode)
                .SetGameMode(CustomGameMode.All);
            AddedMiraHQ = OptionItem.Create(100402, TabGroup.MainSettings, Color.white, "AddedMIRAHQ", false, RandomMapsMode)
                .SetGameMode(CustomGameMode.All);
            AddedPolus = OptionItem.Create(100403, TabGroup.MainSettings, Color.white, "AddedPolus", false, RandomMapsMode)
                .SetGameMode(CustomGameMode.All);
            AddedTheAirShip = OptionItem.Create(100404, TabGroup.MainSettings, Color.white, "AddedTheAirShip", false, RandomMapsMode)
                .SetGameMode(CustomGameMode.All);
            // MapDleks = CustomOption.Create(100405, TabGroup.MainSettings, Color.white, "AddedDleks", false, RandomMapMode)
            //     .SetGameMode(CustomGameMode.All);

            // ランダムスポーン
            RandomSpawn = OptionItem.Create(101300, TabGroup.MainSettings, Color.white, "RandomSpawn", false, isHeader: true)
                .SetGameMode(CustomGameMode.All);
            AirshipAdditionalSpawn = OptionItem.Create(101301, TabGroup.MainSettings, Color.white, "AirshipAdditionalSpawn", false, RandomSpawn)
                .SetGameMode(CustomGameMode.All);

            // ボタン回数同期
            SyncButtonMode = OptionItem.Create(100200, TabGroup.MainSettings, Color.white, "SyncButtonMode", false, null, true)
                .SetGameMode(CustomGameMode.Standard);
            SyncedButtonCount = OptionItem.Create(100201, TabGroup.MainSettings, Color.white, "SyncedButtonCount", 10, 0, 100, 1, SyncButtonMode, format: OptionFormat.Times)
                .SetGameMode(CustomGameMode.Standard);

            // 投票モード
            VoteMode = OptionItem.Create(100500, TabGroup.MainSettings, Color.white, "VoteMode", false, null, true)
                .SetGameMode(CustomGameMode.Standard);
            WhenSkipVote = StringOptionItem.Create(100510, "WhenSkipVote", voteModes[0..3], 0, TabGroup.MainSettings, false).SetParent(VoteMode)
                .SetGameMode(CustomGameMode.Standard);
            WhenSkipVoteIgnoreFirstMeeting = OptionItem.Create(100511, TabGroup.MainSettings, Color.white, "WhenSkipVoteIgnoreFirstMeeting", false, WhenSkipVote)
                .SetGameMode(CustomGameMode.Standard);
            WhenSkipVoteIgnoreNoDeadBody = OptionItem.Create(100512, TabGroup.MainSettings, Color.white, "WhenSkipVoteIgnoreNoDeadBody", false, WhenSkipVote)
                .SetGameMode(CustomGameMode.Standard);
            WhenSkipVoteIgnoreEmergency = OptionItem.Create(100513, TabGroup.MainSettings, Color.white, "WhenSkipVoteIgnoreEmergency", false, WhenSkipVote)
                .SetGameMode(CustomGameMode.Standard);
            WhenNonVote = StringOptionItem.Create(100520, "WhenNonVote", voteModes, 0, TabGroup.MainSettings, false).SetParent(VoteMode)
                .SetGameMode(CustomGameMode.Standard);
            WhenTie = StringOptionItem.Create(100530, "WhenTie", tieModes, 0, TabGroup.MainSettings, false).SetParent(VoteMode)
                .SetGameMode(CustomGameMode.Standard);

            // 全員生存時の会議時間
            AllAliveMeeting = OptionItem.Create(100900, TabGroup.MainSettings, Color.white, "AllAliveMeeting", false);
            AllAliveMeetingTime = FloatOptionItem.Create(100901, "AllAliveMeetingTime", new(1f, 300f, 1f), 10f, TabGroup.MainSettings, false).SetParent(AllAliveMeeting)
                .SetValueFormat(OptionFormat.Seconds);

            // 生存人数ごとの緊急会議
            AdditionalEmergencyCooldown = OptionItem.Create(101400, TabGroup.MainSettings, Color.white, "AdditionalEmergencyCooldown", false);
            AdditionalEmergencyCooldownThreshold = OptionItem.Create(101401, TabGroup.MainSettings, Color.white, "AdditionalEmergencyCooldownThreshold", 1, 1, 15, 1, AdditionalEmergencyCooldown, format: OptionFormat.Players);
            AdditionalEmergencyCooldownTime = FloatOptionItem.Create(101402, "AdditionalEmergencyCooldownTime", new(1f, 60f, 1f), 1f, TabGroup.MainSettings, false).SetParent(AdditionalEmergencyCooldown)
                .SetValueFormat(OptionFormat.Seconds);

            // 転落死
            LadderDeath = OptionItem.Create(101100, TabGroup.MainSettings, Color.white, "LadderDeath", false, null, true);
            LadderDeathChance = StringOptionItem.Create(101110, "LadderDeathChance", rates[1..], 0, TabGroup.MainSettings, false).SetParent(LadderDeath);

            // 通常モードでかくれんぼ用
            StandardHAS = OptionItem.Create(100700, TabGroup.MainSettings, Color.white, "StandardHAS", false, null, true)
                .SetGameMode(CustomGameMode.Standard);
            StandardHASWaitingTime = FloatOptionItem.Create(100701, "StandardHASWaitingTime", new(0f, 180f, 2.5f), 10f, TabGroup.MainSettings, false).SetParent(StandardHAS)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);

            // その他
            FixFirstKillCooldown = OptionItem.Create(900_000, TabGroup.MainSettings, Color.white, "FixFirstKillCooldown", false, null, true)
                .SetGameMode(CustomGameMode.All);
            DisableTaskWin = OptionItem.Create(900_001, TabGroup.MainSettings, Color.white, "DisableTaskWin", false)
                .SetGameMode(CustomGameMode.All);
            NoGameEnd = OptionItem.Create(900_002, TabGroup.MainSettings, Color.white, "NoGameEnd", false)
                .SetGameMode(CustomGameMode.All);
            GhostCanSeeOtherRoles = OptionItem.Create(900_010, TabGroup.MainSettings, Color.white, "GhostCanSeeOtherRoles", true)
                .SetGameMode(CustomGameMode.All);
            GhostCanSeeOtherVotes = OptionItem.Create(900_011, TabGroup.MainSettings, Color.white, "GhostCanSeeOtherVotes", true)
                .SetGameMode(CustomGameMode.All);
            GhostCanSeeDeathReason = OptionItem.Create(900_014, TabGroup.MainSettings, Color.white, "GhostCanSeeDeathReason", false)
                .SetGameMode(CustomGameMode.All);
            GhostIgnoreTasks = OptionItem.Create(900_012, TabGroup.MainSettings, Color.white, "GhostIgnoreTasks", false)
                .SetGameMode(CustomGameMode.All);
            CommsCamouflage = OptionItem.Create(900_013, TabGroup.MainSettings, Color.white, "CommsCamouflage", false)
                .SetGameMode(CustomGameMode.All);

            // プリセット対象外
            AutoDisplayLastResult = OptionItem.Create(1_000_000, TabGroup.MainSettings, Color.white, "AutoDisplayLastResult", true, null, true)
                .SetGameMode(CustomGameMode.All);
            SuffixMode = StringOptionItem.Create(1_000_001, "SuffixMode", suffixModes, 0, TabGroup.MainSettings, true)
                .SetGameMode(CustomGameMode.All);
            HideGameSettings = OptionItem.Create(1_000_002, TabGroup.MainSettings, Color.white, "HideGameSettings", false)
                .SetGameMode(CustomGameMode.All);
            ColorNameMode = OptionItem.Create(1_000_003, TabGroup.MainSettings, Color.white, "ColorNameMode", false)
                .SetGameMode(CustomGameMode.All);
            ChangeNameToRoleInfo = OptionItem.Create(1_000_004, TabGroup.MainSettings, Color.white, "ChangeNameToRoleInfo", true)
                .SetGameMode(CustomGameMode.All);
            RoleAssigningAlgorithm = StringOptionItem.Create(1_000_005, "RoleAssigningAlgorithm", RoleAssigningAlgorithms, 0, TabGroup.MainSettings, true)
                .SetGameMode(CustomGameMode.All)
                .RegisterUpdateValueEvent(
                    (object obj, OptionItem.UpdateValueEventArgs args) => IRandom.SetInstanceById(args.CurrentValue)
                );

            DebugModeManager.SetupCustomOption();

            IsLoaded = true;
        }

        public static void SetupRoleOptions(int id, TabGroup tab, CustomRoles role, CustomGameMode customGameMode = CustomGameMode.Standard)
        {
            var spawnOption = StringOptionItem.Create(id, role.ToString(), ratesZeroOne, 0, tab, false).SetColor(Utils.GetRoleColor(role))
                .SetGameMode(customGameMode);
            var countOption = OptionItem.Create(id + 1, tab, Color.white, "Maximum", 1, 1, 15, 1, spawnOption, false, format: OptionFormat.Players)
                .HiddenOnDisplay(true)
                .SetGameMode(customGameMode);

            CustomRoleSpawnChances.Add(role, spawnOption);
            CustomRoleCounts.Add(role, countOption);
        }
        private static void SetupLoversRoleOptionsToggle(int id, CustomGameMode customGameMode = CustomGameMode.Standard)
        {
            var role = CustomRoles.Lovers;
            var spawnOption = StringOptionItem.Create(id, role.ToString(), ratesZeroOne, 0, TabGroup.Addons, false).SetColor(Utils.GetRoleColor(role))
                .SetGameMode(customGameMode);

            var countOption = OptionItem.Create(id + 1, TabGroup.Addons, Color.white, "NumberOfLovers", 2, 1, 15, 1, spawnOption, false, true)
                .HiddenOnDisplay(false)
                .SetGameMode(customGameMode);

            CustomRoleSpawnChances.Add(role, spawnOption);
            CustomRoleCounts.Add(role, countOption);
        }
        public static void SetupSingleRoleOptions(int id, TabGroup tab, CustomRoles role, int count, CustomGameMode customGameMode = CustomGameMode.Standard)
        {
            var spawnOption = StringOptionItem.Create(id, role.ToString(), ratesZeroOne, 0, tab, false).SetColor(Utils.GetRoleColor(role))
                .SetGameMode(customGameMode);
            // 初期値,最大値,最小値が同じで、stepが0のどうやっても変えることができない個数オプション
            var countOption = OptionItem.Create(id + 1, tab, Color.white, "Maximum", count, count, count, count, spawnOption, false, true)
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
            public OptionItem doOverride;
            public OptionItem assignCommonTasks;
            public OptionItem numLongTasks;
            public OptionItem numShortTasks;

            public OverrideTasksData(int idStart, TabGroup tab, CustomRoles role)
            {
                this.IdStart = idStart;
                this.Role = role;
                Dictionary<string, string> replacementDic = new() { { "%role%", Utils.GetRoleName(role) } };
                doOverride = OptionItem.Create(idStart++, tab, Color.white, "doOverride", false, CustomRoleSpawnChances[role], false, false, OptionFormat.None, replacementDic);
                assignCommonTasks = OptionItem.Create(idStart++, tab, Color.white, "assignCommonTasks", true, doOverride, false, false, OptionFormat.None, replacementDic);
                numLongTasks = OptionItem.Create(idStart++, tab, Color.white, "roleLongTasksNum", 3, 0, 99, 1, doOverride, false, false, OptionFormat.Pieces, replacementDic);
                numShortTasks = OptionItem.Create(idStart++, tab, Color.white, "roleShortTasksNum", 3, 0, 99, 1, doOverride, false, false, OptionFormat.Pieces, replacementDic);

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