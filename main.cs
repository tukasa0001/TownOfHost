using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using System;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using Hazel;
using System.Linq;

namespace TownOfHost
{
    [BepInPlugin(PluginGuid, "Town Of Host", PluginVersion)]
    [BepInProcess("Among Us.exe")]
    public class main : BasePlugin
    {
        //Sorry for many Japanese comments.
        public const string PluginGuid = "com.emptybottle.townofhost";
        public const string PluginVersion = "1.4";
        public const VersionTypes PluginVersionType = VersionTypes.Beta;
        public const string BetaVersion = "4";
        public const string BetaName = "BugFix Beta";
        public static string VersionSuffix => PluginVersionType == VersionTypes.Beta ? "b #" + BetaVersion : "";
        public Harmony Harmony { get; } = new Harmony(PluginGuid);
        public static Version version = Version.Parse(PluginVersion);
        public static BepInEx.Logging.ManualLogSource Logger;
        public static bool hasArgumentException = false;
        public static string ExceptionMessage;
        public static bool ExceptionMessageIsShown = false;
        public static string credentialsText;
        //Client Options
        public static ConfigEntry<bool> HideCodes {get; private set;}
        public static ConfigEntry<string> HideName {get; private set;}
        public static ConfigEntry<string> HideColor {get; private set;}
        public static ConfigEntry<bool> JapaneseRoleName {get; private set;}
        public static ConfigEntry<bool> AmDebugger {get; private set;}
        public static ConfigEntry<int> BanTimestamp {get; private set;}

        public static LanguageUnit EnglishLang {get; private set;}
        //Lang-arrangement
        private static Dictionary<lang, string> JapaneseTexts = new Dictionary<lang, string>();
        private static Dictionary<CustomRoles, string> JapaneseRoleNames = new Dictionary<CustomRoles, string>();
        private static Dictionary<PlayerState.DeathReason, string> JapaneseDeathReason = new Dictionary<PlayerState.DeathReason, string>(); 
        private static Dictionary<lang, string> EnglishTexts = new Dictionary<lang, string>();
        private static Dictionary<CustomRoles, string> EnglishRoleNames = new Dictionary<CustomRoles, string>();
        private static Dictionary<PlayerState.DeathReason, string> EnglishDeathReason = new Dictionary<PlayerState.DeathReason, string>();
        public static Dictionary<byte, PlayerVersion> playerVersion = new Dictionary<byte, PlayerVersion>();
        //Other Configs
        public static ConfigEntry<bool> IgnoreWinnerCommand { get; private set; }
        public static ConfigEntry<string> WebhookURL { get; private set; }
        public static CustomWinner currentWinner;
        public static HashSet<AdditionalWinners> additionalwinners = new HashSet<AdditionalWinners>();
        public static GameOptionsData RealOptionsData;
        public static PlayerState ps;
        public static bool IsHideAndSeek;
        public static bool AllowCloseDoors;
        public static bool IgnoreVent;
        public static bool IgnoreCosmetics;
        public static int HideAndSeekKillDelay;
        public static float HideAndSeekKillDelayTimer;
        public static float HideAndSeekImpVisionMin;

        public static Dictionary<byte, string> AllPlayerNames;
        public static Dictionary<byte, CustomRoles> AllPlayerCustomRoles;
        public static Dictionary<string, CustomRoles> lastAllPlayerCustomRoles;
        public static Dictionary<byte, bool> BlockKilling;
        public static bool SyncButtonMode;
        public static int SyncedButtonCount;
        public static int UsedButtonCount;
        public static bool RandomMapsMode;
        public static bool NoGameEnd;
        public static bool OptionControllerIsEnable;
        //タスク無効化
        public static bool DisableSwipeCard;
        public static bool DisableSubmitScan;
        public static bool DisableUnlockSafe;
        public static bool DisableUploadData;
        public static bool DisableStartReactor;
        //ランダムマップ
        public static bool AddedTheSkeld;
        public static bool AddedMIRAHQ;
        public static bool AddedPolus;
        public static bool AddedDleks;
        public static bool AddedTheAirShip;
        public static Dictionary<CustomRoles,String> roleColors;
        //これ変えたらmod名とかの色が変わる
        public static string modColor = "#00bfff";
        public static bool isFixedCooldown => VampireCount > 0;
        public static float RefixCooldownDelay = 0f;
        public static int BeforeFixMeetingCooldown = 10;
        public static bool forceJapanese = false;
        public static VoteMode whenSkipVote = VoteMode.Default;
        public static VoteMode whenNonVote = VoteMode.Default;
        public static bool canTerroristSuicideWin = false;
        public static List<byte> IgnoreReportPlayers;
        public static List<byte> winnerList;
        public static List<(string, byte)> MessagesToSend;
        public static bool autoDisplayLastRoles = false;

        public static bool isChatCommand = false;

        public static int SetRoleCountToggle(int currentCount)
        {
            if(currentCount > 0) return 0;
            else return 1;
        }
        public static int SetRoleCount(int currentCount, int addCount)
        {
            var fixedCount = currentCount * 10;
            fixedCount += addCount;
            fixedCount = Math.Clamp(fixedCount, 0, 99);
            return fixedCount;
        }
        public static void SetRoleCountToggle(CustomRoles role)
        {
            int count = GetCountFromRole(role);
            count = SetRoleCountToggle(count);
            SetCountFromRole(role, count);
        }
        public static void SetRoleCount(CustomRoles role, int addCount)
        {
            int count = GetCountFromRole(role);
            count = SetRoleCount(count, addCount);
            SetCountFromRole(role, count);
        }
        //Lang-Get
        //langのenumに対応した値をリストから持ってくる
        public static string getLang(lang lang)
        {
            var dic = TranslationController.Instance.CurrentLanguage.languageID == SupportedLangs.Japanese || forceJapanese ? JapaneseTexts : EnglishTexts;
            var isSuccess = dic.TryGetValue(lang, out var text);
            return isSuccess ? text : "<Not Found:" + lang.ToString() + ">";
        }
        public static string getRoleName(CustomRoles role) {
            var dic = (TranslationController.Instance.CurrentLanguage.languageID == SupportedLangs.Japanese || forceJapanese) &&
            JapaneseRoleName.Value == true ? JapaneseRoleNames : EnglishRoleNames;
            var isSuccess = dic.TryGetValue(role, out var text);
            return isSuccess ? text : "<Not Found:" + role.ToString() + ">";
        }
        public static string getDeathReason(PlayerState.DeathReason status)
        {
            var dic = TranslationController.Instance.CurrentLanguage.languageID == SupportedLangs.Japanese || forceJapanese ? JapaneseDeathReason : EnglishDeathReason;
            var isSuccess = dic.TryGetValue(status, out var text);
            return isSuccess ? text : "<Not Found:" + status.ToString() + ">";
        }
        public static Color getRoleColor(CustomRoles role)
        {
            if(!roleColors.TryGetValue(role, out var hexColor))hexColor = "#ffffff";
            ColorUtility.TryParseHtmlString(hexColor,out Color c);
            return c;
        }
        public static string getRoleColorCode(CustomRoles role)
        {
            if(!roleColors.TryGetValue(role, out var hexColor))hexColor = "#ffffff";
            return hexColor;
        }
        public static int GetCountFromRole(CustomRoles role) {
            int count;
            switch(role) {
                case CustomRoles.Jester:
                    count = JesterCount;
                    break;
                case CustomRoles.Madmate:
                    count = MadmateCount;
                    break;
                case CustomRoles.Bait:
                    count = BaitCount;
                    break;
                case CustomRoles.Terrorist:
                    count = TerroristCount;
                    break;
                case CustomRoles.Mafia:
                    count = MafiaCount;
                    break;
                case CustomRoles.Vampire:
                    count = VampireCount;
                    break;
                case CustomRoles.SabotageMaster:
                    count = SabotageMasterCount;
                    break;
                case CustomRoles.MadGuardian:
                    count = MadGuardianCount;
                    break;
                case CustomRoles.Mayor:
                    count = MayorCount;
                    break;
                case CustomRoles.Opportunist:
                    count = OpportunistCount;
                    break;
                case CustomRoles.Sheriff:
                    count = SheriffCount;
                    break;
                case CustomRoles.Snitch:
                    count = SnitchCount;
                    break;
                case CustomRoles.BountyHunter:
                    count = BountyHunterCount;
                    break;
                case CustomRoles.Witch:
                    count = WitchCount;
                    break;
                default:
                    return -1;
            }
            return count;
        }
        public static void SetCountFromRole(CustomRoles role, int count) {
            switch(role) {
                case CustomRoles.Jester:
                    JesterCount = count;
                    break;
                case CustomRoles.Madmate:
                    MadmateCount = count;
                    break;
                case CustomRoles.Bait:
                    BaitCount = count;
                    break;
                case CustomRoles.Terrorist:
                    TerroristCount = count;
                    break;
                case CustomRoles.Mafia:
                    MafiaCount = count;
                    break;
                case CustomRoles.Vampire:
                    VampireCount = count;
                    break;
                case CustomRoles.SabotageMaster:
                    SabotageMasterCount = count;
                    break;
                case CustomRoles.MadGuardian:
                    MadGuardianCount = count;
                    break;
                case CustomRoles.Mayor:
                    MayorCount = count;
                    break;
                case CustomRoles.Opportunist:
                    OpportunistCount = count;
                    break;
                case CustomRoles.Sheriff:
                    SheriffCount = count;
                    break;
                case CustomRoles.Snitch:
                    SnitchCount = count;
                    break;
                case CustomRoles.BountyHunter:
                    BountyHunterCount = count;
                    break;
                case CustomRoles.Witch:
                    WitchCount = count;
                    break;
            }
        }

        public static (string, Color) GetRoleText(PlayerControl player)
        {
            string RoleText = "Invalid Role";
            Color TextColor = Color.red;

            var cRole = player.getCustomRole();
            RoleText = getRoleName(cRole);

            return (RoleText, getRoleColor(cRole));
        }
        public static (string, Color) GetRoleTextHideAndSeek(RoleTypes oRole, CustomRoles hRole)
        {
            string text = "Invalid";
            Color color = Color.red;
            switch (oRole)
            {
                case RoleTypes.Impostor:
                case RoleTypes.Shapeshifter:
                    text = "Impostor";
                    color = Palette.ImpostorRed;
                    break;
                default:
                    switch (hRole)
                    {
                        case CustomRoles.Default:
                            text = "Crewmate";
                            color = Color.white;
                            break;
                        case CustomRoles.Fox:
                            text = "Fox";
                            color = Color.magenta;
                            break;
                        case CustomRoles.Troll:
                            text = "Troll";
                            color = Color.green;
                            break;
                    }
                    break;
            }
            return (text, color);
        }
        public static bool hasTasks(GameData.PlayerInfo p, bool ForRecompute = true)
        {
            //Tasksがnullの場合があるのでその場合タスク無しとする
            if (p.Tasks == null) return false;

            var hasTasks = true;
            if (p.Disconnected) hasTasks = false;
            if (main.IsHideAndSeek)
            {
                if(p.Role.IsImpostor)
                    hasTasks = false; //タスクはバニラ役職で判定される
                
                if (p.IsDead) hasTasks = false;
                var hasRole = main.AllPlayerCustomRoles.TryGetValue(p.PlayerId, out var role);
                if (hasRole)
                {
                    if (role == CustomRoles.Fox || role == CustomRoles.Troll) hasTasks = false;
                }
            } else {
                var cRoleFound = AllPlayerCustomRoles.TryGetValue(p.PlayerId, out var cRole);
                if(cRoleFound) {
                    if (cRole.isImpostor()) hasTasks = false;
                    if (cRole == CustomRoles.Jester) hasTasks = false;
                    if (cRole == CustomRoles.MadGuardian && ForRecompute) hasTasks = false;
                    if (cRole == CustomRoles.Opportunist) hasTasks = false;
                    if (cRole == CustomRoles.Sheriff) hasTasks = false;
                    if (cRole == CustomRoles.Madmate) hasTasks = false;
                    if (cRole == CustomRoles.Terrorist && ForRecompute) hasTasks = false;
                }
            }
            return hasTasks;
        }
        public static string getTaskText(Il2CppSystem.Collections.Generic.List<GameData.TaskInfo> tasks)
        {
            if(tasks == null) return "null";
            int CompletedTaskCount = 0;
            int AllTasksCount = 0;
            foreach (var task in tasks)
            {
                AllTasksCount++;
                if (task.Complete) CompletedTaskCount++;
            }
            return $"{CompletedTaskCount}/{AllTasksCount}";
        }

        public static void ShowActiveRoles()
        {
            main.SendToAll("現在有効な設定の説明:");
            if(main.IsHideAndSeek)
            {
                main.SendToAll(main.getLang(lang.HideAndSeekInfo));
                if(main.FoxCount > 0 ){ main.SendToAll(main.getLang(lang.FoxInfoLong)); }
                if(main.TrollCount > 0 ){ main.SendToAll(main.getLang(lang.TrollInfoLong)); }
            }else{
                if(main.SyncButtonMode){ main.SendToAll(main.getLang(lang.SyncButtonModeInfo)); }
                if(main.RandomMapsMode) { main.SendToAll(main.getLang(lang.RandomMapsModeInfo)); }
                if(main.VampireCount > 0) main.SendToAll(main.getLang(lang.VampireInfoLong));
                if(main.BountyHunterCount > 0) main.SendToAll(main.getLang(lang.BountyHunterInfoLong));
                if(main.WitchCount > 0) main.SendToAll(main.getLang(lang.WitchInfoLong));
                if(main.MafiaCount > 0) main.SendToAll(main.getLang(lang.MafiaInfoLong));
                if(main.MadmateCount > 0) main.SendToAll(main.getLang(lang.MadmateInfoLong));
                if(main.MadGuardianCount > 0) main.SendToAll(main.getLang(lang.MadGuardianInfoLong));
                if(main.JesterCount > 0) main.SendToAll(main.getLang(lang.JesterInfoLong));
                if(main.TerroristCount > 0) main.SendToAll(main.getLang(lang.TerroristInfoLong));
                if(main.OpportunistCount > 0) main.SendToAll(main.getLang(lang.OpportunistInfoLong));
                if(main.BaitCount > 0) main.SendToAll(main.getLang(lang.BaitInfoLong));
                if(main.MayorCount > 0) main.SendToAll(main.getLang(lang.MayorInfoLong));
                if(main.SabotageMasterCount > 0) main.SendToAll(main.getLang(lang.SabotageMasterInfoLong));
                if(main.SheriffCount > 0) main.SendToAll(main.getLang(lang.SheriffInfoLong));
                if(main.SnitchCount > 0) main.SendToAll(main.getLang(lang.SnitchInfoLong));
                // if(main.WarlockCount > 0) main.SendToAll(main.getLang(lang.WarlockInfoLong));
            }
            if(main.NoGameEnd){ main.SendToAll(main.getLang(lang.NoGameEndInfo)); }
        }

        public static void ShowActiveSettings()
        {
            var text = "役職:";
            if(main.IsHideAndSeek)
            {
                if(main.FoxCount > 0 ) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.Fox),main.FoxCount);
                if(main.TrollCount > 0 ) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.Troll),main.TrollCount);
                main.SendToAll(text);
                text = "設定:";
                text += main.getLang(lang.HideAndSeek);
            }else{
                if(main.VampireCount > 0) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.Vampire),main.VampireCount);
                if(main.BountyHunterCount > 0) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.BountyHunter),main.BountyHunterCount);
                if(main.WitchCount > 0) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.Witch),main.WitchCount);
                if(main.MafiaCount > 0) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.Mafia),main.MafiaCount);
                if(main.MadmateCount > 0) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.Madmate),main.MadmateCount);
                if(main.MadGuardianCount > 0)text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.MadGuardian),main.MadGuardianCount);
                if(main.JesterCount > 0) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.Jester),main.JesterCount);
                if(main.OpportunistCount > 0) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.Opportunist),main.OpportunistCount);
                if(main.TerroristCount > 0) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.Terrorist),main.TerroristCount);
                if(main.BaitCount > 0) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.Bait),main.BaitCount);
                if(main.MayorCount > 0) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.Mayor),main.MayorCount);
                if(main.SabotageMasterCount > 0) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.SabotageMaster),main.SabotageMasterCount);
                if(main.SheriffCount > 0) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.Sheriff),main.SheriffCount);
                if(main.SnitchCount > 0) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.Snitch),main.SnitchCount);
                main.SendToAll(text);
                text = "設定:";
                if(main.VampireCount > 0) text += String.Format("\n{0}:{1}",main.getLang(lang.VampireKillDelay),main.VampireKillDelay);
                if(main.SabotageMasterCount > 0)
                {
                    if(main.SabotageMasterSkillLimit > 0) text += String.Format("\n{0}:{1}",main.getLang(lang.SabotageMasterSkillLimit),main.SabotageMasterSkillLimit);
                    if(main.SabotageMasterFixesDoors) text += String.Format("\n{0}:{1}",main.getLang(lang.SabotageMasterFixesDoors),getOnOff(main.SabotageMasterFixesDoors));
                    if(main.SabotageMasterFixesReactors) text += String.Format("\n{0}:{1}",main.getLang(lang.SabotageMasterFixesReactors),getOnOff(main.SabotageMasterFixesReactors));
                    if(main.SabotageMasterFixesOxygens) text += String.Format("\n{0}:{1}",main.getLang(lang.SabotageMasterFixesOxygens),getOnOff(main.SabotageMasterFixesOxygens));
                    if(main.SabotageMasterFixesCommunications) text += String.Format("\n{0}:{1}",main.getLang(lang.SabotageMasterFixesCommunications),getOnOff(main.SabotageMasterFixesCommunications));
                    if(main.SabotageMasterFixesElectrical) text += String.Format("\n{0}:{1}",main.getLang(lang.SabotageMasterFixesElectrical),getOnOff(main.SabotageMasterFixesElectrical));
                }
                if (main.SheriffCount > 0)
                {
                    text += String.Format("\n{0}:{1}",main.getLang(lang.SheriffKillCooldown),main.SheriffKillCooldown);
                    if (main.SheriffCanKillJester) text += String.Format("\n{0}:{1}", main.getLang(lang.SheriffCanKillJester), getOnOff(main.SheriffCanKillJester));
                    if (main.SheriffCanKillTerrorist) text += String.Format("\n{0}:{1}", main.getLang(lang.SheriffCanKillTerrorist), getOnOff(main.SheriffCanKillTerrorist));
                    if (main.SheriffCanKillOpportunist) text += String.Format("\n{0}:{1}", main.getLang(lang.SheriffCanKillOpportunist), getOnOff(main.SheriffCanKillOpportunist));
                    if (main.SheriffCanKillMadmate) text += String.Format("\n{0}:{1}", main.getLang(lang.SheriffCanKillMadmate), getOnOff(main.SheriffCanKillMadmate));
                }
                if(main.MadGuardianCount > 0 || main.MadmateCount > 0)
                {
                    if(main.MadmateCanFixLightsOut) text += String.Format("\n{0}:{1}",main.getLang(lang.MadmateCanFixLightsOut),getOnOff(main.MadmateCanFixLightsOut));
                    if(main.MadmateCanFixComms) text += String.Format("\n{0}:{1}", main.getLang(lang.MadmateCanFixComms), getOnOff(main.MadmateCanFixComms));
                    if(main.MadmateHasImpostorVision) text += String.Format("\n{0}:{1}", main.getLang(lang.MadmateHasImpostorVision), getOnOff(main.MadmateHasImpostorVision));
                }
                if(main.MadGuardianCount > 0)
                {
                    if(main.MadGuardianCanSeeWhoTriedToKill) text += String.Format("\n{0}:{1}",main.getLang(lang.MadGuardianCanSeeWhoTriedToKill),getOnOff(main.MadGuardianCanSeeWhoTriedToKill));
                }
                if(main.MayorCount > 0) text += String.Format("\n{0}:{1}",main.getLang(lang.MayorAdditionalVote),main.MayorAdditionalVote);
                if(main.SyncButtonMode) text += String.Format("\n{0}:{1}",main.getLang(lang.SyncedButtonCount),main.SyncedButtonCount);
                if(main.whenSkipVote != VoteMode.Default) text += String.Format("\n{0}:{1}",main.getLang(lang.WhenSkipVote),main.whenSkipVote);
                if(main.whenNonVote != VoteMode.Default) text += String.Format("\n{0}:{1}",main.getLang(lang.WhenNonVote),main.whenNonVote);
                if((main.whenNonVote == VoteMode.Suicide || main.whenSkipVote == VoteMode.Suicide) && main.TerroristCount > 0) text += String.Format("\n{0}:{1}",main.getLang(lang.CanTerroristSuicideWin),main.canTerroristSuicideWin);
            }
            if(main.NoGameEnd)text += String.Format("\n{0,-14}",lang.NoGameEnd);
            main.SendToAll(text);
        }

        public static void ShowLastRoles()
        {
            var text = getLang(lang.LastResult);
            Dictionary<byte,CustomRoles> cloneRoles = new(AllPlayerCustomRoles);
            foreach(var id in winnerList)
            {
                text += $"\n★ {AllPlayerNames[id]}:{main.getRoleName(AllPlayerCustomRoles[id])}";
                text += $" {main.getDeathReason(ps.deathReasons[id])}";
                cloneRoles.Remove(id);
            }
            foreach (var kvp in cloneRoles)
            {
                var id = kvp.Key;
                text += $"\n　 {AllPlayerNames[id]} : {main.getRoleName(AllPlayerCustomRoles[id])}";
                text += $" {main.getDeathReason(ps.deathReasons[id])}";
            }
            main.SendToAll(text);
        }

        public static void ShowHelp()
        {
            main.SendToAll(
                "コマンド一覧:"
                +"\n/winner - 勝者を表示"
                +"\n/lastroles - 最後の役職割り当てを表示"
                +"\n/rename - ホストの名前を変更"
                +"\n/now - 現在有効な設定を表示"
                +"\n/h now - 現在有効な設定の説明を表示"
                +"\n/h roles <役職名> - 役職の説明を表示"
                +"\n/h modes <モード名> - モードの説明を表示"
                +"\n/dump - デスクトップにログを出力"
                );

        }
        public static Dictionary<byte, string> RealNames;
        public static string getOnOff(bool value) => value ? "ON" : "OFF";
        public static string TextCursor => TextCursorVisible ? "_" : "";
        public static bool TextCursorVisible;
        public static float TextCursorTimer;
        //Enabled Role
        public static int JesterCount;
        public static int MadmateCount;
        public static int BaitCount;
        public static int TerroristCount;
        public static int MafiaCount;
        public static int VampireCount;
        public static int SabotageMasterCount;
        public static int MadGuardianCount;
        public static int MayorCount;
        public static int OpportunistCount;
        public static int SheriffCount;
        public static int SnitchCount;
        public static int BountyHunterCount;
        public static int WitchCount;
        public static int FoxCount;
        public static int TrollCount;
        public static Dictionary<byte, (byte, float)> BitPlayers = new Dictionary<byte, (byte, float)>();
        public static Dictionary<byte, PlayerControl> BountyTargets;

        public static List <PlayerControl> SpelledPlayer = new List<PlayerControl>();
        public static Dictionary<byte, bool> KillOrSpell = new Dictionary<byte, bool>();
        public static bool witchMeeting;
        public static byte ExiledJesterID;
        public static byte WonTerroristID;
        public static bool CustomWinTrigger;
        public static bool VisibleTasksCount;
        public static int VampireKillDelay = 10;
        public static int SabotageMasterSkillLimit = 0;
        public static bool SabotageMasterFixesDoors;
        public static bool SabotageMasterFixesReactors;
        public static bool SabotageMasterFixesOxygens;
        public static bool SabotageMasterFixesCommunications;
        public static bool SabotageMasterFixesElectrical;
        public static int SabotageMasterUsedSkillCount;
        public static int SheriffKillCooldown;
        public static bool SheriffCanKillJester;
        public static bool SheriffCanKillTerrorist;
        public static bool SheriffCanKillOpportunist;
        public static bool SheriffCanKillMadmate;
        public static int MayorAdditionalVote;
        public static int SnitchExposeTaskLeft;

        public static bool MadmateCanFixLightsOut;
        public static bool MadmateCanFixComms;
        public static bool MadmateHasImpostorVision;
        public static bool MadGuardianCanSeeWhoTriedToKill;
        public static OverrideTasksData MadGuardianTasksData;
        public static OverrideTasksData TerroristTasksData;
        public static OverrideTasksData SnitchTasksData;
        public class OverrideTasksData {
            public CustomRoles TargetRole;
            public bool doOverride;
            public bool hasCommonTasks;
            public int NumLongTasks;
            public int NumShortTasks;
            public void CheckAndSet(
                CustomRoles currentTargetRole,
                ref bool doOverride,
                ref bool hasCommonTasks,
                ref int NumLongTasks,
                ref int NumShortTasks
            ) {
                if(currentTargetRole == this.TargetRole && this.doOverride) {
                    doOverride = true;
                    hasCommonTasks = this.hasCommonTasks;
                    NumLongTasks = this.NumLongTasks;
                    NumShortTasks = this.NumShortTasks;
                }
            }
            public void Serialize(MessageWriter writer) {
                writer.Write(doOverride);
                writer.Write(hasCommonTasks);
                writer.Write(NumLongTasks);
                writer.Write(NumShortTasks);
            }
            public static OverrideTasksData Deserialize(MessageReader reader, CustomRoles targetRole) {
                OverrideTasksData data = new OverrideTasksData(targetRole);
                data.doOverride = reader.ReadBoolean();
                data.hasCommonTasks = reader.ReadBoolean();
                data.NumLongTasks = reader.ReadInt32();
                data.NumShortTasks = reader.ReadInt32();
                return data;
            }
            public OverrideTasksData(CustomRoles TargetRole) {
                this.TargetRole = TargetRole;
                doOverride = false;
                hasCommonTasks = true;
                NumLongTasks = 1;
                NumShortTasks = 1;
            }
        }
        public static SuffixModes currentSuffix;
        public static string nickName = "";
        //SyncCustomSettingsRPC Sender
        public static void SyncCustomSettingsRPC()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, 80, Hazel.SendOption.Reliable, -1);
            writer.Write(JesterCount);
            writer.Write(MadmateCount);
            writer.Write(BaitCount);
            writer.Write(TerroristCount);
            writer.Write(MafiaCount);
            writer.Write(VampireCount);
            writer.Write(SabotageMasterCount);
            writer.Write(MadGuardianCount);
            writer.Write(MayorCount);
            writer.Write(OpportunistCount);
            writer.Write(SnitchCount);
            writer.Write(SheriffCount);
            writer.Write(BountyHunterCount);
            writer.Write(WitchCount);
            writer.Write(FoxCount);
            writer.Write(TrollCount);


            writer.Write(IsHideAndSeek);
            writer.Write(NoGameEnd);
            writer.Write(DisableSwipeCard);
            writer.Write(DisableSubmitScan);
            writer.Write(DisableUnlockSafe);
            writer.Write(DisableUploadData);
            writer.Write(DisableStartReactor);
            writer.Write(VampireKillDelay);
            writer.Write(SabotageMasterSkillLimit);
            writer.Write(SabotageMasterFixesDoors);
            writer.Write(SabotageMasterFixesReactors);
            writer.Write(SabotageMasterFixesOxygens);
            writer.Write(SabotageMasterFixesCommunications);
            writer.Write(SabotageMasterFixesElectrical);
            writer.Write(SheriffKillCooldown);
            writer.Write(SheriffCanKillJester);
            writer.Write(SheriffCanKillTerrorist);
            writer.Write(SheriffCanKillOpportunist);
            writer.Write(SheriffCanKillMadmate);
            writer.Write(SyncButtonMode);
            writer.Write(SyncedButtonCount);
            writer.Write((int)whenSkipVote);
            writer.Write((int)whenNonVote);
            writer.Write(canTerroristSuicideWin);
            writer.Write(AllowCloseDoors);
            writer.Write(HideAndSeekKillDelay);
            writer.Write(IgnoreVent);
            writer.Write(MadmateCanFixLightsOut);
            writer.Write(MadmateCanFixComms);
            writer.Write(MadmateHasImpostorVision);
            writer.Write(MadGuardianCanSeeWhoTriedToKill);
            writer.Write(MayorAdditionalVote);

            MadGuardianTasksData.Serialize(writer);
            TerroristTasksData.Serialize(writer);
            SnitchTasksData.Serialize(writer);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void PlaySoundRPC(byte PlayerID, Sounds sound)
        {
            if (AmongUsClient.Instance.AmHost)
                RPCProcedure.PlaySound(PlayerID, sound);
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PlaySound, Hazel.SendOption.Reliable, -1);
            writer.Write(PlayerID);
            writer.Write((byte)sound);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void CheckTerroristWin(GameData.PlayerInfo Terrorist)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            var isAllCompleted = true;
            foreach (var task in Terrorist.Tasks)
            {
                if (!task.Complete) isAllCompleted = false;
            }
            if (isAllCompleted && (!main.ps.isSuicide(Terrorist.PlayerId) || canTerroristSuicideWin)) //タスクが完了で（自殺じゃない OR 自殺勝ちが許可）されていれば
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.TerroristWin, Hazel.SendOption.Reliable, -1);
                writer.Write(Terrorist.PlayerId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPCProcedure.TerroristWin(Terrorist.PlayerId);
            }
        }
        public static void ExileAsync(PlayerControl player)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.Exiled, Hazel.SendOption.Reliable, -1);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            player.Exiled();
        }
        public static void RpcSetRole(PlayerControl targetPlayer, PlayerControl sendTo, RoleTypes role)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(targetPlayer.NetId, (byte)RpcCalls.SetRole, Hazel.SendOption.Reliable, sendTo.getClientId());
            writer.Write((byte)role);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void SendToAll(string text) => SendMessage(text);
        public static void SendMessage(string text, byte sendTo = byte.MaxValue)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            var tmp_text = text.Replace("#","＃");
            string[] textList = tmp_text.Split('\n');
            string tmp = "";
            var l = 0;
            foreach(string t in textList)
            {
                if(tmp.Length+t.Length < 120 && l < 4){
                    tmp += t+"\n";
                    l++;
                }else{
                    MessagesToSend.Add((tmp, sendTo));
                    tmp = t+"\n";
                    l = 1;
                }
            }
            if(tmp.Length != 0) MessagesToSend.Add((tmp, sendTo));
        }
        public static void ApplySuffix() {
            if(!AmongUsClient.Instance.AmHost) return;
            string name = SaveManager.PlayerName;
            if(nickName != "") name = nickName;
            if(!AmongUsClient.Instance.IsGameStarted)
            {
                switch(currentSuffix)
                {
                    case SuffixModes.None:
                        break;
                    case SuffixModes.TOH:
                        name += "\r\n<color=" + modColor + ">TOH v" + PluginVersion + VersionSuffix + "</color>";
                        break;
                    case SuffixModes.Streaming:
                        name += "\r\n配信中";
                        break;
                    case SuffixModes.Recording:
                        name += "\r\n録画中";
                        break;
                }
            }
            if(name != PlayerControl.LocalPlayer.name && PlayerControl.LocalPlayer.CurrentOutfitType == PlayerOutfitType.Default) PlayerControl.LocalPlayer.RpcSetName(name);
        }
        public static PlayerControl getPlayerById(int PlayerId) {
            return PlayerControl.AllPlayerControls.ToArray().Where(pc => pc.PlayerId == PlayerId).FirstOrDefault();
        }
        public static void NotifyRoles(bool isMeeting = false) {
            if(!AmongUsClient.Instance.AmHost) return;
            if(PlayerControl.AllPlayerControls == null) return;

            var caller = new System.Diagnostics.StackFrame(1, false);
            var callerMethod = caller.GetMethod();
            string callerMethodName = callerMethod.Name;
            string callerClassName = callerMethod.DeclaringType.FullName;
            TownOfHost.Logger.info("NotifyRolesが" + callerClassName + "." + callerMethodName + "から呼び出されました","NotifyRoles");
            HudManagerPatch.NowCallNotifyRolesCount++;
            HudManagerPatch.LastSetNameDesyncCount = 0;

            //Snitch警告表示のON/OFF
            bool ShowSnitchWarning = false;
            if(SnitchCount > 0) foreach(var snitch in PlayerControl.AllPlayerControls) {
                if(snitch.isSnitch() && !snitch.Data.IsDead && !snitch.Data.Disconnected) {
                    var taskState = snitch.getPlayerTaskState();
                    if(taskState.doExpose)
                        ShowSnitchWarning = true;
                }
            }

            //seer:ここで行われた変更を見ることができるプレイヤー
            //target:seerが見ることができる変更の対象となるプレイヤー
            foreach(var seer in PlayerControl.AllPlayerControls) {
                TownOfHost.Logger.info("NotifyRoles-Loop1-" + seer.name + ":START","NotifyRoles");
                //Loop1-bottleのSTART-END間でKeyNotFoundException
                //seerが落ちているときに何もしない
                if(seer.Data.Disconnected) continue;

                //seerがタスクを持っている：タスク残量の色コードなどを含むテキスト
                //seerがタスクを持っていない：空
                string SelfTaskText = hasTasks(seer.Data, false) ? $"<color=#ffff00>({main.getTaskText(seer.Data.Tasks)})</color>" : "";

                //Loversのハートマークなどを入れてください。
                string SelfMark = "";
                //インポスターに対するSnitch警告
                if(ShowSnitchWarning && seer.getCustomRole().isImpostor())
                    SelfMark += $"<color={main.getRoleColorCode(CustomRoles.Snitch)}>★</color>";

                //Markとは違い、改行してから追記されます。
                string SelfSuffix = "";

                if(seer.isBountyHunter() && seer.getBountyTarget() != null) {
                    string BountyTargetName = seer.getBountyTarget().getRealName(isMeeting);
                    SelfSuffix = $"<size=1.5>Target:{BountyTargetName}</size>";
                }
                if(seer.isWitch()) {
                    if(seer.GetKillOrSpell() == false) SelfSuffix = "Mode:" + main.getLang(lang.WitchModeKill);
                    if(seer.GetKillOrSpell() == true) SelfSuffix = "Mode:" + main.getLang(lang.WitchModeSpell);
                }

                //RealNameを取得 なければ現在の名前をRealNamesに書き込む
                string SeerRealName = seer.getRealName(isMeeting);

                //seerの役職名とSelfTaskTextとseerのプレイヤー名とSelfMarkを合成
                string SelfName = $"<size=1.5><color={seer.getRoleColorCode()}>{seer.getRoleName()}</color>{SelfTaskText}</size>\r\n<color={seer.getRoleColorCode()}>{SeerRealName}</color>{SelfMark}";
                SelfName += SelfSuffix == "" ? "" : "\r\n" + SelfSuffix;

                //適用
                seer.RpcSetNamePrivate(SelfName, true);
                HudManagerPatch.LastSetNameDesyncCount++;

                //他人用の変数定義
                bool SeerKnowsImpostors = false; //trueの時、インポスターの名前が赤色に見える
                //タスクを終えたSnitchがインポスターを確認できる
                if(seer.isSnitch()) {
                    var TaskState = seer.getPlayerTaskState();
                    if(TaskState.isTaskFinished)
                        SeerKnowsImpostors = true;
                }

                //二週目のループを実行するかどうかを決める処理
                //このリストに入ってあるいずれかのFuncがtrueを返したとき、そのプレイヤーをtargetとしたループを実行する
                //このリストの中身が空の時、foreach自体が実行されなくなる
                List<Func<PlayerControl, bool>> conditions = new List<Func<PlayerControl, bool>>();
                
                //seerが死んでいる
                if(seer.Data.IsDead) 
                    //常時
                    conditions.Add(target => true);
                
                //seerがインポスターを知っている
                if(SeerKnowsImpostors) 
                    //targetがインポスター
                    conditions.Add(target => target.getCustomRole().isImpostor());

                //seerがインポスターで、タスクが終わりそうなSnitchがいる
                if(seer.getCustomRole().isImpostor() && ShowSnitchWarning) 
                    //targetがSnitch
                    conditions.Add(target => target.isSnitch());

                //seer視点用の名前色データが一つ以上ある
                if(NameColorManager.Instance.GetDatasBySeer(seer.PlayerId).Count > 0) 
                    //seer視点用のtargetに対する名前色データが存在する
                    conditions.Add(target => NameColorManager.Instance.GetData(seer.PlayerId, target.PlayerId).color != null);
                
                //seerが死んでいる場合など、必要なときのみ第二ループを実行する
                if(conditions.Count > 0) foreach(var target in PlayerControl.AllPlayerControls) {
                    //targetがseer自身の場合は何もしない
                    if(target == seer) continue;

                    //conditions内のデータによる判定
                    bool doCancel = true;
                    foreach(var func in conditions) {
                        if(func(target)) {
                            doCancel = false;
                            break;
                        }
                    }
                    if(doCancel) continue;
                    
                    TownOfHost.Logger.info("NotifyRoles-Loop2-" + target.name + ":START","NotifyRoles");

                    //他人のタスクはtargetがタスクを持っているかつ、seerが死んでいる場合のみ表示されます。それ以外の場合は空になります。
                    string TargetTaskText = hasTasks(target.Data, false) && seer.Data.IsDead ? $"<color=#ffff00>({main.getTaskText(target.Data.Tasks)})</color>" : "";

                    //Loversのハートマークなどを入れてください。
                    string TargetMark = "";
                    //タスク完了直前のSnitchにマークを表示
                    if(target.isSnitch() && seer.getCustomRole().isImpostor()) {
                        var taskState = target.getPlayerTaskState();
                        if(taskState.doExpose)
                            TargetMark += $"<color={main.getRoleColorCode(CustomRoles.Snitch)}>★</color>";
                    }

                    //他人の役職とタスクはtargetがタスクを持っているかつ、seerが死んでいる場合のみ表示されます。それ以外の場合は空になります。
                    string TargetRoleText = seer.Data.IsDead ? $"<size=1.5><color={target.getRoleColorCode()}>{target.getRoleName()}</color>{TargetTaskText}</size>\r\n" : "";

                    //RealNameを取得 なければ現在の名前をRealNamesに書き込む
                    string TargetPlayerName = target.getRealName(isMeeting);

                    //ターゲットのプレイヤー名の色を書き換えます。
                    if(SeerKnowsImpostors && target.getCustomRole().isImpostor()) //Seerがインポスターが誰かわかる状態
                        TargetPlayerName = "<color=#ff0000>" + TargetPlayerName + "</color>";
                    else {//NameColorManager準拠の処理
                        var ncd = NameColorManager.Instance.GetData(seer.PlayerId, target.PlayerId);
                        TargetPlayerName = ncd.OpenTag + TargetPlayerName + ncd.CloseTag;
                    }

                    //全てのテキストを合成します。
                    string TargetName = $"{TargetRoleText}{TargetPlayerName}{TargetMark}";
                    //適用
                    target.RpcSetNamePrivate(TargetName, true, seer);
                    HudManagerPatch.LastSetNameDesyncCount++;

                    TownOfHost.Logger.info("NotifyRoles-Loop2-" + target.name + ":END","NotifyRoles");
                }
                TownOfHost.Logger.info("NotifyRoles-Loop1-" + seer.name + ":END","NotifyRoles");
            }
            main.witchMeeting = false;
        }
        public static void CustomSyncAllSettings() {
            foreach(var pc in PlayerControl.AllPlayerControls) {
                pc.CustomSyncSettings();
            }
        }

        public static void ChangeInt(ref int ChangeTo, int input, int max) {
            var tmp = ChangeTo * 10;
            tmp += input;
            ChangeTo = Math.Clamp(tmp,0,max);
        }

        public override void Load()
        {
            TextCursorTimer = 0f;
            TextCursorVisible = true;

            //Client Options
            HideCodes = Config.Bind("Client Options", "Hide Game Codes", false);
            HideName = Config.Bind("Client Options", "Hide Game Code Name", "Town Of Host");
            HideColor = Config.Bind("Client Options", "Hide Game Code Color", $"{main.modColor}");
            JapaneseRoleName = Config.Bind("Client Options", "Japanese Role Name", false);

            Logger = BepInEx.Logging.Logger.CreateLogSource("TownOfHost");
            TownOfHost.Logger.enable();
            TownOfHost.Logger.disable("NotifyRoles");

            currentWinner = CustomWinner.Default;
            additionalwinners = new HashSet<AdditionalWinners>();

            RealNames = new Dictionary<byte, string>();

            IsHideAndSeek = false;
            AllowCloseDoors = false;
            IgnoreVent = false;
            IgnoreCosmetics = false;
            HideAndSeekKillDelay = 30;
            HideAndSeekKillDelayTimer = 0f;
            HideAndSeekImpVisionMin = 0.25f;
            TrollCount = 0;
            FoxCount = 0;
            AllPlayerCustomRoles = new Dictionary<byte, CustomRoles>();

            SyncButtonMode = false;
            SyncedButtonCount = 10;
            UsedButtonCount = 0;

            whenSkipVote = VoteMode.Default;
            whenNonVote = VoteMode.Default;
            canTerroristSuicideWin = false;

            NoGameEnd = false;
            CustomWinTrigger = false;
            OptionControllerIsEnable = false;
            BitPlayers = new Dictionary<byte, (byte, float)>();
            BountyTargets = new Dictionary<byte, PlayerControl>();
            SpelledPlayer = new List<PlayerControl>();
            winnerList = new();
            VisibleTasksCount = false;
            MessagesToSend = new List<(string, byte)>();

            DisableSwipeCard = false;
            DisableSubmitScan = false;
            DisableUnlockSafe = false;
            DisableUploadData = false;
            DisableStartReactor = false;

            VampireKillDelay = 10;

            SabotageMasterSkillLimit = 0;
            SabotageMasterFixesDoors = false;
            SabotageMasterFixesReactors = true;
            SabotageMasterFixesOxygens = true;
            SabotageMasterFixesCommunications = true;
            SabotageMasterFixesElectrical = true;

            SheriffKillCooldown = 30;
            SheriffCanKillJester = true;
            SheriffCanKillTerrorist = true;
            SheriffCanKillOpportunist = false;
            SheriffCanKillMadmate = true;

            MadmateCanFixLightsOut = false;
            MadmateCanFixComms = false;
            MadmateHasImpostorVision = true;
            MadGuardianCanSeeWhoTriedToKill = false;

            MadGuardianTasksData = new OverrideTasksData(CustomRoles.MadGuardian);
            TerroristTasksData = new OverrideTasksData(CustomRoles.Terrorist);
            SnitchTasksData = new OverrideTasksData(CustomRoles.Snitch);

            MayorAdditionalVote = 1;

            SnitchExposeTaskLeft = 1;

            currentSuffix = SuffixModes.None;

            IgnoreWinnerCommand = Config.Bind("Other", "IgnoreWinnerCommand", true);
            WebhookURL = Config.Bind("Other", "WebhookURL", "none");
            AmDebugger = Config.Bind("Other", "AmDebugger", false);
            BanTimestamp = Config.Bind("Other", "lastTime", 0);

            CustomOptionController.begin();
            NameColorManager.Begin();

            BlockKilling = new Dictionary<byte, bool>();

            hasArgumentException = false;
            ExceptionMessage = "";
            try {

            roleColors = new Dictionary<CustomRoles, string>(){
                {CustomRoles.Default, "#ffffff"},
                {CustomRoles.Engineer, "#00ffff"},
                {CustomRoles.Scientist, "#00ffff"},
                {CustomRoles.GuardianAngel, "#ffffff"},
                {CustomRoles.Impostor, "#ff0000"},
                {CustomRoles.Shapeshifter, "#ff0000"},
                {CustomRoles.Vampire, "#ff0000"},
                {CustomRoles.Mafia, "#ff0000"},
                {CustomRoles.Madmate, "#ff0000"},
                {CustomRoles.MadGuardian, "#ff0000"},
                {CustomRoles.Jester, "#ec62a5"},
                {CustomRoles.Terrorist, "#00ff00"},
                {CustomRoles.Opportunist, "#00ff00"},
                {CustomRoles.Bait, "#00f7ff"},
                {CustomRoles.SabotageMaster, "#0000ff"},
                {CustomRoles.Snitch, "#b8fb4f"},
                {CustomRoles.Mayor, "#204d42"},
                {CustomRoles.Sheriff, "#f8cd46"},
                {CustomRoles.BountyHunter, "#ff0000"},
                {CustomRoles.Witch, "#ff0000"},
                {CustomRoles.Fox, "#e478ff"},
                {CustomRoles.Troll, "#00ff00"}
            };

            JapaneseTexts = new Dictionary<lang, string>(){
                //役職解説(短)
                {lang.JesterInfo, "追放されよう"},
                {lang.MadmateInfo, "インポスターの援助をしよう"},
                {lang.TeamImpostor, "インポスター陣営"},
                {lang.MadGuardianInfo, "タスクを済ませ、インポスターの援助をしよう"},
                {lang.BaitInfo, "敵を罠にはめよう"},
                {lang.TerroristInfo, "タスクを済ませ、自爆しよう"},
                {lang.MafiaInfo, "インポスターの援助をしよう"},
                {lang.BeforeMafiaInfo,"今はキルをすることができない"},
                {lang.AfterMafiaInfo,"サボを活用して皆殺しにしよう"},
                {lang.VampireInfo, "クルーを噛んで全滅させよう"},
                {lang.SabotageMasterInfo, "より早くサボタージュを直そう"},
                {lang.MayorInfo, "自分の票が何倍もの力を持っている"},
                {lang.OpportunistInfo, "とにかく生き残りましょう"},
                {lang.SnitchInfo, "タスクを早く済ませよう"},
                {lang.SheriffInfo, "インポスターを撃ち抜け"},
                {lang.BountyHunterInfo, "標的を確実に仕留めよう"},
                {lang.WitchInfo, "敵に魔術をかけよう"},
                {lang.FoxInfo, "とにかく生き残りましょう"},
                {lang.TrollInfo, "自爆しよう"},
                //役職解説(長)
                {lang.JesterInfoLong, "ジェスター(第三陣営):\n会議で追放された時に単独勝利できる。\n追放されず試合が終了orキルされると敗北。"},
                {lang.MadmateInfoLong, "マッドメイト(インポスター陣営):\nクルーだがインポスターの味方をする。\nインポスターと互いに認識できない。\nベントが使え、タスクはない。"},
                {lang.MadGuardianInfoLong, "マッドガーディアン(インポスター陣営):\nクルーだがインポスターの味方をする。\nインポスターと互いに認識できない。\nタスクを全て完了させるとｼｰﾙﾄﾞが付与される。"},
                {lang.BaitInfoLong, "ベイト(クルーメイト陣営):\nキルされた時に、自身をキルした人に\n強制的にセルフレポートさせる事ができる。"},
                {lang.TerroristInfoLong, "テロリスト(第三陣営):\n自身のタスクを全て完了させた状態で\nキル・追放された時に単独勝利する。"},
                {lang.MafiaInfoLong, "マフィア(インポスター陣営):\n初期状態ではキルが封じられている。\n仲間が全員死亡後、自身もキルが可能になる。"},
                {lang.VampireInfoLong, "ヴァンパイア(インポスター陣営):\nプレイヤーを噛む事で遅延キルが可能。\n(時間が経過or会議が始まると死亡する。)\nベイトの場合即死亡し、強制的に通報が入る。"},
                {lang.SabotageMasterInfoLong, "サボタージュマスター(クルーメイト陣営):\nリアクター・O2・通信妨害を一人で修理可能。\n停電は一箇所のレバーに触れる事で全て直る。\nドアを開けるとその部屋の全てのドアが開く。"},
                {lang.MayorInfoLong, "メイヤー(クルーメイト陣営):\n会議の票を複数持ち、まとめて一人または\nスキップに入れることができる。(設定有)"},
                {lang.OpportunistInfoLong, "オポチュニスト(第三陣営):\n試合終了時に生存していれば追加勝利となる。"},
                {lang.SnitchInfoLong, "スニッチ(クルーメイト陣営):\nタスク完了と同時にインポスターの名前の色が\n赤く表示され逆に自身のタスクが少なくなると\n名前の横に★が表示されｲﾝﾎﾟｽﾀｰに暴かれる。"},
                {lang.SheriffInfoLong, "シェリフ(クルーメイト陣営):\nインポスター陣営をキルすることができる。\n※第三陣営をキル可能にする設定有。\nｸﾙｰﾒｲﾄをキルすると自殺する。タスクはない。"},
                {lang.BountyHunterInfoLong, "バウンティハンター(インポスター陣営):\n標的をキルすると次のｷﾙｸｰﾙが半分になる。\n標的以外をキルするとｷﾙｸｰﾙは通常になる。"},
                {lang.WitchInfoLong, "魔女(インポスター陣営):\nキルをすると「Kill」と「Spell」が切り変わる。\nSpellの表示でキルすると呪うことが出来る。\n呪いをかけられた人は会議で「†」が付き魔女を追放しなければ会議後に死亡する。"},
                {lang.FoxInfoLong, "狐(HideAndSeek):\nトロールを除くいずれかの陣営が勝利したときに生き残っていれば、勝利した陣営に追加で勝利することができる。"},
                {lang.TrollInfoLong, "トロール(HideAndSeek):\nインポスターにキルされたときに単独勝利となる。この場合、狐が生き残っていても狐は敗北となる。"},
                //モード名
                {lang.HideAndSeek, "HideAndSeek"},
                {lang.NoGameEnd, "NoGameEnd"},
                {lang.SyncButtonMode, "ボタン回数同期モード"},
                {lang.RandomMapsMode, "ランダムマップモード"},
                //モード解説
                {lang.HideAndSeekInfo, "HideAndSeek:会議を開くことはできず、クルーはタスク完了、インポスターは全クルー殺害でのみ勝利することができる。サボタージュ、アドミン、カメラ、待ち伏せなどは禁止事項である。(設定有)"},
                {lang.NoGameEndInfo, "NoGameEnd:勝利判定が存在しないデバッグ用のモード。ホストのSHIFT+L以外でのゲーム終了ができない。"},
                {lang.SyncButtonModeInfo, "ボタン回数同期モード:プレイヤー全員のボタン回数が同期されているモード。(設定有)"},
                {lang.RandomMapsModeInfo, "ランダムマップモード:ランダムにマップが変わるモード。(設定有)"},
                //オプション項目
                {lang.AdvancedRoleOptions, "詳細設定"},
                {lang.VampireKillDelay, "ヴァンパイアの殺害までの時間(秒)"},
                {lang.MadmateCanFixLightsOut, "マッドメイト系役職が停電を直すことができる"},
                {lang.MadmateCanFixComms, "マッドメイト系役職がコミュサボを直すことができる"},
                {lang.MadmateHasImpostorVision, "マッドメイト系役職の視界がインポスターと同じ"},
                {lang.MadGuardianCanSeeWhoTriedToKill, "マッドガーディアンが自身をキルしたインポスターを見ることができる"},
                {lang.SabotageMasterSkillLimit, "ｻﾎﾞﾀｰｼﾞｭﾏｽﾀｰがｻﾎﾞﾀｰｼﾞｭに対して能力を使用できる回数(ﾄﾞｱ閉鎖は除く)"},
                {lang.SabotageMasterFixesDoors, "ｻﾎﾞﾀｰｼﾞｭﾏｽﾀｰが1度に複数のﾄﾞｱを開けることを許可する"},
                {lang.SabotageMasterFixesReactors, "ｻﾎﾞﾀｰｼﾞｭﾏｽﾀｰが原子炉ﾒﾙﾄﾀﾞｳﾝに対して能力を"},
                {lang.SabotageMasterFixesOxygens, "ｻﾎﾞﾀｰｼﾞｭﾏｽﾀｰが酸素妨害に対して能力を使える"},
                {lang.SabotageMasterFixesCommunications, "ｻﾎﾞﾀｰｼﾞｭﾏｽﾀｰがMIRA HQの通信妨害に対して能力を使える"},
                {lang.SabotageMasterFixesElectrical, "ｻﾎﾞﾀｰｼﾞｭﾏｽﾀｰが停電に対して能力を使える"},
                {lang.SheriffKillCooldown, "シェリフのキルクールダウン"},
                {lang.SheriffCanKillJester, "シェリフがジェスターをキルできる"},
                {lang.SheriffCanKillTerrorist, "シェリフがテロリストをキルできる"},
                {lang.SheriffCanKillOpportunist, "シェリフがオポチュニストをキルできる"},
                {lang.SheriffCanKillMadmate, "シェリフがマッドメイト系役職をキルできる"},
                {lang.MayorAdditionalVote, "メイヤーの追加投票の個数"},
                {lang.HideAndSeekOptions, "HideAndSeekの設定"},
                {lang.AllowCloseDoors, "ドア閉鎖を許可する"},
                {lang.HideAndSeekWaitingTime, "インポスターの待機時間(秒)"},
                {lang.IgnoreCosmetics, "装飾品を禁止する"},
                {lang.IgnoreVent, "通気口の使用を禁止する"},
                {lang.HideAndSeekRoles, "HideAndSeekの役職"},
                {lang.SyncedButtonCount, "合計ボタン使用可能回数(回)"},
                {lang.DisableTasks, "タスクを無効化する"},
                {lang.DisableSwipeCardTask, "カードタスクを無効化する"},
                {lang.DisableSubmitScanTask, "医務室のスキャンタスクを無効化する"},
                {lang.DisableUnlockSafeTask, "金庫タスクを無効化する"},
                {lang.DisableUploadDataTask, "ダウンロードタスクを無効化する"},
                {lang.DisableStartReactorTask, "原子炉起動タスクを無効化する"},
                {lang.AddedTheSkeld, "TheSkeldを追加"},
                {lang.AddedMIRAHQ, "MIRAHQを追加"},
                {lang.AddedPolus, "Polusを追加"},
                {lang.AddedDleks, "Dleksを追加"},
                {lang.AddedTheAirShip, "TheAirShipを追加"},
                {lang.SuffixMode, "名前の二行目"},
                {lang.WhenSkipVote, "スキップ時"},
                {lang.WhenNonVote, "無投票時"},
                {lang.OverrideTaskCount, "役職ごとのタスク数"},
                {lang.doOverride, "%role%のタスクを上書きする"},
                {lang.hasCommonTasks, "%role%に通常タスクを割り当てる"},
                {lang.roleLongTasksNum, "%role%のロングタスクの数"},
                {lang.roleShortTasksNum, "%role%のショートタスクの数"},
                //その他
                {lang.WitchCurrentMode, "現在のモード"},
                {lang.WitchModeKill, "キル"},
                {lang.WitchModeSpell, "スペル"},
                {lang.BountyCurrentTarget, "現在のターゲット"},
                {lang.RoleOptions, "役職設定"},
                {lang.ModeOptions, "モード設定"},
                {lang.ForceJapanese, "日本語に強制"},
                {lang.AutoDisplayLastResult,"ゲーム結果の自動表示"},
                {lang.LastResult,"ゲーム結果:"},
                {lang.VoteMode, "投票モード"},
                {lang.Default, "デフォルト"},
                {lang.Suicide, "切腹"},
                {lang.SelfVote, "自投票"},
                {lang.CanTerroristSuicideWin, "テロリストの自殺勝ち"},
                {lang.commandError, "エラー:%1$"},
                {lang.InvalidArgs, "無効な引数"},
                {lang.ON, "ON"},
                {lang.OFF, "OFF"},
                {lang.Win, "勝利"},
            };
            EnglishTexts = new Dictionary<lang, string>(){
                //役職解説(短)
                {lang.JesterInfo, "Get voted out"},
                {lang.MadmateInfo, "Help the Impostors"},
                {lang.TeamImpostor, "Team Impostor"},
                {lang.MadGuardianInfo, "Finish your tasks to help the Impostors"},
                {lang.BaitInfo, "Bait your enemies"},
                {lang.TerroristInfo, "Die after finishing your tasks"},
                {lang.MafiaInfo, "Help the Impostors to kill everyone"},
                {lang.BeforeMafiaInfo,"You can not kill now"},
                {lang.AfterMafiaInfo,"Kill all Crewmates"},
                {lang.VampireInfo, "Kill the Crewmates with your bites"},
                {lang.SabotageMasterInfo, "Fix sabotages faster"},
                {lang.MayorInfo, "Your vote counts twice"},
                {lang.OpportunistInfo, "Do whatever it takes to survive"},
                {lang.SnitchInfo, "Finish your tasks to find the Impostors"},
                {lang.SheriffInfo, "Shoot the Impostors"},
                {lang.BountyHunterInfo, "Hunt your bounty down"},
                {lang.WitchInfo, "Spell your enemies"},
                {lang.FoxInfo, "Do whatever it takes to survive"},
                {lang.TrollInfo, "Die to win"},
                //役職解説(長)
                {lang.JesterInfoLong, "Jester(Neutral):\n会議で追放された時に単独勝利できる。\n追放されず試合が終了orキルされると敗北。"},
                {lang.MadmateInfoLong, "Madmate(Impostor):\nクルーだがインポスターの味方をする。\nインポスターと互いに認識できない。\nベントが使え、タスクはない。"},
                {lang.MadGuardianInfoLong, "MadGuardian(Impostor):\nクルーだがインポスターの味方をする。\nインポスターと互いに認識できない。\nタスクを全て完了させるとｼｰﾙﾄﾞが付与される。"},
                {lang.BaitInfoLong, "Bait(Crewmate):\nキルされた時に、自身をキルした人に\n強制的にセルフレポートさせる事ができる。"},
                {lang.TerroristInfoLong, "Terrorist(Neutral):\n自身のタスクを全て完了させた状態で\nキル・追放された時に単独勝利する。"},
                {lang.MafiaInfoLong, "Mafia(Impostor):\n初期状態ではキルが封じられている。\n仲間が全員死亡後、自身もキルが可能になる。"},
                {lang.VampireInfoLong, "Vampire(Impostor):\nプレイヤーを噛む事で遅延キルが可能。\n(時間が経過or会議が始まると死亡する。)\nベイトの場合即死亡し、強制的に通報が入る。"},
                {lang.SabotageMasterInfoLong, "SabotageMaster(Crewmate):\nリアクター・O2・通信妨害を一人で修理可能。\n停電は一箇所のレバーに触れる事で全て直る。\nドアを開けるとその部屋の全てのドアが開く。"},
                {lang.MayorInfoLong, "Mayor(Crewmate):\n会議の票を複数持ち、まとめて一人または\nスキップに入れることができる。(設定有)"},
                {lang.OpportunistInfoLong, "Opportunist(Neutral):\n試合終了時に生存していれば追加勝利となる。"},
                {lang.SnitchInfoLong, "Snitch(Crewmate):\nタスク完了と同時にインポスターの名前の色が\n赤く表示され逆に自身のタスクが少なくなると\n名前の横に★が表示されｲﾝﾎﾟｽﾀｰに暴かれる。"},
                {lang.SheriffInfoLong, "Sheriff(Crewmate):\nインポスター陣営をキルすることができる。\n※第三陣営をキル可能にする設定有。\nｸﾙｰﾒｲﾄをキルすると自殺する。タスクはない。"},
                {lang.BountyHunterInfoLong, "BountyHunter(Impostor):\n標的をキルすると次のｷﾙｸｰﾙが半分になる。\n標的以外をキルするとｷﾙｸｰﾙは通常になる。"},
                {lang.WitchInfoLong, "Witch(Impostor):\nキルをすると「Kill」と「Spell」が切り変わる。\nSpellの表示でキルすると呪うことが出来る。\n呪いをかけられた人は会議で「†」が付き魔女を追放しなければ会議後に死亡する。"},
                {lang.FoxInfoLong, "Fox(HideAndSeek):\nTrollを除くいずれかの陣営が勝利したときに生き残っていれば、勝利した陣営に追加で勝利することができる。"},
                {lang.TrollInfoLong, "Troll(HideAndSeek):\nImpostorにキルされたときに単独勝利となる。この場合、Foxが生き残っていてもFoxは敗北となる。"},
                //モード名
                {lang.HideAndSeek, "HideAndSeek"},
                {lang.NoGameEnd, "NoGameEnd"},
                {lang.SyncButtonMode, "SyncButtonMode"},
                {lang.RandomMapsMode, "RandomMapsMode"},
                //モード解説
                {lang.HideAndSeekInfo, "HideAndSeek:会議を開くことはできず、Crewmateはタスク完了、Impostorは全クルー殺害でのみ勝利することができる。サボタージュ、アドミン、カメラ、待ち伏せなどは禁止事項である。クルーは青、インポスターは赤になる。(設定有)"},
                {lang.NoGameEndInfo, "NoGameEnd:勝利判定が存在しないデバッグ用のモード。ホストのSHIFT+L以外でのゲーム終了ができない。"},
                {lang.SyncButtonModeInfo, "SyncButtonMode:プレイヤー全員のボタン回数が同期されているモード。(設定有)"},
                {lang.RandomMapsModeInfo, "RandomMapsMode:ランダムにマップが変わるモード。(設定有)"},
                //オプション項目
                {lang.AdvancedRoleOptions, "Advanced Options"},
                {lang.VampireKillDelay, "Vampire Kill Delay(s)"},
                {lang.SabotageMasterSkillLimit, "SabotageMaster Fixes Sabotage Limit(Ignore Closing Doors)"},
                {lang.MadmateCanFixLightsOut, "Madmate Type Roles Can Fix Lights Out"},
                {lang.MadmateCanFixComms, "Madmate Type Roles Can Fix Comms"},
                {lang.MadmateHasImpostorVision, "Madmate Has Impostor Vision"},
                {lang.MadGuardianCanSeeWhoTriedToKill, "MadGuardian Can See Who Tried To Kill"},
                {lang.SabotageMasterFixesDoors, "SabotageMaster Can Fixes Multiple Doors"},
                {lang.SabotageMasterFixesReactors, "SabotageMaster Can Fixes Both Reactors"},
                {lang.SabotageMasterFixesOxygens, "SabotageMaster Can Fixes Both O2"},
                {lang.SabotageMasterFixesCommunications, "SabotageMaster Can Fixes Both Communications In MIRA HQ"},
                {lang.SabotageMasterFixesElectrical, "SabotageMaster Can Fixes Lights Out All At Once"},
                {lang.SheriffKillCooldown, "Sheriff Kill Cooldown"},
                {lang.SheriffCanKillJester, "Sheriff Can Kill Jester"},
                {lang.SheriffCanKillTerrorist, "Sheriff Can Kill Terrorist"},
                {lang.SheriffCanKillOpportunist, "Sheriff Can Kill Opportunist"},
                {lang.SheriffCanKillMadmate, "Sheriff Can Kill Madmate Type Roles"},
                {lang.MayorAdditionalVote, "Mayor Additional Votes Count"},
                {lang.HideAndSeekOptions, "HideAndSeek Options"},
                {lang.AllowCloseDoors, "Allow Closing Doors"},
                {lang.HideAndSeekWaitingTime, "Impostor Waiting Time"},
                {lang.IgnoreCosmetics, "Ignore Cosmetics"},
                {lang.IgnoreVent, "Ignore Using Vents"},
                {lang.HideAndSeekRoles, "HideAndSeek Roles"},
                {lang.SyncedButtonCount, "Max Button Count"},
                {lang.DisableTasks, "Disable Tasks"},
                {lang.DisableSwipeCardTask, "Disable SwipeCard Tasks"},
                {lang.DisableSubmitScanTask, "Disable SubmitScan Tasks"},
                {lang.DisableUnlockSafeTask, "Disable UnlockSafe Tasks"},
                {lang.DisableUploadDataTask, "Disable UploadData Tasks"},
                {lang.DisableStartReactorTask, "Disable StartReactor Tasks"},
                {lang.AddedTheSkeld, "Added TheSkeld"},
                {lang.AddedMIRAHQ, "Added MIRAHQ"},
                {lang.AddedPolus, "Added Polus"},
                {lang.AddedDleks, "Added Dleks"},
                {lang.AddedTheAirShip, "Added TheAirShip"},
                {lang.SuffixMode, "Suffix"},
                {lang.WhenSkipVote, "When Skip Vote"},
                {lang.WhenNonVote, "When Non-Vote"},
                {lang.OverrideTaskCount, "Amount Of Tasks By Roles"},
                {lang.doOverride, "Override %role%'s Tasks"},
                {lang.hasCommonTasks, "Assign Common Tasks For %role%"},
                {lang.roleLongTasksNum, "Amount Of Long Tasks For %role%"},
                {lang.roleShortTasksNum, "Amount Of Short Tasks For %role%"},
                //その他
                {lang.WitchCurrentMode, "Current Mode"},
                {lang.WitchModeKill, "Kill"},
                {lang.WitchModeSpell, "Spell"},
                {lang.BountyCurrentTarget, "Current Target"},
                {lang.RoleOptions, "Role Options"},
                {lang.ModeOptions, "Mode Options"},
                {lang.ForceJapanese, "Force Japanese"},
                {lang.AutoDisplayLastResult,"Auto Display Last Result"},
                {lang.LastResult,"Game Result:"},
                {lang.VoteMode, "VoteMode"},
                {lang.Default, "Default"},
                {lang.Suicide, "Suicide"},
                {lang.SelfVote, "SelfVote"},
                {lang.CanTerroristSuicideWin, "Can Terrorist Suicide Win"},
                {lang.commandError, "Error:%1$"},
                {lang.InvalidArgs, "Invalid Args"},
                {lang.ON, "ON"},
                {lang.OFF, "OFF"},
                {lang.Win, " Wins"},
            };
            EnglishRoleNames = new Dictionary<CustomRoles, string>(){
                {CustomRoles.Default, "Crewmate"},
                {CustomRoles.Engineer, "Engineer"},
                {CustomRoles.Scientist, "Scientist"},
                {CustomRoles.GuardianAngel, "GuardianAngel"},
                {CustomRoles.Impostor, "Impostor"},
                {CustomRoles.Shapeshifter, "Shapeshifter"},
                {CustomRoles.Jester, "Jester"},
                {CustomRoles.Madmate, "Madmate"},
                {CustomRoles.MadGuardian, "MadGuardian"},
                {CustomRoles.Bait, "Bait"},
                {CustomRoles.Terrorist, "Terrorist"},
                {CustomRoles.Mafia, "Mafia"},
                {CustomRoles.Vampire, "Vampire"},
                {CustomRoles.SabotageMaster, "SabotageMaster"},
                {CustomRoles.Mayor, "Mayor"},
                {CustomRoles.Opportunist, "Opportunist"},
                {CustomRoles.Snitch, "Snitch"},
                {CustomRoles.Sheriff, "Sheriff"},
                {CustomRoles.BountyHunter, "BountyHunter"},
                {CustomRoles.Witch, "Witch"},
                {CustomRoles.Fox, "Fox"},
                {CustomRoles.Troll, "Troll"},
            };
            JapaneseRoleNames = new Dictionary<CustomRoles, string>(){
                {CustomRoles.Default, "クルー"},
                {CustomRoles.Engineer, "エンジニア"},
                {CustomRoles.Scientist, "科学者"},
                {CustomRoles.GuardianAngel, "守護天使"},
                {CustomRoles.Impostor, "インポスター"},
                {CustomRoles.Shapeshifter, "シェイプシフター"},
                {CustomRoles.Jester, "ジェスター"},
                {CustomRoles.Madmate, "マッドメイト"},
                {CustomRoles.MadGuardian, "マッドガーディアン"},
                {CustomRoles.Bait, "ベイト"},
                {CustomRoles.Terrorist, "テロリスト"},
                {CustomRoles.Mafia, "マフィア"},
                {CustomRoles.Vampire, "ヴァンパイア"},
                {CustomRoles.SabotageMaster, "サボタージュマスター"},
                {CustomRoles.Mayor, "メイヤー"},
                {CustomRoles.Opportunist, "オポチュニスト"},
                {CustomRoles.Snitch, "スニッチ"},
                {CustomRoles.Sheriff, "シェリフ"},
                {CustomRoles.BountyHunter, "バウンティハンター"},
                {CustomRoles.Witch, "魔女"},
                {CustomRoles.Fox, "狐"},
                {CustomRoles.Troll, "トロール"},
            };
            EnglishDeathReason = new Dictionary<PlayerState.DeathReason, string>(){
                {PlayerState.DeathReason.Kill,"Kill" },
                {PlayerState.DeathReason.Vote,"Vote" },
                {PlayerState.DeathReason.Suicide,"Suicide" },
                {PlayerState.DeathReason.Spell,"Spelled" },
                {PlayerState.DeathReason.etc,"Living" },
            };
            JapaneseDeathReason = new Dictionary<PlayerState.DeathReason, string>(){
                {PlayerState.DeathReason.Kill,"死亡" },
                {PlayerState.DeathReason.Vote,"追放" },
                {PlayerState.DeathReason.Suicide,"自爆" },
                {PlayerState.DeathReason.Spell,"呪殺" },
                {PlayerState.DeathReason.etc,"生存" },
            };


            }
            catch (ArgumentException ex) {
                TownOfHost.Logger.error("エラー:Dictionaryの値の重複を検出しました");
                TownOfHost.Logger.error(ex.Message);
                hasArgumentException = true;
                ExceptionMessage = ex.Message;
                ExceptionMessageIsShown = false;
            }
            TownOfHost.Logger.info($"{nameof(ThisAssembly.Git.Branch)}: {ThisAssembly.Git.Branch}","GitVersion");
            TownOfHost.Logger.info($"{nameof(ThisAssembly.Git.BaseTag)}: {ThisAssembly.Git.BaseTag}","GitVersion");
            TownOfHost.Logger.info($"{nameof(ThisAssembly.Git.Commit)}: {ThisAssembly.Git.Commit}","GitVersion");
            TownOfHost.Logger.info($"{nameof(ThisAssembly.Git.Commits)}: {ThisAssembly.Git.Commits}","GitVersion");
            TownOfHost.Logger.info($"{nameof(ThisAssembly.Git.IsDirty)}: {ThisAssembly.Git.IsDirty}","GitVersion");
            TownOfHost.Logger.info($"{nameof(ThisAssembly.Git.Sha)}: {ThisAssembly.Git.Sha}","GitVersion");
            TownOfHost.Logger.info($"{nameof(ThisAssembly.Git.Tag)}: {ThisAssembly.Git.Tag}","GitVersion");

            Harmony.PatchAll();
        }

        [HarmonyPatch(typeof(TranslationController), nameof(TranslationController.Awake))]
        class TranslationControllerAwakePatch {
            public static void Postfix(TranslationController __instance) {
                var english = __instance.Languages.Where(lang => lang.languageID == SupportedLangs.English).FirstOrDefault();
                EnglishLang = new LanguageUnit(english);
            }
        }
    }
    //Lang-enum
    public enum lang
    {
        //役職解説(短)
        JesterInfo = 0,
        MadmateInfo,
        TeamImpostor,
        BaitInfo,
        TerroristInfo,
        MafiaInfo,
        BeforeMafiaInfo,
        AfterMafiaInfo,
        VampireInfo,
        SabotageMasterInfo,
        MadGuardianInfo,
        MayorInfo,
        OpportunistInfo,
        SnitchInfo,
        SheriffInfo,
        BountyHunterInfo,
        WitchInfo,
        FoxInfo,
        TrollInfo,
        //役職解説(長)
        JesterInfoLong,
        MadmateInfoLong,
        BaitInfoLong,
        TerroristInfoLong,
        MafiaInfoLong,
        VampireInfoLong,
        SabotageMasterInfoLong,
        MadGuardianInfoLong,
        MayorInfoLong,
        OpportunistInfoLong,
        SnitchInfoLong,
        SheriffInfoLong,
        BountyHunterInfoLong,
        WitchInfoLong,
        FoxInfoLong,
        TrollInfoLong,
        //モード名
        HideAndSeek,
        SyncButtonMode,
        NoGameEnd,
        DisableTasks,
        RandomMapsMode,
        //モード解説
        HideAndSeekInfo,
        SyncButtonModeInfo,
        NoGameEndInfo,
        RandomMapsModeInfo,
        //オプション項目
        AdvancedRoleOptions,
        VampireKillDelay,
        MadmateCanFixLightsOut,
        MadmateCanFixComms,
        MadmateHasImpostorVision,
        MadGuardianCanSeeWhoTriedToKill,
        SabotageMasterFixesDoors,
        SabotageMasterSkillLimit,
        SabotageMasterFixesReactors,
        SabotageMasterFixesOxygens,
        SabotageMasterFixesCommunications,
        SabotageMasterFixesElectrical,
        SheriffKillCooldown,
        SheriffCanKillJester,
        SheriffCanKillTerrorist,
        SheriffCanKillOpportunist,
        SheriffCanKillMadmate,
        MayorAdditionalVote,
        HideAndSeekOptions,
        AllowCloseDoors,
        HideAndSeekWaitingTime,
        IgnoreCosmetics,
        IgnoreVent,
        HideAndSeekRoles,
        SyncedButtonCount,
        DisableSwipeCardTask,
        DisableSubmitScanTask,
        DisableUnlockSafeTask,
        DisableUploadDataTask,
        DisableStartReactorTask,
        SuffixMode,
        WhenSkipVote,
        WhenNonVote,
        AddedTheSkeld,
        AddedMIRAHQ,
        AddedPolus,
        AddedDleks,
        AddedTheAirShip,
        //その他
        WitchCurrentMode,
        WitchModeKill,
        WitchModeSpell,
        BountyCurrentTarget,
        RoleOptions,
        ModeOptions,
        ForceJapanese,
        AutoDisplayLastResult,
        LastResult,
        VoteMode,
        Default,
        Suicide,
        SelfVote,
        CanTerroristSuicideWin,
        commandError,
        InvalidArgs,
        OverrideTaskCount,
        doOverride,
        hasCommonTasks,
        roleLongTasksNum,
        roleShortTasksNum,
        ON,
        OFF,
        Win,
    }
    public enum CustomRoles {
        Default = 0,
        Engineer,
        Scientist,
        Impostor,
        Shapeshifter,
        GuardianAngel,
        Jester,
        Madmate,
        Bait,
        Terrorist,
        Mafia,
        Vampire,
        SabotageMaster,
        MadGuardian,
        Mayor,
        Opportunist,
        Snitch,
        Sheriff,
        BountyHunter,
        Witch,
        Fox,
        Troll
    }
    //WinData
    public enum CustomWinner
    {
        Draw = 0,
        Default,
        Impostor,
        Crewmate,
        Jester,
        Terrorist,
        Troll
    }
    public enum AdditionalWinners
    {
        None = 0,
        Opportunist,
        Fox
    }
    /*public enum CustomRoles : byte
    {
        Default = 0,
        Troll = 1,
        Fox = 2
    }*/
    public enum SuffixModes {
        None = 0,
        TOH,
        Streaming,
        Recording
    }
    public enum VersionTypes
    {
        Released = 0,
        Beta = 1
    }

    public enum VoteMode
    {
        Default,
        Suicide,
        SelfVote
    }
}
