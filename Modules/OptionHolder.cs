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
            => CustomGameMode.Standard;

        public static readonly string[] gameModes =
        {
            "Standard"
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
        public static Dictionary<CustomRoles, IntegerOptionItem> CustomAdtRoleSpawnRate;
        public static readonly string[] rates =
        {
            "Rate0",  "Rate5",  "Rate10", "Rate20", "Rate30", "Rate40",
            "Rate50", "Rate60", "Rate70", "Rate80", "Rate90", "Rate100",
        };
        public static readonly string[] ratesZeroOne =
        {
            "RoleOff", /*"Rate10", "Rate20", "Rate30", "Rate40", "Rate50",
            "Rate60", "Rate70", "Rate80", "Rate90", */"RoleRate",
        };
        public static readonly string[] ratesToggle =
        {
            "RoleOff", "RoleRate", "RoleOn"
        };
        public static readonly string[] CheatResponsesName =
        {
            "Ban", "Kick", "NoticeMe","NoticeEveryone"
        };

        // 各役職の詳細設定
        public static OptionItem EnableGM;
        public static float DefaultKillCooldown = Main.NormalOptions?.KillCooldown ?? 20;

        public static OptionItem KillFlashDuration;
        public static OptionItem SendCodeToQQ;
        public static OptionItem SendCodeMinPlayer;
        public static OptionItem DisableVanillaRoles;
        public static OptionItem ConfirmEjections;
        public static OptionItem ConfirmEjectionsNK;
        public static OptionItem ConfirmEjectionsNonNK;
        public static OptionItem ConfirmEjectionsNKAsImp;
        public static OptionItem ConfirmEjectionsRoles;
        public static OptionItem ShowImpRemainOnEject;
        public static OptionItem ShowNKRemainOnEject;
        public static OptionItem CheatResponses;

        public static OptionItem NeutralRolesMinPlayer;
        public static OptionItem NeutralRolesMaxPlayer;

        public static OptionItem DefaultShapeshiftCooldown;
        public static OptionItem ImpKnowAlliesRole;

        public static OptionItem EGCanGuessImp;
        public static OptionItem EGCanGuessAdt;
        public static OptionItem EGCanGuessTime;
        public static OptionItem EGTryHideMsg;
        public static OptionItem WarlockCanKillAllies;
        public static OptionItem WarlockCanKillSelf;
        public static OptionItem ScavengerKillCooldown;
        public static OptionItem ZombieKillCooldown;
        public static OptionItem ZombieSpeedReduce;
        
        public static OptionItem EvilWatcherChance;
        public static OptionItem GGCanGuessCrew;
        public static OptionItem GGCanGuessAdt;
        public static OptionItem GGCanGuessTime;
        public static OptionItem GGTryHideMsg;
        public static OptionItem LuckeyProbability;
        public static OptionItem MayorAdditionalVote;
        public static OptionItem MayorHasPortableButton;
        public static OptionItem MayorNumOfUseButton;
        public static OptionItem MayorHideVote;
        public static OptionItem DoctorTaskCompletedBatteryCharge;
        public static OptionItem SpeedBoosterUpSpeed;
        public static OptionItem SpeedBoosterTimes;
        public static OptionItem TrapperBlockMoveTime;
        public static OptionItem DetectiveCanknowKiller;
        public static OptionItem TransporterTeleportMax;
        public static OptionItem CanTerroristSuicideWin;
        public static OptionItem ArsonistDouseTime;
        public static OptionItem ArsonistCooldown;
        public static OptionItem JesterCanUseButton;
        public static OptionItem NotifyGodAlive;
        public static OptionItem MarioVentNumWin;
        public static OptionItem OKKillCooldown;

        public static OptionItem ParanoiaVentCooldown;
        public static OptionItem ParanoiaNumOfUseButton;
        public static OptionItem PsychicCanSeeNum;
        public static OptionItem PsychicFresh;
        public static OptionItem CkshowEvil;
        public static OptionItem NBshowEvil;
        public static OptionItem NEshowEvil;
        public static OptionItem ImpKnowCyberStarDead;
        public static OptionItem NeutralKnowCyberStarDead;
        public static OptionItem EveryOneKnowSuperStar;
        public static OptionItem HackUsedMaxTime;
        public static OptionItem MNKillCooldown;
        public static OptionItem HackKillCooldown;
        public static OptionItem MafiaCanKillNum;
        public static OptionItem SansDefaultKillCooldown;
        public static OptionItem SansReduceKillCooldown;
        public static OptionItem SansMinKillCooldown;
        public static OptionItem BomberRadius;
        public static OptionItem FlashWhenTrapBoobyTrap;

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
        public static OverrideTasksData TransporterTasks;
        public static OverrideTasksData MadSnitchTasks;

        // その他
        public static OptionItem FixFirstKillCooldown;
        public static OptionItem DisableTaskWin;
        public static OptionItem GhostCanSeeOtherRoles;
        public static OptionItem GhostCanSeeOtherVotes;
        public static OptionItem GhostCanSeeDeathReason;
        public static OptionItem GhostIgnoreTasks;
        public static OptionItem CommsCamouflage;
        public static OptionItem DisableReportWhenCC;

        // プリセット対象外
        public static OptionItem NoGameEnd;
        public static OptionItem AutoDisplayLastResult;
        public static OptionItem SuffixMode;
        public static OptionItem HideGameSettings;
        public static OptionItem ColorNameMode;
        public static OptionItem ChangeNameToRoleInfo;
        public static OptionItem RoleAssigningAlgorithm;
        public static OptionItem EndWhenPlayerBug;

        public static OptionItem AutoKickStart;
        public static OptionItem AutoKickStartAsBan;
        public static OptionItem AutoKickStartTimes;
        public static OptionItem AutoKickStopWords;
        public static OptionItem AutoKickStopWordsAsBan;
        public static OptionItem AutoKickStopWordsTimes;
        public static OptionItem KickAndroidPlayer;
        public static OptionItem KickNonTOHEPlayer;
        public static OptionItem ApplyDenyNameList;
        public static OptionItem KickPlayerFriendCodeNotExist;
        public static OptionItem ApplyBanList;
        public static OptionItem AutoWarnStopWords;

        public static OptionItem DIYGameSettings;
        public static OptionItem PlayerCanSerColor;

        //Add-Ons
        public static OptionItem NoLimitAddonsNum;
        public static OptionItem BewilderVision;
        public static OptionItem FlashmanSpeed;
        public static OptionItem LoverSpawnChances;
        public static OptionItem LoverSuicide;
        public static OptionItem LoverHasNoTask;

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
            "RoleAssigningAlgorithm.MersenneTwister",
        };
        public static SuffixModes GetSuffixMode()
        {
            return (SuffixModes)SuffixMode.GetValue();
        }



        public static int SnitchExposeTaskLeft = 1;

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

        public static int GetRoleSpawnMode(CustomRoles role)
        {
            var mode = CustomRoleSpawnChances.TryGetValue(role, out var sc) ? sc.GetChance() : 0;
            return mode switch
            {
                0 => 0,
                1 => 1,
                2 => 2,
                100 => 1,
                _ => 1,
            };
        }
        public static int GetRoleCount(CustomRoles role)
        {
            var mode = GetRoleSpawnMode(role);
            return mode is 0  ? 0 : CustomRoleCounts.TryGetValue(role, out var option) ? option.GetInt() : roleCounts[role];
        }
        public static float GetRoleChance(CustomRoles role)
        {
            return CustomRoleSpawnChances.TryGetValue(role, out var option) ? option.GetValue()/* / 10f */ : roleSpawnChances[role];
        }
        public static void Load()
        {
            if (IsLoaded) return;
            // 预设
            _ = PresetOptionItem.Create(0, TabGroup.SystemSettings)
                .SetColor(new Color32(204, 204, 0, 255))
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All);

            // 游戏模式
            GameMode = StringOptionItem.Create(1, "GameMode", gameModes, 0, TabGroup.GameSettings, false)
                .SetHidden(true)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All);

            #region 职业详细设置
            CustomRoleCounts = new();
            CustomRoleSpawnChances = new();
            CustomAdtRoleSpawnRate = new();

            // 各职业的总体设定
            ImpKnowAlliesRole = BooleanOptionItem.Create(900045, "ImpKnowAlliesRole", true, TabGroup.ImpostorRoles, false)
               .SetHeader(true);
            DefaultShapeshiftCooldown = FloatOptionItem.Create(5011, "DefaultShapeshiftCooldown", new(5f, 999f, 5f), 15f, TabGroup.ImpostorRoles, false)
                .SetValueFormat(OptionFormat.Seconds);

            NeutralRolesMinPlayer = IntegerOptionItem.Create(505007, "NeutralRolesMinPlayer", new(0, 15, 1), 0, TabGroup.NeutralRoles, false)
                .SetHeader(true)
                .SetValueFormat(OptionFormat.Players);
            NeutralRolesMaxPlayer = IntegerOptionItem.Create(505009, "NeutralRolesMaxPlayer", new(0, 15, 1), 0, TabGroup.NeutralRoles, false)
                .SetValueFormat(OptionFormat.Players);

            // GM
            EnableGM = BooleanOptionItem.Create(100, "GM", false, TabGroup.GameSettings, false)
                .SetColor(Utils.GetRoleColor(CustomRoles.GM))
                .SetHeader(true)
                .SetGameMode(CustomGameMode.Standard);

            // Impostor
            SetupRoleOptions(901065, TabGroup.ImpostorRoles, CustomRoles.EvilGuesser);
            EGCanGuessTime = IntegerOptionItem.Create(901067, "GuesserCanGuessTimes", new(1, 15, 1), 15, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.EvilGuesser])
                .SetValueFormat(OptionFormat.Times);
            EGCanGuessImp = BooleanOptionItem.Create(901069, "EGCanGuessImp", false, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.EvilGuesser]);
            EGCanGuessAdt = BooleanOptionItem.Create(901073, "EGCanGuessAdt", false, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.EvilGuesser]);
            EGTryHideMsg = BooleanOptionItem.Create(901071, "GuesserTryHideMsg", false, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.EvilGuesser])
                .SetColor(Color.green);
            BountyHunter.SetupCustomOption();
            SerialKiller.SetupCustomOption();
            Vampire.SetupCustomOption();
            SetupRoleOptions(1400, TabGroup.ImpostorRoles, CustomRoles.Warlock);
            WarlockCanKillAllies = BooleanOptionItem.Create(901406, "WarlockCanKillAllies", true, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Warlock]);
            WarlockCanKillSelf = BooleanOptionItem.Create(901408, "WarlockCanKillSelf", false, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Warlock]);
            SetupRoleOptions(901455, TabGroup.ImpostorRoles, CustomRoles.Assassin);
            SetupRoleOptions(901585, TabGroup.ImpostorRoles, CustomRoles.Hacker);
            HackKillCooldown = FloatOptionItem.Create(901587, "KillCooldown", new(5f, 999f, 5f), 40f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Hacker])
                .SetValueFormat(OptionFormat.Seconds);
            HackUsedMaxTime = IntegerOptionItem.Create(901589, "HackUsedMaxTime", new(1, 15, 1), 3, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Hacker])
                .SetValueFormat(OptionFormat.Times);
            SetupRoleOptions(905520, TabGroup.ImpostorRoles, CustomRoles.Scavenger);
            ScavengerKillCooldown = FloatOptionItem.Create(905522, "KillCooldown", new(10f, 60f, 2.5f), 40f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Scavenger])
                .SetValueFormat(OptionFormat.Seconds);
            SetupRoleOptions(901590, TabGroup.ImpostorRoles, CustomRoles.Miner);
            SetupRoleOptions(901595, TabGroup.ImpostorRoles, CustomRoles.Escapee);
            Witch.SetupCustomOption();
            SetupRoleOptions(1600, TabGroup.ImpostorRoles, CustomRoles.Mafia);
            MafiaCanKillNum = IntegerOptionItem.Create(901615, "MafiaCanKillNum", new(0, 15, 1), 1, TabGroup.ImpostorRoles,false).SetParent(CustomRoleSpawnChances[CustomRoles.Mafia])
                .SetValueFormat(OptionFormat.Players);
            FireWorks.SetupCustomOption();
            Sniper.SetupCustomOption();
            SetupRoleOptions(2000, TabGroup.ImpostorRoles, CustomRoles.Puppeteer);
            Mare.SetupCustomOption();
            //TimeThief.SetupCustomOption();
            EvilTracker.SetupCustomOption();
            AntiAdminer.SetupCustomOption();
            SetupRoleOptions(902055, TabGroup.ImpostorRoles, CustomRoles.Sans);
            SansDefaultKillCooldown = FloatOptionItem.Create(902057, "SansDefaultKillCooldown", new(2.5f, 900f, 2.5f), 65f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sans])
                .SetValueFormat(OptionFormat.Seconds);
            SansReduceKillCooldown = FloatOptionItem.Create(902059, "SansReduceKillCooldown", new(0f, 120f, 2.5f), 15f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sans])
                .SetValueFormat(OptionFormat.Seconds);
            SansMinKillCooldown = FloatOptionItem.Create(902061, "SansMinKillCooldown", new(0f, 900f, 2.5f), 2.5f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sans])
                .SetValueFormat(OptionFormat.Seconds);
            SetupRoleOptions(902135, TabGroup.ImpostorRoles, CustomRoles.Bomber);
            BomberRadius = FloatOptionItem.Create(902137, "BomberRadius", new(0.5f, 5f, 0.5f), 2f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bomber])
                .SetValueFormat(OptionFormat.Multiplier);

            // Crewmate
            SetupRoleOptions(102255, TabGroup.CrewmateRoles, CustomRoles.NiceGuesser);
            GGCanGuessTime = IntegerOptionItem.Create(102257, "GuesserCanGuessTimes", new(1, 15, 1), 15, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NiceGuesser])
                .SetValueFormat(OptionFormat.Times);
            GGCanGuessCrew = BooleanOptionItem.Create(102259, "GGCanGuessCrew", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NiceGuesser]);
            GGCanGuessAdt = BooleanOptionItem.Create(102263, "GGCanGuessAdt", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NiceGuesser]);
            GGTryHideMsg = BooleanOptionItem.Create(102261, "GuesserTryHideMsg", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.NiceGuesser])
                .SetColor(Color.green);
            SetupRoleOptions(20000, TabGroup.CrewmateRoles, CustomRoles.Bait);
            SetupRoleOptions(1020195, TabGroup.CrewmateRoles, CustomRoles.Luckey);
            LuckeyProbability = IntegerOptionItem.Create(1020197, "LuckeyProbability", new(0, 100, 5), 50, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Luckey])
                .SetValueFormat(OptionFormat.Percent);
            SetupRoleOptions(1020095, TabGroup.CrewmateRoles, CustomRoles.Needy);
            SetupRoleOptions(8020165, TabGroup.CrewmateRoles, CustomRoles.SuperStar);
            EveryOneKnowSuperStar = BooleanOptionItem.Create(8020168, "EveryOneKnowSuperStar", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SuperStar]);
            SetupRoleOptions(8020176, TabGroup.CrewmateRoles, CustomRoles.CyberStar);
            ImpKnowCyberStarDead = BooleanOptionItem.Create(8020178, "ImpKnowCyberStarDead", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.CyberStar]);
            NeutralKnowCyberStarDead = BooleanOptionItem.Create(8020180, "NeutralKnowCyberStarDead", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.CyberStar]);
            SetupRoleOptions(20200, TabGroup.CrewmateRoles, CustomRoles.Mayor);
            MayorAdditionalVote = IntegerOptionItem.Create(20210, "MayorAdditionalVote", new(1, 99, 1), 3, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mayor])
                .SetValueFormat(OptionFormat.Votes);
            MayorHasPortableButton = BooleanOptionItem.Create(20211, "MayorHasPortableButton", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mayor]);
            MayorNumOfUseButton = IntegerOptionItem.Create(20212, "MayorNumOfUseButton", new(1, 99, 1), 1, TabGroup.CrewmateRoles, false).SetParent(MayorHasPortableButton)
                .SetValueFormat(OptionFormat.Times);
            MayorHideVote = BooleanOptionItem.Create(20213, "MayorHideVote", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mayor]);
            SabotageMaster.SetupCustomOption();
            Sheriff.SetupCustomOption();
            SetupRoleOptions(8020490, TabGroup.CrewmateRoles, CustomRoles.Paranoia);
            ParanoiaNumOfUseButton = IntegerOptionItem.Create(8020493, "ParanoiaNumOfUseButton", new(1, 99, 1), 3, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Paranoia])
                .SetValueFormat(OptionFormat.Times);
            SetupRoleOptions(8020450, TabGroup.CrewmateRoles, CustomRoles.Psychic);
            PsychicCanSeeNum = IntegerOptionItem.Create(8020452, "PsychicCanSeeNum", new(1, 15, 1), 3, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Psychic])
                .SetValueFormat(OptionFormat.Players);
            PsychicFresh = BooleanOptionItem.Create(8020456, "PsychicFresh", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Psychic]);
            CkshowEvil = BooleanOptionItem.Create(8020453, "CrewKillingRed", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Psychic]);
            NBshowEvil = BooleanOptionItem.Create(8020454, "NBareRed", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Psychic]);
            NEshowEvil = BooleanOptionItem.Create(800455, "NEareRed", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Psychic]);
            Snitch.SetupCustomOption();
            SetupRoleOptions(20600, TabGroup.CrewmateRoles, CustomRoles.SpeedBooster);
            SpeedBoosterUpSpeed = FloatOptionItem.Create(20610, "SpeedBoosterUpSpeed", new(0.1f, 1.0f, 0.1f), 0.2f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SpeedBooster])
                .SetValueFormat(OptionFormat.Multiplier);
            SpeedBoosterTimes = IntegerOptionItem.Create(20611, "SpeedBoosterTimes", new(1, 99, 1), 5, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.SpeedBooster])
                .SetValueFormat(OptionFormat.Times);
            SetupRoleOptions(20700, TabGroup.CrewmateRoles, CustomRoles.Doctor);
            DoctorTaskCompletedBatteryCharge = FloatOptionItem.Create(20710, "DoctorTaskCompletedBatteryCharge", new(0f, 10f, 1f), 5f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Doctor])
                .SetValueFormat(OptionFormat.Seconds);
            SetupRoleOptions(20800, TabGroup.CrewmateRoles, CustomRoles.Trapper);
            TrapperBlockMoveTime = FloatOptionItem.Create(20810, "TrapperBlockMoveTime", new(1f, 180f, 1f), 5f, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Trapper])
                .SetValueFormat(OptionFormat.Seconds);
            SetupRoleOptions(20900, TabGroup.CrewmateRoles, CustomRoles.Dictator);
            SetupRoleOptions(8021015, TabGroup.CrewmateRoles, CustomRoles.Detective);
            DetectiveCanknowKiller = BooleanOptionItem.Create(8021017, "DetectiveCanknowKiller", true, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Detective]);
            ChivalrousExpert.SetupCustomOption();
            SetupRoleOptions(8021115, TabGroup.CrewmateRoles, CustomRoles.Transporter);
            TransporterTeleportMax = IntegerOptionItem.Create(8021117, "TransporterTeleportMax", new(1, 99, 1), 5, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Transporter])
                .SetValueFormat(OptionFormat.Times);
            TransporterTasks = OverrideTasksData.Create(8021119, TabGroup.CrewmateRoles, CustomRoles.Transporter);

            // Neutral
            SetupRoleOptions(50500, TabGroup.NeutralRoles, CustomRoles.Arsonist);
            ArsonistDouseTime = FloatOptionItem.Create(50510, "ArsonistDouseTime", new(1f, 10f, 1f), 3f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Arsonist])
                .SetValueFormat(OptionFormat.Seconds);
            ArsonistCooldown = FloatOptionItem.Create(50511, "Cooldown", new(5f, 100f, 1f), 10f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Arsonist])
                .SetValueFormat(OptionFormat.Seconds);
            SetupRoleOptions(50000, TabGroup.NeutralRoles, CustomRoles.Jester);
            JesterCanUseButton = BooleanOptionItem.Create(6050007, "JesterCanUseButton", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Jester]);
            SetupRoleOptions(50100, TabGroup.NeutralRoles, CustomRoles.Opportunist);
            SetupRoleOptions(5050100, TabGroup.NeutralRoles, CustomRoles.OpportunistKiller);
            OKKillCooldown = FloatOptionItem.Create(5050105, "KillCooldown", new(5f, 999f, 5f), 35f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.OpportunistKiller])
                .SetValueFormat(OptionFormat.Seconds);
            SetupRoleOptions(50200, TabGroup.NeutralRoles, CustomRoles.Terrorist);
            CanTerroristSuicideWin = BooleanOptionItem.Create(50210, "CanTerroristSuicideWin", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Terrorist])
                .SetGameMode(CustomGameMode.Standard);
            //50220~50223を使用
            TerroristTasks = OverrideTasksData.Create(50220, TabGroup.NeutralRoles, CustomRoles.Terrorist);
            Executioner.SetupCustomOption();
            Jackal.SetupCustomOption();

            // Add-Ons
            NoLimitAddonsNum = BooleanOptionItem.Create(6050250, "NoLimitAddonsNum", false, TabGroup.Addons, false)
                .SetHeader(true)
                .SetColor(Color.cyan);
            SetupLoversRoleOptionsToggle(50300);
            SetupAdtRoleOptions(6050320, CustomRoles.Watcher, canSetNum: true);
            SetupAdtRoleOptions(6050330, CustomRoles.Flashman, canSetNum: true);
            FlashmanSpeed = FloatOptionItem.Create(6050335, "FlashmanSpeed", new(0.25f, 5f, 0.25f), 2.5f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Flashman])
                .SetValueFormat(OptionFormat.Multiplier);
            SetupAdtRoleOptions(6050340, CustomRoles.Lighter, canSetNum: true);
            SetupAdtRoleOptions(6050350, CustomRoles.Seer, canSetNum: true);
            SetupAdtRoleOptions(6050360, CustomRoles.Brakar, canSetNum: true);
            SetupAdtRoleOptions(6050370, CustomRoles.Oblivious, canSetNum: true);
            SetupAdtRoleOptions(6050380, CustomRoles.Bewilder, canSetNum: true);
            BewilderVision = FloatOptionItem.Create(6050383, "BewilderVision", new(0f, 5f, 0.05f), 0.65f, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Bewilder])
                .SetValueFormat(OptionFormat.Multiplier);

            //SetupAdtRoleOptions(6050360, CustomRoles.Madmate, canSetNum: true);
            LastImpostor.SetupCustomOption();

            // 乐子职业

            // 内鬼
            _ = BooleanOptionItem.Create(909090, "TabGroup.ImpostorRoles", false, TabGroup.OtherRoles, false)
                .SetHeader(true)
                .SetText(true);
            SetupRoleOptions(901635, TabGroup.OtherRoles, CustomRoles.Minimalism);
            MNKillCooldown = FloatOptionItem.Create(901638, "KillCooldown", new(2.5f, 999f, 2.5f), 10f, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Minimalism])
                .SetValueFormat(OptionFormat.Seconds);
            SetupRoleOptions(901790, TabGroup.OtherRoles, CustomRoles.Zombie);
            ZombieKillCooldown = FloatOptionItem.Create(901792, "KillCooldown", new(0f, 999f, 2.5f), 5f, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Zombie])
                .SetValueFormat(OptionFormat.Seconds);
            ZombieSpeedReduce = FloatOptionItem.Create(901794, "ZombieSpeedReduce", new(0.0f, 1.0f, 0.1f), 0.1f, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Zombie])
                .SetValueFormat(OptionFormat.Multiplier);
            SetupRoleOptions(902265, TabGroup.OtherRoles, CustomRoles.BoobyTrap);

            //// 船员
            //_ = BooleanOptionItem.Create(909092, "TabGroup.CrewmateRoles", false, TabGroup.OtherRoles, false)
            //    .SetHidden(true)s
            //    .SetHeader(true)
            //    .SetText(true);

            // 中立
            _ = BooleanOptionItem.Create(909094, "TabGroup.NeutralRoles", false, TabGroup.OtherRoles, false)
                .SetHeader(true)
                .SetText(true);
            SetupRoleOptions(5050965, TabGroup.OtherRoles, CustomRoles.God);
            NotifyGodAlive = BooleanOptionItem.Create(5050967, "NotifyGodAlive", true, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.God]);
            SetupRoleOptions(5050110, TabGroup.OtherRoles, CustomRoles.Mario);
            MarioVentNumWin = IntegerOptionItem.Create(5050112, "MarioVentNumWin", new(5, 900, 5), 55, TabGroup.OtherRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Mario])
                .SetValueFormat(OptionFormat.Times);

            // 副职
            _ = BooleanOptionItem.Create(909096, "TabGroup.Addons", false, TabGroup.OtherRoles, false)
                .SetHeader(true)
                .SetText(true);
            SetupAdtRoleOptions(6050310, CustomRoles.Ntr, tab: TabGroup.OtherRoles);
            

            #endregion

            #region 系统设置

            KickAndroidPlayer = BooleanOptionItem.Create(6090071, "KickAndroidPlayer", false, TabGroup.SystemSettings, false)
                .SetGameMode(CustomGameMode.All)
                .SetHeader(true);
            KickPlayerFriendCodeNotExist = BooleanOptionItem.Create(1_000_101, "KickPlayerFriendCodeNotExist", false, TabGroup.SystemSettings, true)
                .SetGameMode(CustomGameMode.All);
            KickNonTOHEPlayer = BooleanOptionItem.Create(6090073, "KickNonTOHEPlayer", true, TabGroup.SystemSettings, false)
                .SetGameMode(CustomGameMode.All);
            ApplyDenyNameList = BooleanOptionItem.Create(1_000_100, "ApplyDenyNameList", true, TabGroup.SystemSettings, true)
                .SetGameMode(CustomGameMode.All);
            ApplyBanList = BooleanOptionItem.Create(1_000_110, "ApplyBanList", true, TabGroup.SystemSettings, true)
                .SetGameMode(CustomGameMode.All);
            AutoKickStart = BooleanOptionItem.Create(1_000_010, "AutoKickStart", false, TabGroup.SystemSettings, false)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All);
            AutoKickStartTimes = IntegerOptionItem.Create(1_000_024, "AutoKickStartTimes", new(0, 99, 1), 1, TabGroup.SystemSettings, false).SetParent(AutoKickStart)
                .SetGameMode(CustomGameMode.All)
                .SetValueFormat(OptionFormat.Times);
            AutoKickStartAsBan = BooleanOptionItem.Create(1_000_026, "AutoKickStartAsBan", false, TabGroup.SystemSettings, false).SetParent(AutoKickStart)
                .SetGameMode(CustomGameMode.All);
            AutoKickStopWords = BooleanOptionItem.Create(1_000_011, "AutoKickStopWords", false, TabGroup.SystemSettings, false)
                .SetGameMode(CustomGameMode.All);
            AutoKickStopWordsTimes = IntegerOptionItem.Create(1_000_022, "AutoKickStopWordsTimes", new(0, 99, 1), 3, TabGroup.SystemSettings, false).SetParent(AutoKickStopWords)
                .SetGameMode(CustomGameMode.All)
                .SetValueFormat(OptionFormat.Times);
            AutoKickStopWordsAsBan = BooleanOptionItem.Create(1_000_028, "AutoKickStopWordsAsBan", false, TabGroup.SystemSettings, false).SetParent(AutoKickStopWords)
                .SetGameMode(CustomGameMode.All);
            AutoWarnStopWords = BooleanOptionItem.Create(1_000_012, "AutoWarnStopWords", false, TabGroup.SystemSettings, false)
                .SetGameMode(CustomGameMode.All);

            SendCodeToQQ = BooleanOptionItem.Create(6090065, "SendCodeToQQ", true, TabGroup.SystemSettings, false)
                .SetHeader(true)
                .SetColor(Color.cyan);
            SendCodeMinPlayer = IntegerOptionItem.Create(6090067, "SendCodeMinPlayer", new(3, 12, 1), 5, TabGroup.SystemSettings, false).SetParent(SendCodeToQQ)
                .SetValueFormat(OptionFormat.Players);

            EndWhenPlayerBug = BooleanOptionItem.Create(1_000_025, "EndWhenPlayerBug", true, TabGroup.SystemSettings, false)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All)
                .SetColor(Color.blue);

            CheatResponses = StringOptionItem.Create(6090121, "CheatResponses", CheatResponsesName, 0, TabGroup.SystemSettings, false)
                .SetHeader(true);

            AutoDisplayLastResult = BooleanOptionItem.Create(1_000_000, "AutoDisplayLastResult", true, TabGroup.SystemSettings, false)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All);
            
            SuffixMode = StringOptionItem.Create(1_000_001, "SuffixMode", suffixModes, 0, TabGroup.SystemSettings, true)
                .SetGameMode(CustomGameMode.All);
            HideGameSettings = BooleanOptionItem.Create(1_000_002, "HideGameSettings", false, TabGroup.SystemSettings, false)
                .SetGameMode(CustomGameMode.All);
            DIYGameSettings = BooleanOptionItem.Create(1_000_013, "DIYGameSettings", false, TabGroup.SystemSettings, false)
                .SetGameMode(CustomGameMode.All);
            PlayerCanSerColor = BooleanOptionItem.Create(1_000_014, "PlayerCanSerColor", false, TabGroup.SystemSettings, false)
                .SetGameMode(CustomGameMode.All);
            ColorNameMode = BooleanOptionItem.Create(1_000_003, "ColorNameMode", false, TabGroup.SystemSettings, false)
                .SetGameMode(CustomGameMode.All);
            ChangeNameToRoleInfo = BooleanOptionItem.Create(1_000_004, "ChangeNameToRoleInfo", false, TabGroup.SystemSettings, false)
                .SetGameMode(CustomGameMode.All);
            NoGameEnd = BooleanOptionItem.Create(900_002, "NoGameEnd", false, TabGroup.SystemSettings, false)
                .SetGameMode(CustomGameMode.All);
            RoleAssigningAlgorithm = StringOptionItem.Create(1_000_005, "RoleAssigningAlgorithm", RoleAssigningAlgorithms, 4, TabGroup.SystemSettings, true)
                .SetGameMode(CustomGameMode.All)
                .RegisterUpdateValueEvent(
                    (object obj, OptionItem.UpdateValueEventArgs args) => IRandom.SetInstanceById(args.CurrentValue)
                );

            DebugModeManager.SetupCustomOption();
            #endregion 

            #region 游戏设置
            DisableVanillaRoles = BooleanOptionItem.Create(6090069, "DisableVanillaRoles", true, TabGroup.GameSettings, false)
                .SetHeader(true);

            ConfirmEjections = BooleanOptionItem.Create(6090105, "ConfirmEjections", false, TabGroup.GameSettings, false)
                .SetHeader(true);
            ConfirmEjectionsNK = BooleanOptionItem.Create(6090107, "ConfirmEjectionsNK", true, TabGroup.GameSettings, false).SetParent(ConfirmEjections);
            ConfirmEjectionsNonNK = BooleanOptionItem.Create(6090109, "ConfirmEjectionsNonNK", true, TabGroup.GameSettings, false).SetParent(ConfirmEjections);
            ConfirmEjectionsNKAsImp = BooleanOptionItem.Create(6090111, "ConfirmEjectionsNKAsImp", false, TabGroup.GameSettings, false).SetParent(ConfirmEjections);
            ConfirmEjectionsRoles = BooleanOptionItem.Create(6090113, "ConfirmEjectionsRoles", true, TabGroup.GameSettings, false);
            ShowImpRemainOnEject = BooleanOptionItem.Create(6090115, "ShowImpRemainOnEject", true, TabGroup.GameSettings, false);
            ShowNKRemainOnEject = BooleanOptionItem.Create(6090119, "ShowNKRemainOnEject", true, TabGroup.GameSettings, false).SetParent(ShowImpRemainOnEject);

            KillFlashDuration = FloatOptionItem.Create(90000, "KillFlashDuration", new(0.1f, 0.45f, 0.05f), 0.2f, TabGroup.GameSettings, false)
                .SetHeader(true)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);

            // リアクターの時間制御
            SabotageTimeControl = BooleanOptionItem.Create(100800, "SabotageTimeControl", false, TabGroup.GameSettings, false)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.Standard);
            PolusReactorTimeLimit = FloatOptionItem.Create(100801, "PolusReactorTimeLimit", new(1f, 60f, 1f), 30f, TabGroup.GameSettings, false).SetParent(SabotageTimeControl)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);
            AirshipReactorTimeLimit = FloatOptionItem.Create(100802, "AirshipReactorTimeLimit", new(1f, 90f, 1f), 60f, TabGroup.GameSettings, false).SetParent(SabotageTimeControl)
                .SetValueFormat(OptionFormat.Seconds)
                .SetGameMode(CustomGameMode.Standard);

            // 停電の特殊設定
            LightsOutSpecialSettings = BooleanOptionItem.Create(101500, "LightsOutSpecialSettings", false, TabGroup.GameSettings, false)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipViewingDeckLightsPanel = BooleanOptionItem.Create(101511, "DisableAirshipViewingDeckLightsPanel", false, TabGroup.GameSettings, false).SetParent(LightsOutSpecialSettings)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipGapRoomLightsPanel = BooleanOptionItem.Create(101512, "DisableAirshipGapRoomLightsPanel", false, TabGroup.GameSettings, false).SetParent(LightsOutSpecialSettings)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipCargoLightsPanel = BooleanOptionItem.Create(101513, "DisableAirshipCargoLightsPanel", false, TabGroup.GameSettings, false).SetParent(LightsOutSpecialSettings)
                .SetGameMode(CustomGameMode.Standard);

            // タスク無効化
            DisableTasks = BooleanOptionItem.Create(100300, "DisableTasks", false, TabGroup.GameSettings, false)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All);
            DisableSwipeCard = BooleanOptionItem.Create(100301, "DisableSwipeCardTask", false, TabGroup.GameSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableSubmitScan = BooleanOptionItem.Create(100302, "DisableSubmitScanTask", false, TabGroup.GameSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableUnlockSafe = BooleanOptionItem.Create(100303, "DisableUnlockSafeTask", false, TabGroup.GameSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableUploadData = BooleanOptionItem.Create(100304, "DisableUploadDataTask", false, TabGroup.GameSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableStartReactor = BooleanOptionItem.Create(100305, "DisableStartReactorTask", false, TabGroup.GameSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);
            DisableResetBreaker = BooleanOptionItem.Create(100306, "DisableResetBreakerTask", false, TabGroup.GameSettings, false).SetParent(DisableTasks)
                .SetGameMode(CustomGameMode.All);

            //デバイス無効化
            DisableDevices = BooleanOptionItem.Create(101200, "DisableDevices", false, TabGroup.GameSettings, false)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.Standard);
            DisableSkeldDevices = BooleanOptionItem.Create(101210, "DisableSkeldDevices", false, TabGroup.GameSettings, false).SetParent(DisableDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableSkeldAdmin = BooleanOptionItem.Create(101211, "DisableSkeldAdmin", false, TabGroup.GameSettings, false).SetParent(DisableSkeldDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableSkeldCamera = BooleanOptionItem.Create(101212, "DisableSkeldCamera", false, TabGroup.GameSettings, false).SetParent(DisableSkeldDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableMiraHQDevices = BooleanOptionItem.Create(101220, "DisableMiraHQDevices", false, TabGroup.GameSettings, false).SetParent(DisableDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableMiraHQAdmin = BooleanOptionItem.Create(101221, "DisableMiraHQAdmin", false, TabGroup.GameSettings, false).SetParent(DisableMiraHQDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableMiraHQDoorLog = BooleanOptionItem.Create(101222, "DisableMiraHQDoorLog", false, TabGroup.GameSettings, false).SetParent(DisableMiraHQDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisablePolusDevices = BooleanOptionItem.Create(101230, "DisablePolusDevices", false, TabGroup.GameSettings, false).SetParent(DisableDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisablePolusAdmin = BooleanOptionItem.Create(101231, "DisablePolusAdmin", false, TabGroup.GameSettings, false).SetParent(DisablePolusDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisablePolusCamera = BooleanOptionItem.Create(101232, "DisablePolusCamera", false, TabGroup.GameSettings, false).SetParent(DisablePolusDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisablePolusVital = BooleanOptionItem.Create(101233, "DisablePolusVital", false, TabGroup.GameSettings, false).SetParent(DisablePolusDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipDevices = BooleanOptionItem.Create(101240, "DisableAirshipDevices", false, TabGroup.GameSettings, false).SetParent(DisableDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipCockpitAdmin = BooleanOptionItem.Create(101241, "DisableAirshipCockpitAdmin", false, TabGroup.GameSettings, false).SetParent(DisableAirshipDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipRecordsAdmin = BooleanOptionItem.Create(101242, "DisableAirshipRecordsAdmin", false, TabGroup.GameSettings, false).SetParent(DisableAirshipDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipCamera = BooleanOptionItem.Create(101243, "DisableAirshipCamera", false, TabGroup.GameSettings, false).SetParent(DisableAirshipDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableAirshipVital = BooleanOptionItem.Create(101244, "DisableAirshipVital", false, TabGroup.GameSettings, false).SetParent(DisableAirshipDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableDevicesIgnoreConditions = BooleanOptionItem.Create(101290, "IgnoreConditions", false, TabGroup.GameSettings, false).SetParent(DisableDevices)
                .SetGameMode(CustomGameMode.Standard);
            DisableDevicesIgnoreImpostors = BooleanOptionItem.Create(101291, "IgnoreImpostors", false, TabGroup.GameSettings, false).SetParent(DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.Standard);
            DisableDevicesIgnoreNeutrals = BooleanOptionItem.Create(101293, "IgnoreNeutrals", false, TabGroup.GameSettings, false).SetParent(DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.Standard);
            DisableDevicesIgnoreCrewmates = BooleanOptionItem.Create(101294, "IgnoreCrewmates", false, TabGroup.GameSettings, false).SetParent(DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.Standard);
            DisableDevicesIgnoreAfterAnyoneDied = BooleanOptionItem.Create(101295, "IgnoreAfterAnyoneDied", false, TabGroup.GameSettings, false).SetParent(DisableDevicesIgnoreConditions)
                .SetGameMode(CustomGameMode.Standard);

            // ランダムマップ
            RandomMapsMode = BooleanOptionItem.Create(100400, "RandomMapsMode", false, TabGroup.GameSettings, false)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All);
            AddedTheSkeld = BooleanOptionItem.Create(100401, "AddedTheSkeld", false, TabGroup.GameSettings, false).SetParent(RandomMapsMode)
                .SetGameMode(CustomGameMode.All);
            AddedMiraHQ = BooleanOptionItem.Create(100402, "AddedMIRAHQ", false, TabGroup.GameSettings, false).SetParent(RandomMapsMode)
                .SetGameMode(CustomGameMode.All);
            AddedPolus = BooleanOptionItem.Create(100403, "AddedPolus", false, TabGroup.GameSettings, false).SetParent(RandomMapsMode)
                .SetGameMode(CustomGameMode.All);
            AddedTheAirShip = BooleanOptionItem.Create(100404, "AddedTheAirShip", false, TabGroup.GameSettings, false).SetParent(RandomMapsMode)
                .SetGameMode(CustomGameMode.All);
            // MapDleks = CustomOption.Create(100405, Color.white, "AddedDleks", false, RandomMapMode)
            //     .SetGameMode(CustomGameMode.All);

            // ランダムスポーン
            RandomSpawn = BooleanOptionItem.Create(101300, "RandomSpawn", false, TabGroup.GameSettings, false)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All);
            AirshipAdditionalSpawn = BooleanOptionItem.Create(101301, "AirshipAdditionalSpawn", false, TabGroup.GameSettings, false).SetParent(RandomSpawn)
                .SetGameMode(CustomGameMode.All);

            // ボタン回数同期
            SyncButtonMode = BooleanOptionItem.Create(100200, "SyncButtonMode", false, TabGroup.GameSettings, false)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.Standard);
            SyncedButtonCount = IntegerOptionItem.Create(100201, "SyncedButtonCount", new(0, 100, 1), 10, TabGroup.GameSettings, false).SetParent(SyncButtonMode)
                .SetValueFormat(OptionFormat.Times)
                .SetGameMode(CustomGameMode.Standard);

            // 投票モード
            VoteMode = BooleanOptionItem.Create(100500, "VoteMode", false, TabGroup.GameSettings, false)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.Standard);
            WhenSkipVote = StringOptionItem.Create(100510, "WhenSkipVote", voteModes[0..3], 0, TabGroup.GameSettings, false).SetParent(VoteMode)
                .SetGameMode(CustomGameMode.Standard);
            WhenSkipVoteIgnoreFirstMeeting = BooleanOptionItem.Create(100511, "WhenSkipVoteIgnoreFirstMeeting", false, TabGroup.GameSettings, false).SetParent(WhenSkipVote)
                .SetGameMode(CustomGameMode.Standard);
            WhenSkipVoteIgnoreNoDeadBody = BooleanOptionItem.Create(100512, "WhenSkipVoteIgnoreNoDeadBody", false, TabGroup.GameSettings, false).SetParent(WhenSkipVote)
                .SetGameMode(CustomGameMode.Standard);
            WhenSkipVoteIgnoreEmergency = BooleanOptionItem.Create(100513, "WhenSkipVoteIgnoreEmergency", false, TabGroup.GameSettings, false).SetParent(WhenSkipVote)
                .SetGameMode(CustomGameMode.Standard);
            WhenNonVote = StringOptionItem.Create(100520, "WhenNonVote", voteModes, 0, TabGroup.GameSettings, false).SetParent(VoteMode)
                .SetGameMode(CustomGameMode.Standard);
            WhenTie = StringOptionItem.Create(100530, "WhenTie", tieModes, 0, TabGroup.GameSettings, false).SetParent(VoteMode)
                .SetGameMode(CustomGameMode.Standard);

            // 全員生存時の会議時間
            AllAliveMeeting = BooleanOptionItem.Create(100900, "AllAliveMeeting", false, TabGroup.GameSettings, false);
            AllAliveMeetingTime = FloatOptionItem.Create(100901, "AllAliveMeetingTime", new(1f, 300f, 1f), 10f, TabGroup.GameSettings, false).SetParent(AllAliveMeeting)
                .SetValueFormat(OptionFormat.Seconds);

            // 生存人数ごとの緊急会議
            AdditionalEmergencyCooldown = BooleanOptionItem.Create(101400, "AdditionalEmergencyCooldown", false, TabGroup.GameSettings, false);
            AdditionalEmergencyCooldownThreshold = IntegerOptionItem.Create(101401, "AdditionalEmergencyCooldownThreshold", new(1, 15, 1), 1, TabGroup.GameSettings, false).SetParent(AdditionalEmergencyCooldown)
                .SetValueFormat(OptionFormat.Players);
            AdditionalEmergencyCooldownTime = FloatOptionItem.Create(101402, "AdditionalEmergencyCooldownTime", new(1f, 60f, 1f), 1f, TabGroup.GameSettings, false).SetParent(AdditionalEmergencyCooldown)
                .SetValueFormat(OptionFormat.Seconds);

            // 転落死
            LadderDeath = BooleanOptionItem.Create(101100, "LadderDeath", false, TabGroup.GameSettings, false)
                .SetHeader(true);
            LadderDeathChance = StringOptionItem.Create(101110, "LadderDeathChance", rates[1..], 0, TabGroup.GameSettings, false).SetParent(LadderDeath);

            //エレキ構造変化
            AirShipVariableElectrical = BooleanOptionItem.Create(101600, "AirShipVariableElectrical", false, TabGroup.GameSettings, false)
                .SetHeader(true);

            // その他
            FixFirstKillCooldown = BooleanOptionItem.Create(900_000, "FixFirstKillCooldown", false, TabGroup.GameSettings, false)
                .SetHeader(true)
                .SetGameMode(CustomGameMode.All);
            DisableTaskWin = BooleanOptionItem.Create(900_001, "DisableTaskWin", false, TabGroup.GameSettings, false)
                .SetGameMode(CustomGameMode.All);
            GhostCanSeeOtherRoles = BooleanOptionItem.Create(900_010, "GhostCanSeeOtherRoles", true, TabGroup.GameSettings, false)
                .SetGameMode(CustomGameMode.All);
            GhostCanSeeOtherVotes = BooleanOptionItem.Create(900_011, "GhostCanSeeOtherVotes", true, TabGroup.GameSettings, false)
                .SetGameMode(CustomGameMode.All);
            GhostCanSeeDeathReason = BooleanOptionItem.Create(900_014, "GhostCanSeeDeathReason", true, TabGroup.GameSettings, false)
                .SetGameMode(CustomGameMode.All);
            GhostIgnoreTasks = BooleanOptionItem.Create(900_012, "GhostIgnoreTasks", false, TabGroup.GameSettings, false)
                .SetGameMode(CustomGameMode.All);
            CommsCamouflage = BooleanOptionItem.Create(900_013, "CommsCamouflage", false, TabGroup.GameSettings, false)
                .SetGameMode(CustomGameMode.All);
            DisableReportWhenCC = BooleanOptionItem.Create(900_015, "DisableReportWhenCC", false, TabGroup.GameSettings, false).SetParent(CommsCamouflage)
                .SetGameMode(CustomGameMode.All);
            #endregion 

            IsLoaded = true;
        }

        public static void SetupRoleOptions(int id, TabGroup tab, CustomRoles role, CustomGameMode customGameMode = CustomGameMode.Standard)
        {
            var spawnOption = StringOptionItem.Create(id, role.ToString(), ratesToggle, 0, tab, false).SetColor(Utils.GetRoleColor(role))
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

            LoverSpawnChances = IntegerOptionItem.Create(id + 2, "LoverSpawnChances", new(0, 100 ,5), 50, TabGroup.Addons, false).SetParent(spawnOption)
                .SetValueFormat(OptionFormat.Percent)
                .SetGameMode(customGameMode);

            LoverSuicide = BooleanOptionItem.Create(id + 3, "LoverSuicide", true, TabGroup.Addons, false).SetParent(spawnOption)
                .SetGameMode(customGameMode);

            LoverHasNoTask = BooleanOptionItem.Create(id + 4, "LoverHasNoTask", false, TabGroup.Addons, false).SetParent(spawnOption)
                .SetGameMode(customGameMode);

            var countOption = IntegerOptionItem.Create(id + 1, "NumberOfLovers", new(2, 2, 1), 2, TabGroup.Addons, false).SetParent(spawnOption)
                .SetHidden(true)
                .SetGameMode(customGameMode);

            CustomRoleSpawnChances.Add(role, spawnOption);
            CustomRoleCounts.Add(role, countOption);
        }

        private static void SetupAdtRoleOptions(int id, CustomRoles role, CustomGameMode customGameMode = CustomGameMode.Standard, bool canSetNum = false, TabGroup tab = TabGroup.Addons)
        {
            var spawnOption = StringOptionItem.Create(id, role.ToString(), ratesZeroOne, 0, tab, false).SetColor(Utils.GetRoleColor(role))
                .SetHeader(true)
                .SetGameMode(customGameMode) as StringOptionItem;

            var countOption = IntegerOptionItem.Create(id + 1, "Maximum", new(1, canSetNum ? 3 : 1, 1), 1, tab, false).SetParent(spawnOption)
                .SetHidden(!canSetNum)
                .SetGameMode(customGameMode);

            var spawnRateOption = IntegerOptionItem.Create(id + 2, "AdditionRolesSpawnRate", new(0, 100, 5), 65, tab, false).SetParent(spawnOption)
                .SetValueFormat(OptionFormat.Percent)
                .SetGameMode(customGameMode) as IntegerOptionItem;

            CustomAdtRoleSpawnRate.Add(role, spawnRateOption);
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