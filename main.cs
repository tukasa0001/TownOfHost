using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using System;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnhollowerBaseLib;
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
        public const string BetaVersion = "3";
        public const string BetaName = "Mad Guardian Beta";
        public static string VersionSuffix => PluginVersionType == VersionTypes.Beta ? "b #" + BetaVersion : "";
        public Harmony Harmony { get; } = new Harmony(PluginGuid);
        public static BepInEx.Logging.ManualLogSource Logger;
        //Client Options
        public static ConfigEntry<bool> HideCodes {get; private set;}
        public static ConfigEntry<bool> JapaneseRoleName {get; private set;}
        public static ConfigEntry<bool> AmDebugger {get; private set;}

        public static LanguageUnit EnglishLang {get; private set;}
        //Lang-arrangement
        private static Dictionary<lang, string> JapaneseTexts = new Dictionary<lang, string>();
        private static Dictionary<CustomRoles, string> JapaneseRoleNames = new Dictionary<CustomRoles, string>();
        private static Dictionary<lang, string> EnglishTexts = new Dictionary<lang, string>();
        private static Dictionary<CustomRoles, string> EnglishRoleNames = new Dictionary<CustomRoles, string>();
        //Other Configs
        public static ConfigEntry<bool> IgnoreWinnerCommand { get; private set; }
        public static ConfigEntry<string> WebhookURL { get; private set; }
        public static CustomWinner currentWinner;
        public static bool IsHideAndSeek;
        public static bool AllowCloseDoors;
        public static bool IgnoreVent;
        public static bool IgnoreCosmetics;
        public static int HideAndSeekKillDelay;
        public static float HideAndSeekKillDelayTimer;
        public static float HideAndSeekImpVisionMin;

        public static Dictionary<byte, CustomRoles> AllPlayerCustomRoles;
        public static bool SyncButtonMode;
        public static int SyncedButtonCount;
        public static int UsedButtonCount;
        public static bool NoGameEnd;
        public static bool OptionControllerIsEnable;
        //タスク無効化
        public static bool DisableSwipeCard;
        public static bool DisableSubmitScan;
        public static bool DisableUnlockSafe;
        public static bool DisableUploadData;
        public static bool DisableStartReactor;
        public static Dictionary<CustomRoles,String> roleColors;
        //これ変えたらmod名とかの色が変わる
        public static string modColor = "#00bfff";
        public static bool isFixedCooldown => VampireCount > 0;
        public static float BeforeFixCooldown = 15f;
        public static float RefixCooldownDelay = 0f;
        public static int BeforeFixMeetingCooldown = 10;
        public static string winnerList;
        public static List<(string, byte)> MessagesToSend;

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
            var dic = TranslationController.Instance.CurrentLanguage.languageID == SupportedLangs.Japanese ? JapaneseTexts : EnglishTexts;
            var isSuccess = dic.TryGetValue(lang, out var text);
            return isSuccess ? text : "<Not Found:" + lang.ToString() + ">";
        }
        public static string getRoleName(CustomRoles role) {
            var dic = TranslationController.Instance.CurrentLanguage.languageID == SupportedLangs.Japanese &&
            JapaneseRoleName.Value == true ? JapaneseRoleNames : EnglishRoleNames;
            var isSuccess = dic.TryGetValue(role, out var text);
            return isSuccess ? text : "<Not Found:" + role.ToString() + ">";
        }
        public static Color getRoleColor(CustomRoles role)
        {
            string hexColor;
            if(!roleColors.TryGetValue(role, out hexColor)) hexColor = "#ffffff";
            MatchCollection matches = Regex.Matches(hexColor, "[0-9a-fA-F]{2}");
            return new Color(Convert.ToInt32(matches[0].Value,16)/255f,Convert.ToInt32(matches[1].Value,16)/255f,Convert.ToInt32(matches[2].Value,16)/255f);
        }
        public static string getRoleColorCode(CustomRoles role)
        {
            string hexColor;
            if(!roleColors.TryGetValue(role, out hexColor))hexColor = "#ffffff";
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
            var hasTasks = true;
            if (p.Disconnected) hasTasks = false;
            if(p.Role.IsImpostor)
                hasTasks = false; //タスクはCustomRoleを元に判定する
            if (main.IsHideAndSeek)
            {
                if (p.IsDead) hasTasks = false;
                var hasRole = main.AllPlayerCustomRoles.TryGetValue(p.PlayerId, out var role);
                if (hasRole)
                {
                    if (role == CustomRoles.Fox ||
                    role == CustomRoles.Troll) hasTasks = false;
                }
            } else {
                var cRoleFound = AllPlayerCustomRoles.TryGetValue(p.PlayerId, out var cRole);
                if(cRoleFound) {
                    if (cRole == CustomRoles.Jester) hasTasks = false;
                    if (cRole == CustomRoles.MadGuardian && ForRecompute) hasTasks = false;
                    if (cRole == CustomRoles.Opportunist) hasTasks = false;
                    if (cRole == CustomRoles.Sheriff) hasTasks = false;
                    if (cRole == CustomRoles.Madmate) hasTasks = false;
                    if (cRole == CustomRoles.Terrorist && ForRecompute) hasTasks = false;
                    if (cRole == CustomRoles.Impostor) hasTasks = false;
                    if (cRole == CustomRoles.Shapeshifter) hasTasks = false;
                }
            }
            return hasTasks;
        }
        public static string getTaskText(Il2CppSystem.Collections.Generic.List<GameData.TaskInfo> tasks)
        {
            if(tasks == null) return "null";
            string taskText = "";
            int CompletedTaskCount = 0;
            int AllTasksCount = 0;
            foreach (var task in tasks)
            {
                AllTasksCount++;
                if (task.Complete) CompletedTaskCount++;
            }
            taskText = CompletedTaskCount + "/" + AllTasksCount;
            return taskText;
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
                if(main.VampireCount > 0) main.SendToAll(main.getLang(lang.VampireInfoLong));
                if(main.MafiaCount > 0) main.SendToAll(main.getLang(lang.MafiaInfoLong));
                if(main.MadmateCount > 0) main.SendToAll(main.getLang(lang.MadmateInfoLong));
                if(main.TerroristCount > 0) main.SendToAll(main.getLang(lang.TerroristInfoLong));
                if(main.BaitCount > 0) main.SendToAll(main.getLang(lang.BaitInfoLong));
                if(main.JesterCount > 0) main.SendToAll(main.getLang(lang.JesterInfoLong));
                if(main.SabotageMasterCount > 0) main.SendToAll(main.getLang(lang.SabotageMasterInfoLong));
                if(main.MayorCount > 0) main.SendToAll(main.getLang(lang.MayorInfoLong));
                if(main.MadGuardianCount > 0) main.SendToAll(main.getLang(lang.MadGuardianInfoLong));
                if(main.OpportunistCount > 0) main.SendToAll(main.getLang(lang.OpportunistInfoLong));
                if(main.SheriffCount > 0) main.SendToAll(main.getLang(lang.SheriffInfoLong));
                if(main.SnitchCount > 0) main.SendToAll(main.getLang(lang.SnitchInfoLong));
            }
            if(main.NoGameEnd){ main.SendToAll(main.getLang(lang.NoGameEndInfo)); }
        }

        public static void ShowActiveSettings()
        {
            var text = "役職:";
            if(main.IsHideAndSeek)
            {
                if(main.FoxCount > 0 ) text += String.Format("\n{0,-5}:{1}",main.getRoleName(CustomRoles.Fox),main.FoxCount);
                if(main.TrollCount > 0 ) text += String.Format("\n{0,-5}:{1}",main.getRoleName(CustomRoles.Troll),main.TrollCount);
                main.SendToAll(text);
                text = "設定:";
                text += main.getLang(lang.HideAndSeek);
            }else{
                if(main.VampireCount > 0) text += String.Format("\n{0,-14}:{1}",main.getRoleName(CustomRoles.Vampire),main.VampireCount);
                if(main.MafiaCount > 0) text += String.Format("\n{0,-14}:{1}",main.getRoleName(CustomRoles.Mafia),main.MafiaCount);
                if(main.MadmateCount > 0) text += String.Format("\n{0,-14}:{1}",main.getRoleName(CustomRoles.Madmate),main.MadmateCount);
                if(main.TerroristCount > 0) text += String.Format("\n{0,-14}:{1}",main.getRoleName(CustomRoles.Terrorist),main.TerroristCount);
                if(main.BaitCount > 0) text += String.Format("\n{0,-14}:{1}",main.getRoleName(CustomRoles.Bait),main.BaitCount);
                if(main.JesterCount > 0) text += String.Format("\n{0,-14}:{1}",main.getRoleName(CustomRoles.Jester),main.JesterCount);
                if(main.SabotageMasterCount > 0) text += String.Format("\n{0,-14}:{1}",main.getRoleName(CustomRoles.SabotageMaster),main.SabotageMasterCount);
                if(main.MayorCount > 0) text += String.Format("\n{0,-14}:{1}",main.getRoleName(CustomRoles.Mayor),main.MayorCount);
                if(main.MadGuardianCount > 0)text += String.Format("\n{0,-14}:{1}",main.getRoleName(CustomRoles.MadGuardian),main.MadGuardianCount);
                if(main.OpportunistCount > 0) text += String.Format("\n{0,-14}:{1}",main.getRoleName(CustomRoles.Opportunist),main.OpportunistCount);
                if(main.SnitchCount > 0) text += String.Format("\n{0,-14}:{1}",main.getRoleName(CustomRoles.Snitch),main.SnitchCount);
                main.SendToAll(text);
                text = "設定:";
                if(main.VampireCount > 0) text += String.Format("\n{0}:{1}",main.getLang(lang.VampireKillDelay),main.VampireKillDelay);
                if(main.SabotageMasterCount > 0) {
                    if(main.SabotageMasterSkillLimit > 0) text += String.Format("\n{0}:{1}",main.getLang(lang.SabotageMasterSkillLimit),main.SabotageMasterSkillLimit);
                    if(main.SabotageMasterFixesDoors) text += String.Format("\n{0}:{1}",main.getLang(lang.SabotageMasterFixesDoors),getOnOff(main.SabotageMasterFixesDoors));
                    if(main.SabotageMasterFixesReactors) text += String.Format("\n{0}:{1}",main.getLang(lang.SabotageMasterFixesReactors),getOnOff(main.SabotageMasterFixesReactors));
                    if(main.SabotageMasterFixesOxygens) text += String.Format("\n{0}:{1}",main.getLang(lang.SabotageMasterFixesOxygens),getOnOff(main.SabotageMasterFixesOxygens));
                    if(main.SabotageMasterFixesCommunications) text += String.Format("\n{0}:{1}",main.getLang(lang.SabotageMasterFixesCommunications),getOnOff(main.SabotageMasterFixesCommunications));
                    if(main.SabotageMasterFixesElectrical) text += String.Format("\n{0}:{1}",main.getLang(lang.SabotageMasterFixesElectrical),getOnOff(main.SabotageMasterFixesElectrical));
                }
                if(main.MadGuardianCount > 0 || main.MadmateCount > 0){
                    if(main.MadmateCanFixLightsOut) text += String.Format("\n{0}:{1}",main.getLang(lang.MadmateCanFixLightsOut),getOnOff(main.MadmateCanFixLightsOut));
                }
                if(main.MadGuardianCount > 0) {
                    if(main.MadGuardianCanSeeBarrier) text += String.Format("\n{0}:{1}",main.getLang(lang.MadGuardianCanSeeBarrier),getOnOff(main.MadGuardianCanSeeBarrier));
                }
                if(main.MayorCount > 0) text += String.Format("\n{0}:{1}",main.getLang(lang.MayorAdditionalVote),main.MayorAdditionalVote);
                if(main.SyncButtonMode) text += String.Format("\n{0}:{1}",main.getLang(lang.SyncedButtonCount),main.SyncedButtonCount);
            }
            if(main.NoGameEnd)text += String.Format("\n{0,-14}",lang.NoGameEnd);
            main.SendToAll(text);
        }

        public static void ShowHelp()
        {
            main.SendToAll(
                "コマンド一覧:"
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
        public static int FoxCount;
        public static int TrollCount;

        public static Dictionary<byte, (byte, float)> BitPlayers = new Dictionary<byte, (byte, float)>();
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
        public static int MayorAdditionalVote;
        public static int SnitchExposeTaskLeft;

        public static bool MadmateCanFixLightsOut;
        public static bool MadGuardianCanSeeBarrier;
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
            writer.Write(SyncButtonMode);
            writer.Write(SyncedButtonCount);
            writer.Write(AllowCloseDoors);
            writer.Write(HideAndSeekKillDelay);
            writer.Write(IgnoreVent);
            writer.Write(MadmateCanFixLightsOut);
            writer.Write(MadGuardianCanSeeBarrier);
            writer.Write(MayorAdditionalVote);
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
            if (isAllCompleted)
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
        public static void RpcSetRole(PlayerControl targetPlayer, PlayerControl sendto, RoleTypes role)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(targetPlayer.NetId, (byte)RpcCalls.SetRole, Hazel.SendOption.Reliable, sendto.getClientId());
            writer.Write((byte)role);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void SendToAll(string text) => SendMessage(text);
        public static void SendMessage(string text, byte sendTo = byte.MaxValue)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            string[] textList = text.Split('\n');
            string tmp = "";
            foreach(string t in textList)
            {
                if(tmp.Length+t.Length < 120 ){
                    tmp += t+"\n";
                }else{
                    MessagesToSend.Add((tmp, sendTo));
                    tmp = t+"\n";
                }
            }
            if(tmp.Length != 0) MessagesToSend.Add((tmp, sendTo));
        }
        public static void ApplySuffix() {
            if(!AmongUsClient.Instance.AmHost) return;
            string name = SaveManager.PlayerName;
            if(nickName != "") name = nickName;
            if(!AmongUsClient.Instance.IsGameStarted) {
                switch(currentSuffix) {
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
            var player = PlayerControl.AllPlayerControls.ToArray().Where(pc => pc.PlayerId == PlayerId).FirstOrDefault();
            return player;
        }
        public static void NotifyRoles() {
            if(!AmongUsClient.Instance.AmHost) return;
            if(PlayerControl.AllPlayerControls == null) return;
            foreach(var pc in PlayerControl.AllPlayerControls) {
                var found = AllPlayerCustomRoles.TryGetValue(pc.PlayerId, out var role);
                string RoleName = "STRMISS";
                if(found) RoleName = getRoleName(role);
                pc.RpcSetNamePrivate($"<size=1.5><color={getRoleColorCode(role)}>{RoleName}</color></size>\r\n{pc.name}", true);
            }
        }

        public override void Load()
        {
            TextCursorTimer = 0f;
            TextCursorVisible = true;

            //Client Options
            HideCodes = Config.Bind("Client Options", "Hide Game Codes", false);
            JapaneseRoleName = Config.Bind("Client Options", "Japanese Role Name", false);

            Logger = BepInEx.Logging.Logger.CreateLogSource("TownOfHost");

            currentWinner = CustomWinner.Default;

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


            NoGameEnd = false;
            CustomWinTrigger = false;
            OptionControllerIsEnable = false;
            BitPlayers = new Dictionary<byte, (byte, float)>();
            winnerList = "";
            VisibleTasksCount = false;
            MessagesToSend = new List<(string, byte)>();



            AllPlayerCustomRoles = new Dictionary<byte, CustomRoles>();

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

            MadmateCanFixLightsOut = false;
            MadGuardianCanSeeBarrier = false;

            MayorAdditionalVote = 1;

            SnitchExposeTaskLeft = 1;

            currentSuffix = SuffixModes.None;

            IgnoreWinnerCommand = Config.Bind("Other", "IgnoreWinnerCommand", true);
            WebhookURL = Config.Bind("Other", "WebhookURL", "none");
            AmDebugger = Config.Bind("Other", "AmDebugger", false);

            roleColors = new Dictionary<CustomRoles, string>(){
                {CustomRoles.Default, "#ffffff"},
                {CustomRoles.Engineer, "#00ffff"},
                {CustomRoles.Scientist, "#00ffff"},
                {CustomRoles.GuardianAngel, "#ffffff"},
                {CustomRoles.Impostor, "#ff0000"},
                {CustomRoles.Shapeshifter, "#ff0000"},
                {CustomRoles.Vampire, "#a757a8"},
                {CustomRoles.Mafia, "#ff0000"},
                {CustomRoles.Madmate, "#ff0000"},
                {CustomRoles.MadGuardian, "#ff0000"},
                {CustomRoles.Jester, "#ec62a5"},
                {CustomRoles.Terrorist, "#00ff00"},
                {CustomRoles.Opportunist, "#00ff00"},
                {CustomRoles.Bait, "#00bfff"},
                {CustomRoles.SabotageMaster, "#0000ff"},
                {CustomRoles.Snitch, "#b8fb4f"},
                {CustomRoles.Mayor, "#204d42"},
                {CustomRoles.Mayor, "#ffff00"},
                {CustomRoles.Fox, "#e478ff"},
                {CustomRoles.Troll, "#00ff00"}
            };

            JapaneseTexts = new Dictionary<lang, string>(){
                //役職解説(短)
                {lang.JesterInfo, "追放されて勝利しろ"},
                {lang.MadmateInfo, "インポスターを助けろ"},
                {lang.MadGuardianInfo, "タスクを完了させ、インポスターを助けろ"},
                {lang.BaitInfo, "クルーのおとりになれ"},
                {lang.TerroristInfo, "タスクを完了させ、自爆しろ"},
                {lang.MafiaInfo, "インポスターの後継者となれ"},
                {lang.BeforeMafiaInfo,"今はキルをすることができない"},
                {lang.AfterMafiaInfo,"クルーメイトに復讐をしろ"},
                {lang.VampireInfo, "全員を噛んで倒せ"},
                {lang.SabotageMasterInfo, "より早くサボタージュを直せ"},
                {lang.MayorInfo, "人外を追放しろ"},
                {lang.OpportunistInfo, "とにかく生き残れ"},
                {lang.SnitchInfo, "タスクを完了させ、人外を暴け"},
                {lang.SheriffInfo, "インポスターを撃ち抜け"},
                //役職解説(長)
                {lang.JesterInfoLong, "ジェスター:\n会議で追放されたときに単独勝利となる第三陣営の役職。追放されずにゲームが終了するか、キルされると敗北となる。"},
                {lang.MadmateInfoLong, "狂人:\nインポスター陣営に属するが、インポスターが誰なのかはわからない。インポスターからも狂人が誰なのかはわからない。キルやサボタージュは使えないが、通気口を使うことができる。"},
                {lang.MadGuardianInfoLong, "守護狂人:\nインポスター陣営に属するが、誰が仲間かはわからない。ImpostorからもMadGuardianが誰なのかはわからないが、タスクを完了させるとキルされなくなる。キルやサボタージュ、通気口は使えない。(設定有)"},
                {lang.BaitInfoLong, "ベイト:\nキルされたときに、自分をキルした人に強制的に自分の死体を通報させることができる。"},
                {lang.TerroristInfoLong, "テロリスト:\n自身のタスクを全て完了させた状態で死亡したときに単独勝利となる第三陣営の役職。死因はキルと追放のどちらでもよい。タスクを完了させずに死亡したり、死亡しないまま試合が終了すると敗北する。"},
                {lang.MafiaInfoLong, "マフィア:\n初期状態でベントやサボタージュ、変身は可能だが、キルはできない。マフィアではないインポスターが全員死亡すると、マフィアもキルが可能となる。"},
                {lang.VampireInfoLong, "吸血鬼:\nキルボタンを押してから一定秒数経って実際にキルが発生する役職。キルをしたときのテレポートは発生せず、キルボタンを押してから設定された秒数が経つまでに会議が始まるとその瞬間にキルが発生する。(設定有)"},
                {lang.SabotageMasterInfoLong, "サボタージュマスター:\n原子炉メルトダウンや酸素妨害、MIRA HQの通信妨害は片方を修理すれば両方が直る。停電は一箇所のレバーに触れると全て直る。ドアを開けるとその部屋の全てのドアが開く。(設定有)"},
                {lang.MayorInfoLong, "メイヤー:\n票を複数持っており、まとめて一人またはスキップに入れることができる。(設定有)"},
                {lang.OpportunistInfoLong, "オポチュニスト:\nゲーム終了時に生き残っていれば追加勝利となる第三陣営の役職。タスクはない。"},
                {lang.SnitchInfoLong, "スニッチ:\nタスクを完了させると人外の名前が赤色に変化する。スニッチのタスクが少なくなると人外からスニッチの名前が変わって見える。"},
                {lang.SheriffInfoLong, "シェリフ:\n人外をキルすることができるが、クルーメイトをキルしようとすると自爆してしまう役職。タスクはない。"},
                {lang.FoxInfoLong, "狐(HideAndSeek):\nトロールを除くいずれかの陣営が勝利したときに生き残っていれば、勝利した陣営に追加で勝利することができる。"},
                {lang.TrollInfoLong, "トロール(HideAndSeek):\nインポスターにキルされたときに単独勝利となる。この場合、狐が生き残っていても狐は敗北となる。"},
                //モード名
                {lang.HideAndSeek, "HideAndSeek"},
                {lang.NoGameEnd, "NoGameEnd"},
                {lang.SyncButtonMode, "ボタン回数同期モード"},
                //モード解説
                {lang.HideAndSeekInfo, "HideAndSeek:会議を開くことはできず、クルーはタスク完了、インポスターは全クルー殺害でのみ勝利することができる。サボタージュ、アドミン、カメラ、待ち伏せなどは禁止事項である。(設定有)"},
                {lang.NoGameEndInfo, "NoGameEnd:勝利判定が存在しないデバッグ用のモード。ホストのSHIFT+L以外でのゲーム終了ができない。"},
                {lang.SyncButtonModeInfo, "ボタン回数同期モード:プレイヤー全員のボタン回数が同期されているモード。(設定有)"},
                //オプション項目
                {lang.AdvancedRoleOptions, "詳細設定"},
                {lang.VampireKillDelay, "吸血鬼の殺害までの時間(秒)"},
                {lang.MadmateCanFixLightsOut, "狂人が停電を直すことができる"},
                {lang.MadGuardianCanSeeBarrier, "守護狂人が自身の割れたバリアを見ることができる"},
                {lang.SabotageMasterSkillLimit, "ｻﾎﾞﾀｰｼﾞｭﾏｽﾀｰがｻﾎﾞﾀｰｼﾞｭに対して能力を使用できる回数(ﾄﾞｱ閉鎖は除く)"},
                {lang.SabotageMasterFixesDoors, "ｻﾎﾞﾀｰｼﾞｭﾏｽﾀｰが1度に複数のﾄﾞｱを開けることを許可する"},
                {lang.SabotageMasterFixesReactors, "ｻﾎﾞﾀｰｼﾞｭﾏｽﾀｰが原子炉ﾒﾙﾄﾀﾞｳﾝに対して能力を"},
                {lang.SabotageMasterFixesOxygens, "ｻﾎﾞﾀｰｼﾞｭﾏｽﾀｰが酸素妨害に対して能力を使える"},
                {lang.SabotageMasterFixesCommunications, "ｻﾎﾞﾀｰｼﾞｭﾏｽﾀｰがMIRA HQの通信妨害に対して能力を使える"},
                {lang.SabotageMasterFixesElectrical, "ｻﾎﾞﾀｰｼﾞｭﾏｽﾀｰが停電に対して能力を使える"},
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
                {lang.SuffixMode, "名前の二行目"},
                //その他
                {lang.SheriffDeadMessage, "あなたは死亡しました\n以降、キル・通報などはできません。\n他のプレイヤーからはあなたは死んでいるものとして見えています。"},
                {lang.commandError, "エラー:%1$"},
                {lang.InvalidArgs, "無効な引数"},
                {lang.ON, "ON"},
                {lang.OFF, "OFF"},
            };
            EnglishTexts = new Dictionary<lang, string>(){
                //役職解説(短)
                {lang.JesterInfo, "Get voted out to win"},
                {lang.MadmateInfo, "Help the Impostors"},
                {lang.MadGuardianInfo, "Finish your tasks and help the Impostors"},
                {lang.BaitInfo, "Be a decoy for the Crewmates"},
                {lang.TerroristInfo, "Die after finishing your tasks"},
                {lang.MafiaInfo, "Be the successor for the Impostors"},
                {lang.BeforeMafiaInfo,"You can not kill now"},
                {lang.AfterMafiaInfo,"Revenge to the Crewmates"},
                {lang.VampireInfo, "Kill everyone with your bites"},
                {lang.SabotageMasterInfo, "Fix sabotages faster"},
                {lang.MayorInfo, "Ban the extraperson"},
                {lang.OpportunistInfo, "Do whatever it takes to survive"},
                {lang.SnitchInfo, "Finish your tasks and uncover evildoer"},
                {lang.SheriffInfo, "Shoot the Impostors"},
                //役職解説(長)
                {lang.JesterInfoLong, "Jester:\n会議で追放されたときに単独勝利となる第三陣営の役職。追放されずにゲームが終了するか、キルされると敗北となる。"},
                {lang.MadmateInfoLong, "Madmate:\nインポスター陣営に属するが、Impostorが誰なのかはわからない。ImpostorからもMadmateが誰なのかはわからない。キルやサボタージュは使えないが、通気口を使うことができる。"},
                {lang.MadGuardianInfoLong, "MadGuardian:\nインポスター陣営に属するが、誰が仲間かはわからない。ImpostorからもMadGuardianが誰なのかはわからないが、タスクを完了させるとキルされなくなる。キルやサボタージュ、通気口は使えない。(設定有)"},
                {lang.BaitInfoLong, "Bait:\nキルされたときに、自分をキルした人に強制的に自分の死体を通報させることができる。"},
                {lang.TerroristInfoLong, "Terrorist:\n自身のタスクを全て完了させた状態で死亡したときに単独勝利となる第三陣営の役職。死因はキルと追放のどちらでもよい。タスクを完了させずに死亡したり、死亡しないまま試合が終了すると敗北する。"},
                {lang.MafiaInfoLong, "Mafia:\n初期状態でベントやサボタージュ、変身は可能だが、キルはできない。MafiaではないImpostorが全員死亡すると、Mafiaもキルが可能となる。"},
                {lang.VampireInfoLong, "Vampire:\nキルボタンを押してから一定秒数経って実際にキルが発生する役職。キルをしたときのテレポートは発生せず、キルボタンを押してから設定された秒数が経つまでに会議が始まるとその瞬間にキルが発生する。(設定有)"},
                {lang.SabotageMasterInfoLong, "SabotageMaster:\n原子炉メルトダウンや酸素妨害、MIRA HQの通信妨害は片方を修理すれば両方が直る。停電は一箇所のレバーに触れると全て直る。ドアを開けるとその部屋の全てのドアが開く。(設定有)"},
                {lang.MayorInfoLong, "Mayor:\n票を複数持っており、まとめて一人またはスキップに入れることができる。(設定有)"},
                {lang.OpportunistInfoLong, "Opportunist:\nゲーム終了時に生き残っていれば追加勝利となる第三陣営の役職。タスクはない。"},
                {lang.SnitchInfoLong, "Snitch:\nタスクを完了させると人外の名前が赤色に変化する。Snitchのタスクが少なくなると人外からSnitchの名前が変わって見える。"},
                {lang.SheriffInfoLong, "Sheriff:\n人外をキルすることができるが、クルーをキルしようとすると自爆してしまう役職。タスクはない。"},
                {lang.FoxInfoLong, "Fox(HideAndSeek):\nTrollを除くいずれかの陣営が勝利したときに生き残っていれば、勝利した陣営に追加で勝利することができる。"},
                {lang.TrollInfoLong, "Troll(HideAndSeek):\nImpostorにキルされたときに単独勝利となる。この場合、Foxが生き残っていてもFoxは敗北となる。"},
                //モード名
                {lang.HideAndSeek, "HideAndSeek"},
                {lang.NoGameEnd, "NoGameEnd"},
                {lang.SyncButtonMode, "SyncButtonMode"},
                //モード解説
                {lang.HideAndSeekInfo, "HideAndSeek:会議を開くことはできず、Crewmateはタスク完了、Impostorは全クルー殺害でのみ勝利することができる。サボタージュ、アドミン、カメラ、待ち伏せなどは禁止事項である。(設定有)"},
                {lang.NoGameEndInfo, "NoGameEnd:勝利判定が存在しないデバッグ用のモード。ホストのSHIFT+L以外でのゲーム終了ができない。"},
                {lang.SyncButtonModeInfo, "SyncButtonMode:プレイヤー全員のボタン回数が同期されているモード。(設定有)"},
                //オプション項目
                {lang.AdvancedRoleOptions, "Advanced Options"},
                {lang.VampireKillDelay, "Vampire Kill Delay(s)"},
                {lang.SabotageMasterSkillLimit, "SabotageMaster Fixes Sabotage Limit(Ignore Closing Doors)"},
                {lang.MadmateCanFixLightsOut, "Madmate Can Fix Lights Out"},
                {lang.MadGuardianCanSeeBarrier, "MadGuardian Can See Own Cracked Barrier"},
                {lang.SabotageMasterFixesDoors, "SabotageMaster Can Fixes Multiple Doors"},
                {lang.SabotageMasterFixesReactors, "SabotageMaster Can Fixes Both Reactors"},
                {lang.SabotageMasterFixesOxygens, "SabotageMaster Can Fixes Both O2"},
                {lang.SabotageMasterFixesCommunications, "SabotageMaster Can Fixes Both Communications In MIRA HQ"},
                {lang.SabotageMasterFixesElectrical, "SabotageMaster Can Fixes Lights Out All At Once"},
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
                {lang.SuffixMode, "Suffix"},
                //その他
                {lang.SheriffDeadMessage, "You Are Dead\n以降、キル・通報などはできません。\n他のプレイヤーからはあなたは死んでいるものとして見えています。"},
                {lang.commandError, "Error:%1$"},
                {lang.InvalidArgs, "Invalid Args"},
                {lang.ON, "ON"},
                {lang.OFF, "OFF"},
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
                {CustomRoles.Madmate, "狂人"},
                {CustomRoles.MadGuardian, "守護狂人"},
                {CustomRoles.Bait, "ベイト"},
                {CustomRoles.Terrorist, "テロリスト"},
                {CustomRoles.Mafia, "マフィア"},
                {CustomRoles.Vampire, "吸血鬼"},
                {CustomRoles.SabotageMaster, "サボタージュマスター"},
                {CustomRoles.Mayor, "メイヤー"},
                {CustomRoles.Opportunist, "オポチュニスト"},
                {CustomRoles.Snitch, "スニッチ"},
                {CustomRoles.Sheriff, "シェリフ"},
                {CustomRoles.Fox, "狐"},
                {CustomRoles.Troll, "トロール"},
            };

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
        FoxInfoLong,
        TrollInfoLong,
        //モード名
        HideAndSeek,
        SyncButtonMode,
        NoGameEnd,
        DisableTasks,
        //モード解説
        HideAndSeekInfo,
        SyncButtonModeInfo,
        NoGameEndInfo,
        //オプション項目
        AdvancedRoleOptions,
        VampireKillDelay,
        MadmateCanFixLightsOut,
        MadGuardianCanSeeBarrier,
        SabotageMasterFixesDoors,
        SabotageMasterSkillLimit,
        SabotageMasterFixesReactors,
        SabotageMasterFixesOxygens,
        SabotageMasterFixesCommunications,
        SabotageMasterFixesElectrical,
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
        //その他
        SheriffDeadMessage,
        commandError,
        InvalidArgs,
        ON,
        OFF,
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
        Fox,
        Troll
    }
    //WinData
    public enum CustomWinner
    {
        Draw = 0,
        Default,
        Jester,
        Terrorist
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
}
