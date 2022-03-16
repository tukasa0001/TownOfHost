using System.Linq;
using System;
using System.Collections.Generic;
namespace TownOfHost
{
    public static class Options
    {
        public static Dictionary<CustomRoles, int> roleCounts;
        public static bool OptionControllerIsEnable = false;

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

        static Options()
        {
            resetRoleCounts();
        }
        public static void resetRoleCounts()
        {
            roleCounts = new Dictionary<CustomRoles, int>();
            foreach (var role in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>())
            {
                roleCounts.Add(role, 0);
            }
        }
        public static void setRoleCount(CustomRoles role, int count) { roleCounts[role] = count; }
        public static int getRoleCount(CustomRoles role) { return roleCounts[role]; }
    }
}
