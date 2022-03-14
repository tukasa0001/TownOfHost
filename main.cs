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
        public static Dictionary<byte, string> AllPlayerNames;
        public static Dictionary<byte, CustomRoles> AllPlayerCustomRoles;
        public static Dictionary<string, CustomRoles> lastAllPlayerCustomRoles;
        public static Dictionary<byte, bool> BlockKilling;
        public static bool OptionControllerIsEnable;
        public static Dictionary<CustomRoles,String> roleColors;
        //これ変えたらmod名とかの色が変わる
        public static string modColor = "#00bfff";
        public static bool isFixedCooldown => CustomRoles.Vampire.isEnable();
        public static float RefixCooldownDelay = 0f;
        public static int BeforeFixMeetingCooldown = 10;
        public static List<byte> IgnoreReportPlayers;
        public static List<byte> winnerList;
        public static List<(string, byte)> MessagesToSend;

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
            var lang = (TranslationController.Instance.CurrentLanguage.languageID == SupportedLangs.Japanese || Options.forceJapanese) &&
            JapaneseRoleName.Value == true ? SupportedLangs.Japanese : SupportedLangs.English;
            return getString(Enum.GetName(typeof(CustomRoles),role),lang);
        }
        public static string getDeathReason(PlayerState.DeathReason status)
        {
            return getString("DeathReason." + Enum.GetName(typeof(PlayerState.DeathReason),status));
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
        public static int GetCountFromRole(CustomRoles role) => role.getCount();
        public static void SetCountFromRole(CustomRoles role, int count) => role.setCount(count);

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
            if (Options.IsHideAndSeek)
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
            main.SendToAll(getString("CurrentActiveSettingsHelp")+":");
            if(Options.IsHideAndSeek)
            {
                main.SendToAll(getString("HideAndSeekInfo"));
                if(CustomRoles.Fox.isEnable()){ main.SendToAll(getRoleName(CustomRoles.Fox)+getString("FoxInfoLong")); }
                if(CustomRoles.Troll.isEnable()){ main.SendToAll(getRoleName(CustomRoles.Troll)+getString("TrollInfoLong")); }
            }else{
                if(Options.SyncButtonMode){ main.SendToAll(getString("SyncButtonModeInfo")); }
                if(Options.RandomMapsMode) { main.SendToAll(getString("RandomMapsModeInfo")); }
                if(CustomRoles.Vampire.isEnable()) main.SendToAll(getRoleName(CustomRoles.Vampire)+getString("VampireInfoLong"));
                if(CustomRoles.BountyHunter.isEnable()) main.SendToAll(getRoleName(CustomRoles.BountyHunter)+getString("BountyHunterInfoLong"));
                if(CustomRoles.Witch.isEnable()) main.SendToAll(getRoleName(CustomRoles.Witch)+getString("WitchInfoLong"));
                if(CustomRoles.Warlock.isEnable()) main.SendToAll(getRoleName(CustomRoles.Warlock)+getString("WarlockInfoLong"));
                if(CustomRoles.SerialKiller.isEnable()) main.SendToAll(getRoleName(CustomRoles.SerialKiller)+getString("SerialKillerInfoLong"));
                if(CustomRoles.Mafia.isEnable()) main.SendToAll(getRoleName(CustomRoles.Mafia)+getString("MafiaInfoLong"));
                if(CustomRoles.ShapeMaster.isEnable()) main.SendToAll(getRoleName(CustomRoles.Shapeshifter)+getString("ShapeMasterInfoLong"));
                if(CustomRoles.Madmate.isEnable()) main.SendToAll(getRoleName(CustomRoles.Madmate)+getString("MadmateInfoLong"));
                if(CustomRoles.SKMadmate.isEnable()) main.SendToAll(getRoleName(CustomRoles.SKMadmate)+getString("SKMadmateInfoLong"));
                if(CustomRoles.MadGuardian.isEnable()) main.SendToAll(getRoleName(CustomRoles.MadGuardian)+getString("MadGuardianInfoLong"));
                if(CustomRoles.MadSnitch.isEnable()) main.SendToAll(getRoleName(CustomRoles.MadSnitch)+getString("MadSnitchInfoLong"));
                if(CustomRoles.Jester.isEnable()) main.SendToAll(getRoleName(CustomRoles.Jester)+getString("JesterInfoLong"));
                if(CustomRoles.Terrorist.isEnable()) main.SendToAll(getRoleName(CustomRoles.Terrorist)+getString("TerroristInfoLong"));
                if(CustomRoles.Opportunist.isEnable()) main.SendToAll(getRoleName(CustomRoles.Opportunist)+getString("OpportunistInfoLong"));
                if(CustomRoles.Bait.isEnable()) main.SendToAll(getRoleName(CustomRoles.Bait)+getString("BaitInfoLong"));
                if(CustomRoles.Mayor.isEnable()) main.SendToAll(getRoleName(CustomRoles.Mayor)+getString("MayorInfoLong"));
                if(CustomRoles.SabotageMaster.isEnable()) main.SendToAll(getRoleName(CustomRoles.SabotageMaster)+getString("SabotageMasterInfoLong"));
                if(CustomRoles.Sheriff.isEnable()) main.SendToAll(getRoleName(CustomRoles.Sheriff)+getString("SheriffInfoLong"));
                if(CustomRoles.Snitch.isEnable()) main.SendToAll(getRoleName(CustomRoles.Snitch)+getString("SnitchInfoLong"));
            }
            if(Options.NoGameEnd){ main.SendToAll(getString("NoGameEndInfo")); }
        }

        public static void ShowActiveSettings()
        {
            var text = getString("Roles")+":";
            if(Options.IsHideAndSeek)
            {
                if(CustomRoles.Fox.isEnable()) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.Fox),CustomRoles.Fox.getCount());
                if(CustomRoles.Troll.isEnable()) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.Troll),CustomRoles.Troll.getCount());
                main.SendToAll(text);
                text = getString("Settings")+":";
                text += getString("HideAndSeek");
            }else{
                if(CustomRoles.Vampire.isEnable()) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.Vampire),CustomRoles.Vampire.getCount());
                if(CustomRoles.BountyHunter.isEnable()) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.BountyHunter),CustomRoles.BountyHunter.getCount());
                if(CustomRoles.Witch.isEnable()) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.Witch),CustomRoles.Witch.getCount());
                if(CustomRoles.Warlock.isEnable()) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.Warlock),CustomRoles.Warlock.getCount());
                if(CustomRoles.SerialKiller.isEnable()) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.SerialKiller),CustomRoles.SerialKiller.getCount());;
                if(CustomRoles.Mafia.isEnable()) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.Mafia),CustomRoles.Mafia.getCount());
                if(CustomRoles.ShapeMaster.isEnable()) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.ShapeMaster),CustomRoles.ShapeMaster.getCount());
                if(CustomRoles.Madmate.isEnable()) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.Madmate),CustomRoles.Madmate.getCount());
                if(CustomRoles.SKMadmate.isEnable()) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.SKMadmate),CustomRoles.SKMadmate.getCount());
                if(CustomRoles.MadGuardian.isEnable())text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.MadGuardian),CustomRoles.MadGuardian.getCount());
                if(CustomRoles.MadSnitch.isEnable())text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.MadSnitch),CustomRoles.MadSnitch.getCount());
                if(CustomRoles.Jester.isEnable()) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.Jester),CustomRoles.Jester.getCount());
                if(CustomRoles.Opportunist.isEnable()) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.Opportunist),CustomRoles.Opportunist.getCount());
                if(CustomRoles.Terrorist.isEnable()) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.Terrorist),CustomRoles.Terrorist.getCount());
                if(CustomRoles.Bait.isEnable()) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.Bait),CustomRoles.Bait.getCount());
                if(CustomRoles.Mayor.isEnable()) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.Mayor),CustomRoles.Mayor.getCount());
                if(CustomRoles.SabotageMaster.isEnable()) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.SabotageMaster),CustomRoles.SabotageMaster.getCount());
                if(CustomRoles.Sheriff.isEnable()) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.Sheriff),CustomRoles.Sheriff.getCount());
                if(CustomRoles.Snitch.isEnable()) text += String.Format("\n{0}:{1}",main.getRoleName(CustomRoles.Snitch),CustomRoles.Snitch.getCount());
                main.SendToAll(text);
                text = getString("Settings")+":";
                if(CustomRoles.Vampire.isEnable()) text += String.Format("\n{0}:{1}",getString("VampireKillDelay"),Options.VampireKillDelay);
                if(CustomRoles.SabotageMaster.isEnable())
                {
                    if(Options.SabotageMasterSkillLimit > 0) text += String.Format("\n{0}:{1}",getString("SabotageMasterSkillLimit"),Options.SabotageMasterSkillLimit);
                    if(Options.SabotageMasterFixesDoors) text += String.Format("\n{0}:{1}",getString("SabotageMasterFixesDoors"),getOnOff(Options.SabotageMasterFixesDoors));
                    if(Options.SabotageMasterFixesReactors) text += String.Format("\n{0}:{1}",getString("SabotageMasterFixesReactors"),getOnOff(Options.SabotageMasterFixesReactors));
                    if(Options.SabotageMasterFixesOxygens) text += String.Format("\n{0}:{1}",getString("SabotageMasterFixesOxygens"),getOnOff(Options.SabotageMasterFixesOxygens));
                    if(Options.SabotageMasterFixesCommunications) text += String.Format("\n{0}:{1}",getString("SabotageMasterFixesCommunications"),getOnOff(Options.SabotageMasterFixesCommunications));
                    if(Options.SabotageMasterFixesElectrical) text += String.Format("\n{0}:{1}",getString("SabotageMasterFixesElectrical"),getOnOff(Options.SabotageMasterFixesElectrical));
                }
                if (CustomRoles.Sheriff.isEnable())
                {
                    text += String.Format("\n{0}:{1}",getString("SheriffKillCooldown"),Options.SheriffKillCooldown);
                    if (Options.SheriffCanKillJester) text += String.Format("\n{0}:{1}", getString("SheriffCanKillJester"), getOnOff(Options.SheriffCanKillJester));
                    if (Options.SheriffCanKillTerrorist) text += String.Format("\n{0}:{1}", getString("SheriffCanKillTerrorist"), getOnOff(Options.SheriffCanKillTerrorist));
                    if (Options.SheriffCanKillOpportunist) text += String.Format("\n{0}:{1}", getString("SheriffCanKillOpportunist"), getOnOff(Options.SheriffCanKillOpportunist));
                    if (Options.SheriffCanKillMadmate) text += String.Format("\n{0}:{1}", getString("SheriffCanKillMadmate"), getOnOff(Options.SheriffCanKillMadmate));
                }
                if(CustomRoles.MadGuardian.isEnable() || CustomRoles.MadSnitch.isEnable() || CustomRoles.Madmate.isEnable() || CustomRoles.SKMadmate.isEnable())
                {
                    if(Options.MadmateVisionAsImpostor) text += String.Format("\n{0}:{1}",getString("MadmateVisionAsImpostor"),getOnOff(Options.MadmateVisionAsImpostor));
                    if(Options.MadmateCanFixLightsOut) text += String.Format("\n{0}:{1}",getString("MadmateCanFixLightsOut"),getOnOff(Options.MadmateCanFixLightsOut));
                    if(Options.MadmateCanFixComms) text += String.Format("\n{0}:{1}", getString("MadmateCanFixComms"), getOnOff(Options.MadmateCanFixComms));
                }
                if(CustomRoles.MadGuardian.isEnable())
                {
                    if(Options.MadGuardianCanSeeWhoTriedToKill) text += String.Format("\n{0}:{1}",getString("MadGuardianCanSeeWhoTriedToKill"),getOnOff(Options.MadGuardianCanSeeWhoTriedToKill));
                }
                if(CustomRoles.MadSnitch.isEnable())text += String.Format("\n{0}:{1}",getString("MadSnitchTasks"),Options.MadSnitchTasks);
                if(CustomRoles.Mayor.isEnable()) text += String.Format("\n{0}:{1}",getString("MayorAdditionalVote"),Options.MayorAdditionalVote);
                if(Options.SyncButtonMode) text += String.Format("\n{0}:{1}",getString("SyncedButtonCount"),Options.SyncedButtonCount);
                if(Options.whenSkipVote != VoteMode.Default) text += String.Format("\n{0}:{1}",getString("WhenSkipVote"),Options.whenSkipVote);
                if(Options.whenNonVote != VoteMode.Default) text += String.Format("\n{0}:{1}",getString("WhenNonVote"),Options.whenNonVote);
                if((Options.whenNonVote == VoteMode.Suicide || Options.whenSkipVote == VoteMode.Suicide) && CustomRoles.Terrorist.isEnable()) text += String.Format("\n{0}:{1}",getString("CanTerroristSuicideWin"),Options.canTerroristSuicideWin);
            }
            if(Options.NoGameEnd)text += String.Format("\n{0,-14}",getString("NoGameEnd"));
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
        public static Dictionary<byte, bool> CheckShapeshift = new Dictionary<byte, bool>();
        public static byte ExiledJesterID;
        public static byte WonTerroristID;
        public static bool CustomWinTrigger;
        public static bool VisibleTasksCount;
        public static string nickName = "";
        //SyncCustomSettingsRPC Sender
        public static void SyncCustomSettingsRPC()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, 80, Hazel.SendOption.Reliable, -1);
            foreach(CustomRoles r in Enum.GetValues(typeof(CustomRoles))) writer.Write(r.getCount());

            writer.Write(Options.IsHideAndSeek);
            writer.Write(Options.NoGameEnd);
            writer.Write(Options.DisableSwipeCard);
            writer.Write(Options.DisableSubmitScan);
            writer.Write(Options.DisableUnlockSafe);
            writer.Write(Options.DisableUploadData);
            writer.Write(Options.DisableStartReactor);
            writer.Write(Options.DisableResetBreaker);
            writer.Write(Options.VampireKillDelay);
            writer.Write(Options.SabotageMasterSkillLimit);
            writer.Write(Options.SabotageMasterFixesDoors);
            writer.Write(Options.SabotageMasterFixesReactors);
            writer.Write(Options.SabotageMasterFixesOxygens);
            writer.Write(Options.SabotageMasterFixesCommunications);
            writer.Write(Options.SabotageMasterFixesElectrical);
            writer.Write(Options.SheriffKillCooldown);
            writer.Write(Options.SheriffCanKillJester);
            writer.Write(Options.SheriffCanKillTerrorist);
            writer.Write(Options.SheriffCanKillOpportunist);
            writer.Write(Options.SheriffCanKillMadmate);
            writer.Write(Options.SyncButtonMode);
            writer.Write(Options.SyncedButtonCount);
            writer.Write((int)Options.whenSkipVote);
            writer.Write((int)Options.whenNonVote);
            writer.Write(Options.canTerroristSuicideWin);
            writer.Write(Options.AllowCloseDoors);
            writer.Write(Options.HideAndSeekKillDelay);
            writer.Write(Options.IgnoreVent);
            writer.Write(Options.IgnoreCosmetics);
            writer.Write(Options.MadmateCanFixLightsOut);
            writer.Write(Options.MadmateCanFixComms);
            writer.Write(Options.MadmateVisionAsImpostor);
            writer.Write(Options.CanMakeMadmateCount);
            writer.Write(Options.MadGuardianCanSeeWhoTriedToKill);
            writer.Write(Options.MadSnitchTasks);
            writer.Write(Options.MayorAdditionalVote);
            writer.Write(Options.SerialKillerCooldown);
            writer.Write(Options.SerialKillerLimit);
            writer.Write(Options.BountyTargetChangeTime);
            writer.Write(Options.BountySuccessKillCooldown);
            writer.Write(Options.BountyFailureKillCooldown);
            writer.Write(Options.BHDefaultKillCooldown);
            writer.Write(Options.ShapeMasterShapeshiftDuration);
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
            if (taskState.isTaskFinished && (!main.ps.isSuicide(Terrorist.PlayerId) || Options.canTerroristSuicideWin)) //タスクが完了で（自殺じゃない OR 自殺勝ちが許可）されていれば
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
            var tmp_text = text.Replace("#","＃").Replace("<","＜").Replace(">","＞");
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
                switch(Options.currentSuffix)
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
            if(CustomRoles.Snitch.isEnable()) foreach(var snitch in PlayerControl.AllPlayerControls) {
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

            AllPlayerCustomRoles = new Dictionary<byte, CustomRoles>();
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
