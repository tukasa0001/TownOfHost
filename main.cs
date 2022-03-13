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
using static TownOfHost.Translator;

namespace TownOfHost
{
    [BepInPlugin(PluginGuid, "Town Of Host", PluginVersion)]
    [BepInProcess("Among Us.exe")]
    public class main : BasePlugin
    {
        //Sorry for many Japanese comments.
        public const string PluginGuid = "com.emptybottle.townofhost";
        public const string PluginVersion = "1.5.0";
        public const VersionTypes PluginVersionType = VersionTypes.Beta;
        public const string BetaVersion = "1";
        public const string BetaName = "**** Beta";
        public static string VersionSuffix => PluginVersionType == VersionTypes.Beta ? "b #" + BetaVersion : "";
        public Harmony Harmony { get; } = new Harmony(PluginGuid);
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
        public static bool DisableResetBreaker;
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
        public static string getRoleName(CustomRoles role) {
            var lang = (TranslationController.Instance.CurrentLanguage.languageID == SupportedLangs.Japanese || forceJapanese) &&
            JapaneseRoleName.Value == true ? SupportedLangs.Japanese : SupportedLangs.English;
            return getString(Enum.GetName(typeof(CustomRoles),role),lang);
        }
        public static string getDeathReason(PlayerState.DeathReason status)
        {
            return getString(Enum.GetName(typeof(PlayerState.DeathReason),status));
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
                case CustomRoles.SKMadmate:
                    count = SKMadmateCount;
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
                case CustomRoles.MadSnitch:
                    count = MadSnitchCount;
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
                case CustomRoles.ShapeMaster:
                    count = ShapeMasterCount;
                    break;
                case CustomRoles.Warlock:
                    count = WarlockCount;
                    break;
                case CustomRoles.SerialKiller:
                    count = SerialKillerCount;
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
                case CustomRoles.SKMadmate:
                    SKMadmateCount = count;
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
                case CustomRoles.MadSnitch:
                    MadSnitchCount = count;
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
                case CustomRoles.ShapeMaster:
                    ShapeMasterCount = count;
                    break;
                case CustomRoles.Warlock:
                    WarlockCount = count;
                    break;
                case CustomRoles.SerialKiller:
                    SerialKillerCount = count;
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
                        case CustomRoles.Crewmate:
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
                    if (cRole == CustomRoles.MadSnitch && ForRecompute) hasTasks = false;
                    if (cRole == CustomRoles.Opportunist) hasTasks = false;
                    if (cRole == CustomRoles.Sheriff) hasTasks = false;
                    if (cRole == CustomRoles.Madmate) hasTasks = false;
                    if (cRole == CustomRoles.SKMadmate) hasTasks = false;
                    if (cRole == CustomRoles.Terrorist && ForRecompute) hasTasks = false;
                }
            }
            return hasTasks;
        }
        public static string getTaskText(PlayerControl pc)
        {
            var taskState = pc.getPlayerTaskState();
            if (!taskState.hasTasks) return "null";
            return $"{taskState.CompletedTasksCount}/{taskState.AllTasksCount}";
        }

        public static void ShowActiveRoles()
        {
            main.SendToAll("現在有効な設定の説明:");
            if(main.IsHideAndSeek)
            {
                main.SendToAll(getString("HideAndSeekInfo"));
                if(main.FoxCount > 0 ){ main.SendToAll(getString("FoxInfoLong")); }
                if(main.TrollCount > 0 ){ main.SendToAll(getString("TrollInfoLong")); }
            }else{
                if(main.SyncButtonMode){ main.SendToAll(getString("SyncButtonModeInfo")); }
                if(main.RandomMapsMode) { main.SendToAll(getString("RandomMapsModeInfo")); }
                if(main.VampireCount > 0) main.SendToAll(getString("VampireInfoLong"));
                if(main.BountyHunterCount > 0) main.SendToAll(getString("BountyHunterInfoLong"));
                if(main.WitchCount > 0) main.SendToAll(getString("WitchInfoLong"));
                if(main.WarlockCount > 0) main.SendToAll(getString("WarlockInfoLong"));
                if(main.SerialKillerCount > 0) main.SendToAll(getString("SerialKillerInfoLong"));
                if(main.MafiaCount > 0) main.SendToAll(getString("MafiaInfoLong"));
                if(main.ShapeMasterCount > 0) main.SendToAll(getString("ShapeMasterInfoLong"));
                if(main.MadmateCount > 0) main.SendToAll(getString("MadmateInfoLong"));
                if(main.SKMadmateCount > 0) main.SendToAll(getString("SKMadmateInfoLong"));
                if(main.MadGuardianCount > 0) main.SendToAll(getString("MadGuardianInfoLong"));
                if(main.MadSnitchCount > 0) main.SendToAll(getString("MadSnitchInfoLong"));
                if(main.JesterCount > 0) main.SendToAll(getString("JesterInfoLong"));
                if(main.TerroristCount > 0) main.SendToAll(getString("TerroristInfoLong"));
                if(main.OpportunistCount > 0) main.SendToAll(getString("OpportunistInfoLong"));
                if(main.BaitCount > 0) main.SendToAll(getString("BaitInfoLong"));
                if(main.MayorCount > 0) main.SendToAll(getString("MayorInfoLong"));
                if(main.SabotageMasterCount > 0) main.SendToAll(getString("SabotageMasterInfoLong"));
                if(main.SheriffCount > 0) main.SendToAll(getString("SheriffInfoLong"));
                if(main.SnitchCount > 0) main.SendToAll(getString("SnitchInfoLong"));
            }
            if(main.NoGameEnd){ main.SendToAll(getString("NoGameEndInfo")); }
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
                text += getString("HideAndSeek");
            }else{
                if(main.VampireCount > 0) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.Vampire),main.VampireCount);
                if(main.BountyHunterCount > 0) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.BountyHunter),main.BountyHunterCount);
                if(main.WitchCount > 0) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.Witch),main.WitchCount);
                if(main.WarlockCount > 0) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.Warlock),main.WarlockCount);
                if(main.SerialKillerCount > 0) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.SerialKiller),main.SerialKillerCount);;
                if(main.MafiaCount > 0) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.Mafia),main.MafiaCount);
                if(main.ShapeMasterCount > 0) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.ShapeMaster),main.ShapeMasterCount);
                if(main.MadmateCount > 0) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.Madmate),main.MadmateCount);
                if(main.SKMadmateCount > 0) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.SKMadmate),main.SKMadmateCount);
                if(main.MadGuardianCount > 0)text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.MadGuardian),main.MadGuardianCount);
                if(main.MadSnitchCount > 0)text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.MadSnitch),main.MadSnitchCount);
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
                if(main.VampireCount > 0) text += String.Format("\n{0}:{1}",getString("VampireKillDelay"),main.VampireKillDelay);
                if(main.SabotageMasterCount > 0)
                {
                    if(main.SabotageMasterSkillLimit > 0) text += String.Format("\n{0}:{1}",getString("SabotageMasterSkillLimit"),main.SabotageMasterSkillLimit);
                    if(main.SabotageMasterFixesDoors) text += String.Format("\n{0}:{1}",getString("SabotageMasterFixesDoors"),getOnOff(main.SabotageMasterFixesDoors));
                    if(main.SabotageMasterFixesReactors) text += String.Format("\n{0}:{1}",getString("SabotageMasterFixesReactors"),getOnOff(main.SabotageMasterFixesReactors));
                    if(main.SabotageMasterFixesOxygens) text += String.Format("\n{0}:{1}",getString("SabotageMasterFixesOxygens"),getOnOff(main.SabotageMasterFixesOxygens));
                    if(main.SabotageMasterFixesCommunications) text += String.Format("\n{0}:{1}",getString("SabotageMasterFixesCommunications"),getOnOff(main.SabotageMasterFixesCommunications));
                    if(main.SabotageMasterFixesElectrical) text += String.Format("\n{0}:{1}",getString("SabotageMasterFixesElectrical"),getOnOff(main.SabotageMasterFixesElectrical));
                }
                if (main.SheriffCount > 0)
                {
                    text += String.Format("\n{0}:{1}",getString("SheriffKillCooldown"),main.SheriffKillCooldown);
                    if (main.SheriffCanKillJester) text += String.Format("\n{0}:{1}", getString("SheriffCanKillJester"), getOnOff(main.SheriffCanKillJester));
                    if (main.SheriffCanKillTerrorist) text += String.Format("\n{0}:{1}", getString("SheriffCanKillTerrorist"), getOnOff(main.SheriffCanKillTerrorist));
                    if (main.SheriffCanKillOpportunist) text += String.Format("\n{0}:{1}", getString("SheriffCanKillOpportunist"), getOnOff(main.SheriffCanKillOpportunist));
                    if (main.SheriffCanKillMadmate) text += String.Format("\n{0}:{1}", getString("SheriffCanKillMadmate"), getOnOff(main.SheriffCanKillMadmate));
                }
                if(main.MadGuardianCount > 0 || main.MadSnitchCount > 0 || main.MadmateCount > 0 || main.SKMadmateCount > 0)
                {
                    if(main.MadmateVisionAsImpostor) text += String.Format("\n{0}:{1}",getString("MadmateVisionAsImpostor"),getOnOff(main.MadmateVisionAsImpostor));
                    if(main.MadmateCanFixLightsOut) text += String.Format("\n{0}:{1}",getString("MadmateCanFixLightsOut"),getOnOff(main.MadmateCanFixLightsOut));
                    if(main.MadmateCanFixComms) text += String.Format("\n{0}:{1}", getString("MadmateCanFixComms"), getOnOff(main.MadmateCanFixComms));
                }
                if(main.MadGuardianCount > 0)
                {
                    if(main.MadGuardianCanSeeWhoTriedToKill) text += String.Format("\n{0}:{1}",getString("MadGuardianCanSeeWhoTriedToKill"),getOnOff(main.MadGuardianCanSeeWhoTriedToKill));
                }
                if(main.MadSnitchCount > 0)text += String.Format("\n{0}:{1}",getString("MadSnitchTasks"),main.MadSnitchTasks);
                if(main.MayorCount > 0) text += String.Format("\n{0}:{1}",getString("MayorAdditionalVote"),main.MayorAdditionalVote);
                if(main.SyncButtonMode) text += String.Format("\n{0}:{1}",getString("SyncedButtonCount"),main.SyncedButtonCount);
                if(main.whenSkipVote != VoteMode.Default) text += String.Format("\n{0}:{1}",getString("WhenSkipVote"),main.whenSkipVote);
                if(main.whenNonVote != VoteMode.Default) text += String.Format("\n{0}:{1}",getString("WhenNonVote"),main.whenNonVote);
                if((main.whenNonVote == VoteMode.Suicide || main.whenSkipVote == VoteMode.Suicide) && main.TerroristCount > 0) text += String.Format("\n{0}:{1}",getString("CanTerroristSuicideWin"),main.canTerroristSuicideWin);
            }
            if(main.NoGameEnd)text += String.Format("\n{0,-14}",getString("NoGameEnd"));
            main.SendToAll(text);
        }

        public static void ShowLastRoles()
        {
            var text = getString("LastResult");
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
        public static int SKMadmateCount;
        public static int BaitCount;
        public static int TerroristCount;
        public static int MafiaCount;
        public static int VampireCount;
        public static int SabotageMasterCount;
        public static int MadGuardianCount;
        public static int MadSnitchCount;
        public static int MayorCount;
        public static int OpportunistCount;
        public static int SheriffCount;
        public static int SnitchCount;
        public static int BountyHunterCount;
        public static int WitchCount;
        public static int ShapeMasterCount;
        public static int WarlockCount;
        public static int SerialKillerCount;
        public static int FoxCount;
        public static int TrollCount;
        public static Dictionary<byte, (byte, float)> BitPlayers = new Dictionary<byte, (byte, float)>();
        public static Dictionary<byte, float> SerialKillerTimer = new Dictionary<byte, float>();
        public static Dictionary<byte, float> BountyTimer = new Dictionary<byte, float>();
        public static Dictionary<byte, PlayerControl> BountyTargets;
        public static Dictionary<byte, bool> isTargetKilled = new Dictionary<byte, bool>();
        public static Dictionary<byte, PlayerControl> CursedPlayers = new Dictionary<byte, PlayerControl>();
        public static List<PlayerControl> CursedPlayerDie = new List<PlayerControl>();
        public static List <PlayerControl> SpelledPlayer = new List<PlayerControl>();
        public static Dictionary<byte, bool> KillOrSpell = new Dictionary<byte, bool>();
        public static Dictionary<byte, bool> FirstCursedCheck = new Dictionary<byte, bool>();
        public static int SKMadmateNowCount;
        public static bool witchMeeting;
        public static bool isShipStart;
        public static bool BountyMeetingCheck;
        public static bool isBountyKillSuccess;
        public static bool BountyTimerCheck;
        public static int ShapeMasterShapeshiftDuration;
        public static Dictionary<byte, bool> CheckShapeshift = new Dictionary<byte, bool>();
        public static int SerialKillerCooldown;
        public static int SerialKillerLimit;
        public static int BountyTargetChangeTime;
        public static int BountySuccessKillCooldown;
        public static int BHDefaultKillCooldown;//キルクールを2.5秒にしないとバグるのでこちらを追加。
        public static int BountyFailureKillCooldown;
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

        public static bool MadmateVisionAsImpostor;
        public static bool MadmateCanFixLightsOut;
        public static bool MadmateCanFixComms;
        public static int CanMakeMadmateCount;
        public static bool MadGuardianCanSeeWhoTriedToKill;
        public static int MadSnitchTasks;
        public static SuffixModes currentSuffix;
        public static string nickName = "";
        //SyncCustomSettingsRPC Sender
        public static void SyncCustomSettingsRPC()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, 80, Hazel.SendOption.Reliable, -1);
            writer.Write(JesterCount);
            writer.Write(MadmateCount);
            writer.Write(SKMadmateCount);
            writer.Write(BaitCount);
            writer.Write(TerroristCount);
            writer.Write(MafiaCount);
            writer.Write(VampireCount);
            writer.Write(SabotageMasterCount);
            writer.Write(MadGuardianCount);
            writer.Write(MadSnitchCount);
            writer.Write(MayorCount);
            writer.Write(OpportunistCount);
            writer.Write(SnitchCount);
            writer.Write(SheriffCount);
            writer.Write(BountyHunterCount);
            writer.Write(WitchCount);
            writer.Write(ShapeMasterCount);
            writer.Write(WarlockCount);
            writer.Write(SerialKillerCount);
            writer.Write(FoxCount);
            writer.Write(TrollCount);


            writer.Write(IsHideAndSeek);
            writer.Write(NoGameEnd);
            writer.Write(DisableSwipeCard);
            writer.Write(DisableSubmitScan);
            writer.Write(DisableUnlockSafe);
            writer.Write(DisableUploadData);
            writer.Write(DisableStartReactor);
            writer.Write(DisableResetBreaker);
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
            writer.Write(MadmateVisionAsImpostor);
            writer.Write(CanMakeMadmateCount);
            writer.Write(MadGuardianCanSeeWhoTriedToKill);
            writer.Write(MadSnitchTasks);
            writer.Write(MayorAdditionalVote);
            writer.Write(SerialKillerCooldown);
            writer.Write(SerialKillerLimit);
            writer.Write(BountyTargetChangeTime);
            writer.Write(BountySuccessKillCooldown);
            writer.Write(BountyFailureKillCooldown);
            writer.Write(BHDefaultKillCooldown);
            writer.Write(ShapeMasterShapeshiftDuration);
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
            var taskState = getPlayerById(Terrorist.PlayerId).getPlayerTaskState();
            if (taskState.isTaskFinished && (!main.ps.isSuicide(Terrorist.PlayerId) || canTerroristSuicideWin)) //タスクが完了で（自殺じゃない OR 自殺勝ちが許可）されていれば
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
                string SelfTaskText = hasTasks(seer.Data, false) ? $"<color=#ffff00>({main.getTaskText(seer)})</color>" : "";
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
                    if(seer.GetKillOrSpell() == false) SelfSuffix = "Mode:" + getString("WitchModeKill");
                    if(seer.GetKillOrSpell() == true) SelfSuffix = "Mode:" + getString("WitchModeSpell");
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
                if(seer.isMadSnitch()) {
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
                if(NameColorManager.Instance.GetDataBySeer(seer.PlayerId).Count > 0) 
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
                    string TargetTaskText = hasTasks(target.Data, false) && seer.Data.IsDead ? $"<color=#ffff00>({main.getTaskText(target)})</color>" : "";
                    
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
            SerialKillerTimer = new Dictionary<byte, float>();
            BountyTimer = new Dictionary<byte, float>();
            BountyTargets = new Dictionary<byte, PlayerControl>();
            CursedPlayers = new Dictionary<byte, PlayerControl>();
            CursedPlayerDie = new List<PlayerControl>();
            SpelledPlayer = new List<PlayerControl>();
            winnerList = new();
            VisibleTasksCount = false;
            MessagesToSend = new List<(string, byte)>();

            DisableSwipeCard = false;
            DisableSubmitScan = false;
            DisableUnlockSafe = false;
            DisableUploadData = false;
            DisableStartReactor = false;
            DisableResetBreaker = false;

            VampireKillDelay = 10;
            SerialKillerCooldown = 20;
            SerialKillerLimit = 60;
            BountyTargetChangeTime = 150;
            BountySuccessKillCooldown = 2;
            BountyFailureKillCooldown = 50;
            BHDefaultKillCooldown = 30;
            ShapeMasterShapeshiftDuration = 10;

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
            MadmateVisionAsImpostor = true;
            CanMakeMadmateCount = 0;
            MadGuardianCanSeeWhoTriedToKill = false;
            MadSnitchTasks = 4;

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
                {CustomRoles.Crewmate, "#ffffff"},
                {CustomRoles.Engineer, "#00ffff"},
                {CustomRoles.Scientist, "#00ffff"},
                {CustomRoles.GuardianAngel, "#ffffff"},
                {CustomRoles.Impostor, "#ff0000"},
                {CustomRoles.Shapeshifter, "#ff0000"},
                {CustomRoles.Vampire, "#ff0000"},
                {CustomRoles.Mafia, "#ff0000"},
                {CustomRoles.Madmate, "#ff0000"},
                {CustomRoles.SKMadmate, "#ff0000"},
                {CustomRoles.MadGuardian, "#ff0000"},
                {CustomRoles.MadSnitch, "#ff0000"},
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
                {CustomRoles.ShapeMaster, "#ff0000"},
                {CustomRoles.Warlock, "#ff0000"},
                {CustomRoles.SerialKiller, "#ff0000"},
                {CustomRoles.Fox, "#e478ff"},
                {CustomRoles.Troll, "#00ff00"}
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
    public enum CustomRoles {
        Crewmate = 0,
        Engineer,
        Scientist,
        Impostor,
        Shapeshifter,
        GuardianAngel,
        Jester,
        Madmate,
        SKMadmate,
        Bait,
        Terrorist,
        Mafia,
        Vampire,
        SabotageMaster,
        MadGuardian,
        MadSnitch,
        Mayor,
        Opportunist,
        Snitch,
        Sheriff,
        BountyHunter,
        Witch,
        ShapeMaster,
        Warlock,
        SerialKiller,
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
