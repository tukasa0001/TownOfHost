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
        public static Dictionary<CustomRoles, int> roleCounts;
        public static Dictionary<CustomRoles, float> roleSpawnChances;
        public static bool OptionControllerIsEnable = false;

        public static CustomOption Preset;
        private static readonly string[] presets =
        {
            "Preset_1", "Preset_2", "Preset_3",
            "Preset_4", "Preset_5"
        };

        public static CustomOption GameMode;
        public static CustomGameMode CurrentGameMode
            => GameMode.Selection == 0 ? CustomGameMode.Standard : CustomGameMode.HideAndSeek;
        
        public static readonly string[] gameModes =
        {
            "Standard", "HideAndSeek",
        };

        public static Dictionary<CustomRoles, CustomOption> RoleCounts;
        public static Dictionary<CustomRoles, CustomOption> RoleSpawnChances;
        public static readonly string[] rates =
        {
            "Rate0", "Rate10", "Rate20", "Rate30", "Rate40", "Rate50",
            "Rate60", "Rate70", "Rate80", "Rate90", "Rate100",
        };

        // 各役職の詳細設定
        public static CustomOption R_BountyTargetChangeTime;
        public static CustomOption R_BountySuccessKillCooldown;
        public static CustomOption R_BountyFailureKillCooldown;
        public static CustomOption R_BHDefaultKillCooldown;
        public static CustomOption R_SerialKillerCooldown;
        public static CustomOption R_SerialKillerLimit;
        public static CustomOption R_VampireKillDelay;
        public static CustomOption R_ShapeMasterShapeshiftDuration;
        public static CustomOption R_MadmateCanFixLightOut; // TODO:mii-47 マッド役職統一
        public static CustomOption R_MadmateCanFixComms;
        public static CustomOption R_MadmateHasImpostorVision;
        public static CustomOption R_MadGuardianCanSeeBarrier;
        public static CustomOption R_MadSnitchTasks;
        public static CustomOption R_CanMakeMadmateCount;

        public static CustomOption R_MayorAdditionalVote;
        public static CustomOption R_SabotageMasterSkillLimit;
        public static CustomOption R_SabotageMasterFixesDoors;
        public static CustomOption R_SabotageMasterFixesReactors;
        public static CustomOption R_SabotageMasterFixesOxygens;
        public static CustomOption R_SabotageMasterFixesComms;
        public static CustomOption R_SabotageMasterElectrical;
        public static CustomOption R_SheriffKillCooldown;
        public static CustomOption R_SheriffCanKillJester;
        public static CustomOption R_SheriffCanKillTerrorist;
        public static CustomOption R_SheriffCanKillOpportunist;
        
        // HideAndSeek
        public static CustomOption HideAndSeek_AllowCloseDoors;
        public static CustomOption HideNadSeek_WaitingTime;
        public static CustomOption HideNadSeek_IgnoreCosmetics;
        public static CustomOption HideNadSeek_IgnoreVent;

        // ボタン回数
        public static CustomOption N_SyncButtonMode;
        public static CustomOption Button_SyncedButtonCount;

        // タスク無効化
        public static CustomOption DisableTasks;
        public static CustomOption Task_DisableSwipeCard;
        public static CustomOption Task_DisableSubmitScan;
        public static CustomOption Task_DisableUnlockScan;
        public static CustomOption Task_DisableUploadData;
        public static CustomOption Task_DisableStartReactor;
        public static CustomOption Task_DisableResetBreaker;

        // ランダムマップ
        public static CustomOption RandomMapMode;
        public static CustomOption MapSkeld;
        public static CustomOption MapMiraHQ;
        public static CustomOption MapPolus;
        public static CustomOption MapAirship;
        public static CustomOption MapDleks;

        public static CustomOption N_NoGameEnd;

        // 投票モード
        public static CustomOption N_WhenSkipVote;
        public static CustomOption N_WhenNonVote;
        public static CustomOption N_CanTerroristSuicideWin;
        public static readonly string[] voteModes =
        {
            "Default", "Suicide", "Skip"
        };

        public static CustomOption ForceJapanese;
        public static CustomOption AutoDisplayLastResult;

        //詳細設定
        public static bool IsHideAndSeek = false;
        public static bool AllowCloseDoors = false;
        public static bool IgnoreVent = false;
        public static bool IgnoreCosmetics = false;
        public static int HideAndSeekKillDelay = 30;
        public static float HideAndSeekKillDelayTimer = 0f;
        public static float HideAndSeekImpVisionMin = 0.25f;
        public static bool SyncButtonMode = false;
        public static int SyncedButtonCount = 10;
        public static int UsedButtonCount = 0;
        public static bool RandomMapsMode;
        public static bool NoGameEnd = false;
        //タスク無効化
        public static bool DisableSwipeCard = false;
        public static bool DisableSubmitScan = false;
        public static bool DisableUnlockSafe = false;
        public static bool DisableUploadData = false;
        public static bool DisableStartReactor = false;
        public static bool DisableResetBreaker = false;
        //ランダムマップ
        public static bool AddedTheSkeld;
        public static bool AddedMIRAHQ;
        public static bool AddedPolus;
        public static bool AddedDleks;
        public static bool AddedTheAirShip;
        public static bool canTerroristSuicideWin = false;
        public static bool autoDisplayLastRoles = false;
        public static int ShapeMasterShapeshiftDuration = 10;
        public static int SerialKillerCooldown = 20;
        public static int SerialKillerLimit = 60;
        public static int BountyTargetChangeTime = 150;
        public static int BountySuccessKillCooldown = 2;
        public static int BountyFailureKillCooldown = 50;
        public static int BHDefaultKillCooldown = 30;
        public static int VampireKillDelay = 10;
        public static int SabotageMasterSkillLimit = 0;
        public static bool SabotageMasterFixesDoors = false;
        public static bool SabotageMasterFixesReactors = true;
        public static bool SabotageMasterFixesOxygens = true;
        public static bool SabotageMasterFixesCommunications = true;
        public static bool SabotageMasterFixesElectrical = true;
        public static int SheriffKillCooldown = 30;
        public static bool SheriffCanKillJester = true;
        public static bool SheriffCanKillTerrorist = true;
        public static bool SheriffCanKillOpportunist = false;
        public static bool SheriffCanKillMadmate = true;
        public static int MayorAdditionalVote = 1;
        public static int SnitchExposeTaskLeft = 1;
        public static bool MadmateHasImpostorVision = true;
        public static bool MadmateCanFixLightsOut = false;
        public static bool MadmateCanFixComms = false;
        public static bool MadGuardianCanSeeWhoTriedToKill = false;
        public static int MadSnitchTasks = 4;
        public static int CanMakeMadmateCount;
        public static VoteMode whenSkipVote = VoteMode.Default;
        public static VoteMode whenNonVote = VoteMode.Default;
        public static bool forceJapanese = false;
        public static SuffixModes currentSuffix = SuffixModes.None;
        public static int SabotageMasterUsedSkillCount;

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
        public static void setRoleCount(CustomRoles role, int count) { roleCounts[role] = count; }
        public static int getRoleCount(CustomRoles role) { return roleCounts[role]; }

        public static void Load()
        {
            if (IsLoaded) return;
            
            Preset = CustomOption.Create(0, new Color(204f / 255f, 204f / 255f, 0, 1f), "Preset", presets, presets[0], null, true)
                .HiddenOnDisplay(true)
                .SetGameMode(CustomGameMode.All);

            GameMode = CustomOption.Create(1, new Color(204f / 255f, 204f / 255f, 0, 1f), "GameMode", gameModes, gameModes[0], null, true)
                .SetGameMode(CustomGameMode.All);


            // === スタンダード役職 ===
            RoleCounts = new Dictionary<CustomRoles, CustomOption>();
            RoleSpawnChances = new Dictionary<CustomRoles, CustomOption>();
            
            SetupRoleOptions(CustomRoles.BountyHunter);
            R_BountyTargetChangeTime = CustomOption.Create(100, Color.white, "BountyTargetChangeTime", 150, 5, 1000, 5, RoleSpawnChances[CustomRoles.BountyHunter]);
            R_BountySuccessKillCooldown = CustomOption.Create(100, Color.white, "BountySuccessKillCooldown", 2, 5, 999, 1, RoleSpawnChances[CustomRoles.BountyHunter]);
            R_BountyFailureKillCooldown = CustomOption.Create(100, Color.white, "BountyFailureKillCooldown", 50, 5, 999, 1, RoleSpawnChances[CustomRoles.BountyHunter]);
            R_BHDefaultKillCooldown = CustomOption.Create(100, Color.white, "BHDefaultKillCooldown", 30, 2, 999, 1, RoleSpawnChances[CustomRoles.BountyHunter]);
            SetupRoleOptions(CustomRoles.SerialKiller);
            R_SerialKillerCooldown = CustomOption.Create(100, Color.white, "SerialKillerCooldown", 20, 5, 1000, 1, RoleSpawnChances[CustomRoles.SerialKiller]);
            R_SerialKillerLimit = CustomOption.Create(100, Color.white, "SerialKillerLimit", 60, 5, 1000, 1, RoleSpawnChances[CustomRoles.SerialKiller]);
            SetupRoleOptions(CustomRoles.ShapeMaster);
            R_ShapeMasterShapeshiftDuration = CustomOption.Create(100, Color.white, "ShapeMasterShapeshiftDuration", 10, 1, 1000, 1, RoleSpawnChances[CustomRoles.ShapeMaster]);
            SetupRoleOptions(CustomRoles.Vampire);
            R_VampireKillDelay = CustomOption.Create(100, Color.white, "VampireKillDelay", 10, 1, 1000, 1, RoleSpawnChances[CustomRoles.Vampire]);
            SetupRoleOptions(CustomRoles.Warlock);
            SetupRoleOptions(CustomRoles.Witch);
            SetupRoleOptions(CustomRoles.Mafia);

            SetupRoleOptions(CustomRoles.Madmate);
            R_MadmateCanFixLightOut = CustomOption.Create(100, Color.white, "MadmateCanFixLightsOut", false, RoleSpawnChances[CustomRoles.Madmate]);
            R_MadmateCanFixComms = CustomOption.Create(100, Color.white, "MadmateCanFixComms", false, RoleSpawnChances[CustomRoles.Madmate]);
            R_MadmateHasImpostorVision = CustomOption.Create(100, Color.white, "MadmateHasImpostorVision", false, RoleSpawnChances[CustomRoles.Madmate]);
            SetupRoleOptions(CustomRoles.MadGuardian);
            R_MadGuardianCanSeeBarrier = CustomOption.Create(100, Color.white, "MadGuardianCanSeeWhoTriedToKill", false, RoleSpawnChances[CustomRoles.MadGuardian]);
            SetupRoleOptions(CustomRoles.MadSnitch);
            R_MadGuardianCanSeeBarrier = CustomOption.Create(100, Color.white, "MadSnitchTasks", false, RoleSpawnChances[CustomRoles.MadSnitch]);

            SetupRoleOptions(CustomRoles.Bait);
            SetupRoleOptions(CustomRoles.Lighter);
            SetupRoleOptions(CustomRoles.Mayor);
            R_MayorAdditionalVote = CustomOption.Create(100, Color.white, "MayorAdditionalVote", 1, 1, 99, 1, RoleSpawnChances[CustomRoles.Mayor]);
            SetupRoleOptions(CustomRoles.SabotageMaster);
            R_SabotageMasterSkillLimit = CustomOption.Create(100, Color.white, "SabotageMasterSkillLimit", 1, 0, 99, 1, RoleSpawnChances[CustomRoles.SabotageMaster]);
            R_SabotageMasterFixesDoors = CustomOption.Create(100, Color.white, "SabotageMasterFixesDoors", false, RoleSpawnChances[CustomRoles.SabotageMaster]);
            R_SabotageMasterFixesReactors  = CustomOption.Create(100, Color.white, "SabotageMasterFixesReactors", false, RoleSpawnChances[CustomRoles.SabotageMaster]);
            R_SabotageMasterFixesOxygens = CustomOption.Create(100, Color.white, "SabotageMasterFixesOxygens", false, RoleSpawnChances[CustomRoles.SabotageMaster]);
            R_SabotageMasterFixesComms = CustomOption.Create(100, Color.white, "SabotageMasterFixesCommunications", false, RoleSpawnChances[CustomRoles.SabotageMaster]);
            R_SabotageMasterElectrical = CustomOption.Create(100, Color.white, "SabotageMasterFixesElectrical", false, RoleSpawnChances[CustomRoles.SabotageMaster]);
            SetupRoleOptions(CustomRoles.Sheriff);
            R_SheriffKillCooldown = CustomOption.Create(100, Color.white, "SheriffKillCooldown", 30, 0, 990, 1, RoleSpawnChances[CustomRoles.Sheriff]);
            R_SheriffCanKillJester = CustomOption.Create(100, Color.white, "SheriffCanKillJester", true, RoleSpawnChances[CustomRoles.Sheriff]);
            R_SheriffCanKillTerrorist = CustomOption.Create(100, Color.white, "SheriffCanKillTerrorist", true, RoleSpawnChances[CustomRoles.Sheriff]);
            R_SheriffCanKillOpportunist= CustomOption.Create(100, Color.white, "SheriffCanKillOpportunist", true, RoleSpawnChances[CustomRoles.Sheriff]);
            SetupRoleOptions(CustomRoles.Snitch);

            SetupRoleOptions(CustomRoles.Jester);
            SetupRoleOptions(CustomRoles.Opportunist);
            SetupRoleOptions(CustomRoles.Terrorist);

            // HideAndSeek
            SetupRoleOptions(CustomRoles.Fox, CustomGameMode.HideAndSeek);
            SetupRoleOptions(CustomRoles.Troll, CustomGameMode.HideAndSeek);
            HideAndSeek_AllowCloseDoors = CustomOption.Create(count, Color.white, "AllowCloseDoors", false, null, true)
                .SetGameMode(CustomGameMode.HideAndSeek);
            HideNadSeek_WaitingTime = CustomOption.Create(count, Color.white, "HideAndSeekWaitingTime", 10, 0, 180, 5)
                .SetGameMode(CustomGameMode.HideAndSeek);
            HideNadSeek_IgnoreCosmetics = CustomOption.Create(count, Color.white, "IgnoreCosmetics", false)
                .SetGameMode(CustomGameMode.HideAndSeek);
            HideNadSeek_IgnoreVent = CustomOption.Create(count, Color.white, "IgnoreVent", false)
                .SetGameMode(CustomGameMode.HideAndSeek);

            // ボタン回数同期
            N_SyncButtonMode = CustomOption.Create(count, Color.white, "SyncButtonMode", false, null, true)
                .SetGameMode(CustomGameMode.Standard);
            Button_SyncedButtonCount = CustomOption.Create(count, Color.white, "SyncedButtonCount", 10, 0, 100, 1, N_SyncButtonMode)
                .SetGameMode(CustomGameMode.Standard);

            // タスク無効化
            DisableTasks = CustomOption.Create(count, Color.white, "DisableTasks", false, null, true)
                .SetGameMode(CustomGameMode.All);
            Task_DisableSwipeCard = CustomOption.Create(count, Color.white, "DisableSwipeCardTask", false, DisableTasks)
                .SetGameMode(CustomGameMode.All);
            Task_DisableSubmitScan = CustomOption.Create(count, Color.white, "DisableSubmitScanTask", false, DisableTasks)
                .SetGameMode(CustomGameMode.All);
            Task_DisableUnlockScan = CustomOption.Create(count, Color.white, "DisableUnlockSafeTask", false, DisableTasks)
                .SetGameMode(CustomGameMode.All);
            Task_DisableUploadData = CustomOption.Create(count, Color.white, "DisableUploadDataTask", false, DisableTasks)
                .SetGameMode(CustomGameMode.All);
            Task_DisableStartReactor = CustomOption.Create(count, Color.white, "DisableStartReactorTask", false, DisableTasks)
                .SetGameMode(CustomGameMode.All);
            Task_DisableResetBreaker = CustomOption.Create(count, Color.white, "DisableResetBreakerTask", false, DisableTasks)
                .SetGameMode(CustomGameMode.All);

            // ランダムマップ
            RandomMapMode = CustomOption.Create(count, Color.white, "RandomMapsMode", false, null, true)
                .SetGameMode(CustomGameMode.All);
            MapSkeld = CustomOption.Create(count, Color.white, "AddedTheSkeld", false, RandomMapMode)
                .SetGameMode(CustomGameMode.All);
            MapMiraHQ = CustomOption.Create(count, Color.white, "AddedMIRAHQ", false, RandomMapMode)
                .SetGameMode(CustomGameMode.All);
            MapPolus = CustomOption.Create(count, Color.white, "AddedPolus", false, RandomMapMode)
                .SetGameMode(CustomGameMode.All);
            MapAirship = CustomOption.Create(count, Color.white, "AddedTheAirShip", false, RandomMapMode)
                .SetGameMode(CustomGameMode.All);
            // MapDleks = CustomOption.Create(count, Color.white, "AddedDleks", false, RandomMapMode)
            //     .SetGameMode(CustomGameMode.All);

            Task_DisableResetBreaker = CustomOption.Create(count, Color.white, "DisableResetBreakerTask", false, DisableTasks)
                .SetGameMode(CustomGameMode.All);

            N_NoGameEnd = CustomOption.Create(count, Color.white, "NoGameEnd", false, null, true)
                .SetGameMode(CustomGameMode.All);

            N_WhenSkipVote = CustomOption.Create(count, Color.white, "WhenSkipVote", voteModes, voteModes[0], null, true)
                .SetGameMode(CustomGameMode.Standard);
            N_WhenNonVote = CustomOption.Create(count, Color.white, "WhenNonVote", voteModes, voteModes[0], null, false)
                .SetGameMode(CustomGameMode.Standard);
            N_CanTerroristSuicideWin = CustomOption.Create(count, Color.white, "CanTerroristSuicideWin", false, null, false)
                .SetGameMode(CustomGameMode.Standard);


            ForceJapanese = CustomOption.Create(count, Color.white, "ForceJapanese", false, null, true)
                .SetGameMode(CustomGameMode.All);

            AutoDisplayLastResult = CustomOption.Create(count, Color.white, "AutoDisplayLastResult", false, null, true)
                .SetGameMode(CustomGameMode.All);

            IsLoaded = true;
        }

        private static int count = 100;

        private static void SetupRoleOptions(CustomRoles role, CustomGameMode customGameMode = CustomGameMode.Standard)
        {
            var spawnOption = CustomOption.Create(count, Utils.getRoleColor(role), Utils.getRoleName(role), rates, rates[0], null, true)
                .HiddenOnDisplay(true)
                .SetGameMode(customGameMode);
            var countOption = CustomOption.Create(count + 1, Color.white, "Maximum", 1, 1, 15, 1, spawnOption, false)
                .HiddenOnDisplay(true)
                .SetGameMode(customGameMode);

            RoleSpawnChances.Add(role, spawnOption);
            RoleCounts.Add(role, countOption);

            count += 2;
        }

    }
}
