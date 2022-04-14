using System.Linq;
using System;
using System.Collections.Generic;
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

        // 各役職の詳細設定
        public static CustomOption EnableLastImpostor;
        public static CustomOption LastImpostorKillCooldown;
        public static CustomOption BountyTargetChangeTime;
        public static CustomOption BountySuccessKillCooldown;
        public static CustomOption BountyFailureKillCooldown;
        public static CustomOption BHDefaultKillCooldown;
        public static CustomOption SerialKillerCooldown;
        public static CustomOption SerialKillerLimit;
        public static CustomOption VampireKillDelay;
        public static CustomOption ShapeMasterShapeshiftDuration;
        public static CustomOption DefaultShapeshiftCooldown;
        public static CustomOption MadmateCanFixLightsOut; // TODO:mii-47 マッド役職統一
        public static CustomOption MadmateCanFixComms;
        public static CustomOption MadmateHasImpostorVision;
        public static CustomOption MadGuardianCanSeeWhoTriedToKill;
        public static CustomOption MadSnitchTasks;
        public static CustomOption CanMakeMadmateCount;

        public static CustomOption EvilWatcherChance;
        public static CustomOption MayorAdditionalVote;
        public static CustomOption SabotageMasterSkillLimit;
        public static CustomOption SabotageMasterFixesDoors;
        public static CustomOption SabotageMasterFixesReactors;
        public static CustomOption SabotageMasterFixesOxygens;
        public static CustomOption SabotageMasterFixesComms;
        public static CustomOption SabotageMasterFixesElectrical;
        public static int SabotageMasterUsedSkillCount;
        public static CustomOption SheriffKillCooldown;
        public static CustomOption SheriffCanKillArsonist;
        public static CustomOption SheriffCanKillMadmate;
        public static CustomOption SheriffCanKillJester;
        public static CustomOption SheriffCanKillTerrorist;
        public static CustomOption SheriffCanKillOpportunist;
        public static CustomOption SheriffCanKillEgoist;
        public static CustomOption SheriffCanKillEgoShrodingerCat;
        public static CustomOption SheriffCanKillCrewmatesAsIt;
        public static CustomOption SheriffShotLimit;
        public static CustomOption SpeedBoosterUpSpeed;
        public static CustomOption CanTerroristSuicideWin;
        public static CustomOption ArsonistDouseTime;
        public static CustomOption ArsonistCooldown;
        public static CustomOption CanBeforeSchrodingerCatWinTheCrewmate;
        public static CustomOption SchrodingerCatExiledTeamChanges;

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

        // その他
        public static CustomOption NoGameEnd;
        public static CustomOption AutoDisplayLastResult;
        public static CustomOption SuffixMode;
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
            if (UnityEngine.Random.Range(1, 100) < EvilWatcherRate)
                IsEvilWatcher = true;
            else
                IsEvilWatcher = false;
        }
        private static bool IsLoaded = false;

        static Options()
        {
            resetRoleCounts();
        }
        public static void resetRoleCounts()
        {
            roleCounts = new Dictionary<CustomRoles, int>();
            roleSpawnChances = new Dictionary<CustomRoles, float>();

            foreach (var role in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>())
            {
                roleCounts.Add(role, 0);
                roleSpawnChances.Add(role, 0);
            }
        }

        public static void setRoleCount(CustomRoles role, int count)
        {
            roleCounts[role] = count;

            if (CustomRoleCounts.TryGetValue(role, out var option))
            {
                option.UpdateSelection(count - 1);
            }
        }

        public static int getRoleCount(CustomRoles role)
        {
            var chance = CustomRoleSpawnChances.TryGetValue(role, out var sc) ? sc.GetSelection() : 0;
            if (chance == 0) return 0;
            return CustomRoleCounts.TryGetValue(role, out var option) ? (int)option.GetFloat() : roleCounts[role];
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
            BountyTargetChangeTime = CustomOption.Create(1010, Color.white, "BountyTargetChangeTime", 10, 5, 1000, 5, CustomRoleSpawnChances[CustomRoles.BountyHunter]);
            BountySuccessKillCooldown = CustomOption.Create(1011, Color.white, "BountySuccessKillCooldown", 5, 5, 999, 1, CustomRoleSpawnChances[CustomRoles.BountyHunter]);
            BountyFailureKillCooldown = CustomOption.Create(1012, Color.white, "BountyFailureKillCooldown", 50, 5, 999, 5, CustomRoleSpawnChances[CustomRoles.BountyHunter]);
            SetupRoleOptions(1100, CustomRoles.SerialKiller);
            SerialKillerCooldown = CustomOption.Create(1110, Color.white, "SerialKillerCooldown", 20, 1, 1000, 1, CustomRoleSpawnChances[CustomRoles.SerialKiller]);
            SerialKillerLimit = CustomOption.Create(1111, Color.white, "SerialKillerLimit", 60, 5, 1000, 5, CustomRoleSpawnChances[CustomRoles.SerialKiller]);
            SetupRoleOptions(1200, CustomRoles.ShapeMaster);
            ShapeMasterShapeshiftDuration = CustomOption.Create(1210, Color.white, "ShapeMasterShapeshiftDuration", 10, 1, 1000, 1, CustomRoleSpawnChances[CustomRoles.ShapeMaster]);
            SetupRoleOptions(1300, CustomRoles.Vampire);
            VampireKillDelay = CustomOption.Create(1310, Color.white, "VampireKillDelay", 10, 1, 1000, 1, CustomRoleSpawnChances[CustomRoles.Vampire]);
            SetupRoleOptions(1400, CustomRoles.Warlock);
            SetupRoleOptions(1500, CustomRoles.Witch);
            SetupRoleOptions(1600, CustomRoles.Mafia);

            BHDefaultKillCooldown = CustomOption.Create(5010, Color.white, "BHDefaultKillCooldown", 30, 1, 999, 1, null, true);
            DefaultShapeshiftCooldown = CustomOption.Create(5011, Color.white, "DefaultShapeshiftCooldown", 15, 5, 999, 5, null, true);
            CanMakeMadmateCount = CustomOption.Create(5012, Color.white, "CanMakeMadmateCount", 1, 0, 15, 1, null, true);

            // Madmate
            SetupRoleOptions(10000, CustomRoles.Madmate);
            SetupRoleOptions(10100, CustomRoles.MadGuardian);
            MadGuardianCanSeeWhoTriedToKill = CustomOption.Create(10110, Color.white, "MadGuardianCanSeeWhoTriedToKill", false, CustomRoleSpawnChances[CustomRoles.MadGuardian]);
            SetupRoleOptions(10200, CustomRoles.MadSnitch);
            MadSnitchTasks = CustomOption.Create(10210, Color.white, "MadSnitchTasks", 4, 1, 20, 1, CustomRoleSpawnChances[CustomRoles.MadSnitch]);
            // Madmate Common Options
            MadmateCanFixLightsOut = CustomOption.Create(10010, Color.white, "MadmateCanFixLightsOut", false, null, true);
            MadmateCanFixComms = CustomOption.Create(10011, Color.white, "MadmateCanFixComms", false);
            MadmateHasImpostorVision = CustomOption.Create(10012, Color.white, "MadmateHasImpostorVision", false);
            // Both
            SetupRoleOptions(30000, CustomRoles.Watcher);
            EvilWatcherChance = CustomOption.Create(30010, Color.white, "EvilWatcherChance", 0, 0, 100, 10, CustomRoleSpawnChances[CustomRoles.Watcher]);
            // Crewmate
            SetupRoleOptions(20000, CustomRoles.Bait);
            SetupRoleOptions(20100, CustomRoles.Lighter);
            SetupRoleOptions(20200, CustomRoles.Mayor);
            MayorAdditionalVote = CustomOption.Create(20210, Color.white, "MayorAdditionalVote", 1, 1, 99, 1, CustomRoleSpawnChances[CustomRoles.Mayor]);
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
            SheriffCanKillCrewmatesAsIt = CustomOption.Create(20415, Color.white, "SheriffCanKillCrewmatesAsIt", false, CustomRoleSpawnChances[CustomRoles.Sheriff]);
            SheriffShotLimit = CustomOption.Create(20416, Color.white, "SheriffShotLimit", 15, 1, 15, 1, CustomRoleSpawnChances[CustomRoles.Sheriff]);
            SetupRoleOptions(20500, CustomRoles.Snitch);
            SetupRoleOptions(20600, CustomRoles.SpeedBooster);
            SpeedBoosterUpSpeed = CustomOption.Create(20610, Color.white, "SpeedBoosterUpSpeed", 2f, 0.25f, 3f, 0.25f, CustomRoleSpawnChances[CustomRoles.SpeedBooster]);
            // Other
            SetupRoleOptions(50500, CustomRoles.Arsonist);
            ArsonistDouseTime = CustomOption.Create(50510, Color.white, "ArsonistDouseTime", 3, 1, 10, 1, CustomRoleSpawnChances[CustomRoles.Arsonist]);
            ArsonistCooldown = CustomOption.Create(50511, Color.white, "ArsonistCooldown", 10, 5, 100, 1, CustomRoleSpawnChances[CustomRoles.Arsonist]);
            SetupRoleOptions(50000, CustomRoles.Jester);
            SetupRoleOptions(50100, CustomRoles.Opportunist);
            SetupRoleOptions(50200, CustomRoles.Terrorist);
            CanTerroristSuicideWin = CustomOption.Create(50210, Color.white, "CanTerroristSuicideWin", false, CustomRoleSpawnChances[CustomRoles.Terrorist], false)
                .SetGameMode(CustomGameMode.Standard);
            SetupRoleOptions(50400, CustomRoles.SchrodingerCat);
            CanBeforeSchrodingerCatWinTheCrewmate = CustomOption.Create(50410, Color.white, "CanBeforeSchrodingerCatWinTheCrewmate", false, CustomRoleSpawnChances[CustomRoles.SchrodingerCat]);
            SchrodingerCatExiledTeamChanges = CustomOption.Create(50411, Color.white, "SchrodingerCatExiledTeamChanges", false, CustomRoleSpawnChances[CustomRoles.SchrodingerCat]);
            SetupRoleOptions(50600, CustomRoles.Egoist);

            EnableLastImpostor = CustomOption.Create(80000, Utils.getRoleColor(CustomRoles.Impostor), "LastImpostor", false, null, true)
                .SetGameMode(CustomGameMode.Standard);
            LastImpostorKillCooldown = CustomOption.Create(80010, Color.white, "LastImpostorKillCooldown", 15, 0, 180, 1, EnableLastImpostor)
                .SetGameMode(CustomGameMode.Standard);
            #endregion

            // HideAndSeek
            SetupRoleOptions(100000, CustomRoles.Fox, CustomGameMode.HideAndSeek);
            SetupRoleOptions(100100, CustomRoles.Troll, CustomGameMode.HideAndSeek);
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

            // その他
            NoGameEnd = CustomOption.Create(100600, Color.white, "NoGameEnd", false, null, true)
                .SetGameMode(CustomGameMode.All);
            AutoDisplayLastResult = CustomOption.Create(100601, Color.white, "AutoDisplayLastResult", false)
                .SetGameMode(CustomGameMode.All);
            SuffixMode = CustomOption.Create(100602, Color.white, "SuffixMode", suffixModes, suffixModes[0])
                .SetGameMode(CustomGameMode.All);

            IsLoaded = true;
        }

        private static void SetupRoleOptions(int id, CustomRoles role, CustomGameMode customGameMode = CustomGameMode.Standard)
        {
            var spawnOption = CustomOption.Create(id, Utils.getRoleColor(role), role.ToString(), rates, rates[0], null, true)
                .HiddenOnDisplay(true)
                .SetGameMode(customGameMode);
            var countOption = CustomOption.Create(id + 1, Color.white, "Maximum", 1, 1, 15, 1, spawnOption, false)
                .HiddenOnDisplay(true)
                .SetGameMode(customGameMode);

            CustomRoleSpawnChances.Add(role, spawnOption);
            CustomRoleCounts.Add(role, countOption);
        }

    }
}
