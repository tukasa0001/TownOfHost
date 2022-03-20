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

        public static Dictionary<CustomRoles, CustomOption> CustomRoleCounts;
        public static Dictionary<CustomRoles, CustomOption> CustomRoleSpawnChances;
        public static readonly string[] rates =
        {
            "Rate0", "Rate10", "Rate20", "Rate30", "Rate40", "Rate50",
            "Rate60", "Rate70", "Rate80", "Rate90", "Rate100",
        };

        // 各役職の詳細設定
        public static CustomOption BountyTargetChangeTime;
        public static CustomOption BountySuccessKillCooldown;
        public static CustomOption BountyFailureKillCooldown;
        public static CustomOption BHDefaultKillCooldown;
        public static CustomOption SerialKillerCooldown;
        public static CustomOption SerialKillerLimit;
        public static CustomOption VampireKillDelay;
        public static CustomOption ShapeMasterShapeshiftDuration;
        public static CustomOption MadmateCanFixLightsOut; // TODO:mii-47 マッド役職統一
        public static CustomOption MadmateCanFixComms;
        public static CustomOption MadmateHasImpostorVision;
        public static CustomOption MadGuardianCanSeeWhoTriedToKill;
        public static CustomOption MadSnitchTasks;
        public static CustomOption CanMakeMadmateCount;

        public static CustomOption MayorAdditionalVote;
        public static CustomOption SabotageMasterSkillLimit;
        public static CustomOption SabotageMasterFixesDoors;
        public static CustomOption SabotageMasterFixesReactors;
        public static CustomOption SabotageMasterFixesOxygens;
        public static CustomOption SabotageMasterFixesComms;
        public static CustomOption SabotageMasterFixesElectrical;
        public static int SabotageMasterUsedSkillCount;
        public static CustomOption SheriffKillCooldown;
        public static CustomOption SheriffCanKillMadmate;
        public static CustomOption SheriffCanKillJester;
        public static CustomOption SheriffCanKillTerrorist;
        public static CustomOption SheriffCanKillOpportunist;

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

        public static CustomOption NoGameEnd;

        // 投票モード
        public static CustomOption WhenSkipVote;
        public static CustomOption WhenNonVote;
        public static CustomOption CanTerroristSuicideWin;
        public static readonly string[] voteModes =
        {
            "Default", "Suicide", "Skip"
        };
        public static VoteMode GetVoteMode(CustomOption option)
        {
            return (VoteMode) option.GetSelection();
        }
        

        public static CustomOption ForceJapanese;
        public static CustomOption AutoDisplayLastResult;
        public static CustomOption SuffixMode;
        public static readonly string[] suffixModes =
        {
            "SuffixMode_Node",
            "SuffixMode_Version", 
            "SuffixMode_Streaming",
            "SuffixMode_Recording"
        };

        public static SuffixModes GetSuffixMode()
        {
            return (SuffixModes) SuffixMode.GetSelection();
        }

        //詳細設定
        public const int PresetId = 0;
        public const int ForceJapaneseOptionId = 9999;

        public static int SnitchExposeTaskLeft = 1;


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
                option.UpdateSelection(count);
            }
        }

        public static int getRoleCount(CustomRoles role)
        {
            return CustomRoleCounts.TryGetValue(role, out var option) ? option.GetSelection() : roleCounts[role];
        }

        public static void Load()
        {
            if (IsLoaded) return;
            
            Preset = CustomOption.Create(0, new Color(204f / 255f, 204f / 255f, 0, 1f), "Preset", presets, presets[0], null, true)
                .HiddenOnDisplay(true)
                .SetGameMode(CustomGameMode.All);

            GameMode = CustomOption.Create(1, new Color(204f / 255f, 204f / 255f, 0, 1f), "GameMode", gameModes, gameModes[0], null, true)
                .SetGameMode(CustomGameMode.All);


            // === スタンダード役職 ===
            CustomRoleCounts = new Dictionary<CustomRoles, CustomOption>();
            CustomRoleSpawnChances = new Dictionary<CustomRoles, CustomOption>();
            
            SetupRoleOptions(CustomRoles.BountyHunter);
            BountyTargetChangeTime = CustomOption.Create(100, Color.white, "BountyTargetChangeTime", 150, 5, 1000, 5, CustomRoleSpawnChances[CustomRoles.BountyHunter]);
            BountySuccessKillCooldown = CustomOption.Create(100, Color.white, "BountySuccessKillCooldown", 2, 5, 999, 1, CustomRoleSpawnChances[CustomRoles.BountyHunter]);
            BountyFailureKillCooldown = CustomOption.Create(100, Color.white, "BountyFailureKillCooldown", 50, 5, 999, 1, CustomRoleSpawnChances[CustomRoles.BountyHunter]);
            BHDefaultKillCooldown = CustomOption.Create(100, Color.white, "BHDefaultKillCooldown", 30, 2, 999, 1, CustomRoleSpawnChances[CustomRoles.BountyHunter]);
            SetupRoleOptions(CustomRoles.SerialKiller);
            SerialKillerCooldown = CustomOption.Create(100, Color.white, "SerialKillerCooldown", 20, 5, 1000, 1, CustomRoleSpawnChances[CustomRoles.SerialKiller]);
            SerialKillerLimit = CustomOption.Create(100, Color.white, "SerialKillerLimit", 60, 5, 1000, 1, CustomRoleSpawnChances[CustomRoles.SerialKiller]);
            SetupRoleOptions(CustomRoles.ShapeMaster);
            ShapeMasterShapeshiftDuration = CustomOption.Create(100, Color.white, "ShapeMasterShapeshiftDuration", 10, 1, 1000, 1, CustomRoleSpawnChances[CustomRoles.ShapeMaster]);
            SetupRoleOptions(CustomRoles.Vampire);
            VampireKillDelay = CustomOption.Create(100, Color.white, "VampireKillDelay", 10, 1, 1000, 1, CustomRoleSpawnChances[CustomRoles.Vampire]);
            SetupRoleOptions(CustomRoles.Warlock);
            CanMakeMadmateCount = CustomOption.Create(100, Color.white, "CanMakeMadmateCount", 1, 1, 15, 1, CustomRoleSpawnChances[CustomRoles.Warlock]);
            SetupRoleOptions(CustomRoles.Witch);
            SetupRoleOptions(CustomRoles.Mafia);

            SetupRoleOptions(CustomRoles.Madmate);
            MadmateCanFixLightsOut = CustomOption.Create(100, Color.white, "MadmateCanFixLightsOut", false, CustomRoleSpawnChances[CustomRoles.Madmate]);
            MadmateCanFixComms = CustomOption.Create(100, Color.white, "MadmateCanFixComms", false, CustomRoleSpawnChances[CustomRoles.Madmate]);
            MadmateHasImpostorVision = CustomOption.Create(100, Color.white, "MadmateHasImpostorVision", false, CustomRoleSpawnChances[CustomRoles.Madmate]);
            SetupRoleOptions(CustomRoles.MadGuardian);
            MadGuardianCanSeeWhoTriedToKill = CustomOption.Create(100, Color.white, "MadGuardianCanSeeWhoTriedToKill", false, CustomRoleSpawnChances[CustomRoles.MadGuardian]);
            SetupRoleOptions(CustomRoles.MadSnitch);
            MadSnitchTasks = CustomOption.Create(100, Color.white, "MadSnitchTasks", 4, 1, 20, 1, CustomRoleSpawnChances[CustomRoles.MadSnitch]);

            SetupRoleOptions(CustomRoles.Bait);
            SetupRoleOptions(CustomRoles.Lighter);
            SetupRoleOptions(CustomRoles.Mayor);
            MayorAdditionalVote = CustomOption.Create(100, Color.white, "MayorAdditionalVote", 1, 1, 99, 1, CustomRoleSpawnChances[CustomRoles.Mayor]);
            SetupRoleOptions(CustomRoles.SabotageMaster);
            SabotageMasterSkillLimit = CustomOption.Create(100, Color.white, "SabotageMasterSkillLimit", 1, 0, 99, 1, CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
            SabotageMasterFixesDoors = CustomOption.Create(100, Color.white, "SabotageMasterFixesDoors", false, CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
            SabotageMasterFixesReactors  = CustomOption.Create(100, Color.white, "SabotageMasterFixesReactors", false, CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
            SabotageMasterFixesOxygens = CustomOption.Create(100, Color.white, "SabotageMasterFixesOxygens", false, CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
            SabotageMasterFixesComms = CustomOption.Create(100, Color.white, "SabotageMasterFixesCommunications", false, CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
            SabotageMasterFixesElectrical = CustomOption.Create(100, Color.white, "SabotageMasterFixesElectrical", false, CustomRoleSpawnChances[CustomRoles.SabotageMaster]);
            SetupRoleOptions(CustomRoles.Sheriff);
            SheriffKillCooldown = CustomOption.Create(100, Color.white, "SheriffKillCooldown", 30, 0, 990, 1, CustomRoleSpawnChances[CustomRoles.Sheriff]);
            SheriffCanKillMadmate = CustomOption.Create(100, Color.white, "SheriffCanKillMadmate", true, CustomRoleSpawnChances[CustomRoles.Sheriff]);
            SheriffCanKillJester = CustomOption.Create(100, Color.white, "SheriffCanKillJester", true, CustomRoleSpawnChances[CustomRoles.Sheriff]);
            SheriffCanKillTerrorist = CustomOption.Create(100, Color.white, "SheriffCanKillTerrorist", true, CustomRoleSpawnChances[CustomRoles.Sheriff]);
            SheriffCanKillOpportunist = CustomOption.Create(100, Color.white, "SheriffCanKillOpportunist", true, CustomRoleSpawnChances[CustomRoles.Sheriff]);
            SetupRoleOptions(CustomRoles.Snitch);

            SetupRoleOptions(CustomRoles.Jester);
            SetupRoleOptions(CustomRoles.Opportunist);
            SetupRoleOptions(CustomRoles.Terrorist);

            // HideAndSeek
            SetupRoleOptions(CustomRoles.Fox, CustomGameMode.HideAndSeek);
            SetupRoleOptions(CustomRoles.Troll, CustomGameMode.HideAndSeek);
            AllowCloseDoors = CustomOption.Create(count, Color.white, "AllowCloseDoors", false, null, true)
                .SetGameMode(CustomGameMode.HideAndSeek);
            KillDelay = CustomOption.Create(count, Color.white, "HideAndSeekWaitingTime", 10, 0, 180, 5)
                .SetGameMode(CustomGameMode.HideAndSeek);
            IgnoreCosmetics = CustomOption.Create(count, Color.white, "IgnoreCosmetics", false)
                .SetGameMode(CustomGameMode.HideAndSeek);
            IgnoreVent = CustomOption.Create(count, Color.white, "IgnoreVent", false)
                .SetGameMode(CustomGameMode.HideAndSeek);

            // ボタン回数同期
            SyncButtonMode = CustomOption.Create(count, Color.white, "SyncButtonMode", false, null, true)
                .SetGameMode(CustomGameMode.Standard);
            SyncedButtonCount = CustomOption.Create(count, Color.white, "SyncedButtonCount", 10, 0, 100, 1, SyncButtonMode)
                .SetGameMode(CustomGameMode.Standard);

            // タスク無効化
            DisableSwipeCard = CustomOption.Create(count, Color.white, "DisableSwipeCardTask", false, null, true)
                .SetGameMode(CustomGameMode.All);
            DisableSubmitScan = CustomOption.Create(count, Color.white, "DisableSubmitScanTask", false)
                .SetGameMode(CustomGameMode.All);
            DisableUnlockSafe = CustomOption.Create(count, Color.white, "DisableUnlockSafeTask", false)
                .SetGameMode(CustomGameMode.All);
            DisableUploadData = CustomOption.Create(count, Color.white, "DisableUploadDataTask", false)
                .SetGameMode(CustomGameMode.All);
            DisableStartReactor = CustomOption.Create(count, Color.white, "DisableStartReactorTask", false)
                .SetGameMode(CustomGameMode.All);
            DisableResetBreaker = CustomOption.Create(count, Color.white, "DisableResetBreakerTask", false)
                .SetGameMode(CustomGameMode.All);

            // ランダムマップ
            RandomMapsMode = CustomOption.Create(count, Color.white, "RandomMapsMode", false, null, true)
                .SetGameMode(CustomGameMode.All);
            AddedTheSkeld = CustomOption.Create(count, Color.white, "AddedTheSkeld", false, RandomMapsMode)
                .SetGameMode(CustomGameMode.All);
            AddedMiraHQ = CustomOption.Create(count, Color.white, "AddedMIRAHQ", false, RandomMapsMode)
                .SetGameMode(CustomGameMode.All);
            AddedPolus = CustomOption.Create(count, Color.white, "AddedPolus", false, RandomMapsMode)
                .SetGameMode(CustomGameMode.All);
            AddedTheAirShip = CustomOption.Create(count, Color.white, "AddedTheAirShip", false, RandomMapsMode)
                .SetGameMode(CustomGameMode.All);
            // MapDleks = CustomOption.Create(count, Color.white, "AddedDleks", false, RandomMapMode)
            //     .SetGameMode(CustomGameMode.All);

            NoGameEnd = CustomOption.Create(count, Color.white, "NoGameEnd", false, null, true)
                .SetGameMode(CustomGameMode.All);

            WhenSkipVote = CustomOption.Create(count, Color.white, "WhenSkipVote", voteModes, voteModes[0], null, true)
                .SetGameMode(CustomGameMode.Standard);
            WhenNonVote = CustomOption.Create(count, Color.white, "WhenNonVote", voteModes, voteModes[0], null, false)
                .SetGameMode(CustomGameMode.Standard);
            CanTerroristSuicideWin = CustomOption.Create(count, Color.white, "CanTerroristSuicideWin", false, null, false)
                .SetGameMode(CustomGameMode.Standard);


            ForceJapanese = CustomOption.Create(ForceJapaneseOptionId, Color.white, "ForceJapanese", false, null, true)
                .SetGameMode(CustomGameMode.All);
            AutoDisplayLastResult = CustomOption.Create(count, Color.white, "AutoDisplayLastResult", false)
                .SetGameMode(CustomGameMode.All);
            SuffixMode = CustomOption.Create(count, Color.white, "SuffixMode", suffixModes, suffixModes[0])
                .SetGameMode(CustomGameMode.All);

            IsLoaded = true;
        }

        private static int count = 100;

        private static void SetupRoleOptions(CustomRoles role, CustomGameMode customGameMode = CustomGameMode.Standard)
        {
            var spawnOption = CustomOption.Create(count, Utils.getRoleColor(role), Utils.getRoleName(role), rates, rates[0], null, true)
                .HiddenOnDisplay(true)
                .SetGameMode(customGameMode);
            var countOption = CustomOption.Create(count + 1, Color.white, "Maximum", 0, 0, 15, 1, spawnOption, false)
                .HiddenOnDisplay(true)
                .SetGameMode(customGameMode);

            CustomRoleSpawnChances.Add(role, spawnOption);
            CustomRoleCounts.Add(role, countOption);

            count += 2;
        }

    }
}
