﻿using BepInEx;
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
//herro
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
        public static ConfigEntry<bool> TeruteruColor { get; private set; }
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
        public static Color JesterColor = new Color(0.925f, 0.384f, 0.647f);
        public static Color MayorColor = new Color(1f, 0f, 1f);
        public static Color VampireColor = new Color(0.65f, 0.34f, 0.65f);
        //これ変えたらmod名とかの色が変わる
        public static string modColor = "#00bfff";
        public static bool isFixedCooldown => RoleCounts[CustomRoles.Vampire] > 0;
        public static float BeforeFixCooldown = 15f;
        public static float RefixCooldownDelay = 0f;
        public static int BeforeFixMeetingCooldown = 10;
        public static string winnerList;
        public static List<string> MessagesToSend;
        public static bool isJester(PlayerControl target)
        {
            if (target.getCustomRole() == CustomRoles.Jester)
                return true;
            return false;
        }
        public static bool isMadmate(PlayerControl target)
        {
            if (target.getCustomRole() == CustomRoles.Madmate)
                return true;
            return false;
        }
        public static bool isBait(PlayerControl target)
        {
            if (target.getCustomRole() == CustomRoles.Bait)
                return true;
            return false;
        }
        public static bool isTerrorist(PlayerControl target)
        {
            if (target.getCustomRole() == CustomRoles.Terrorist)
                return true;
            return false;
        }
        public static bool isSidekick(PlayerControl target)
        {
            if (target.getCustomRole() == CustomRoles.Sidekick)
                return true;
            return false;
        }
        public static bool isVampire(PlayerControl target)
        {
            if (target.getCustomRole() == CustomRoles.Vampire)
                return true;
            return false;
        }
        public static bool isSabotageMaster(PlayerControl target)
        {
            if (target.getCustomRole() == CustomRoles.SabotageMaster)
                return true;
            return false;
        }
        public static bool isMadGuardian(PlayerControl target)
        {
            if (target.getCustomRole() == CustomRoles.MadGuardian)
                return true;
            return false;
        }
        public static bool isMayor(PlayerControl target)
        {
            if (target.getCustomRole() == CustomRoles.Mayor)
                return true;
            return false;
        }
        public static bool isOpportunist(PlayerControl target)
        {
            if (target.getCustomRole() == CustomRoles.Opportunist)
                return true;
            return false;
        }

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
        public static string getRoleName(RoleTypes role) {
            var currentLanguage = TranslationController.Instance.CurrentLanguage;
            if(JapaneseRoleName.Value == false && EnglishLang != null) 
                currentLanguage = EnglishLang;
            string text = currentLanguage.GetString(RoleTypeHelpers.RoleToName[role], "Invalid Role", new Il2CppSystem.Object[0]{});
            return text;
        }
        public static int GetCountFromRole(CustomRoles role) {
            int count;
            switch(role) {
                case CustomRoles.Jester:
                    count = RoleCounts[CustomRoles.Jester];
                    break;
                case CustomRoles.Madmate:
                    count = RoleCounts[CustomRoles.Madmate];
                    break;
                case CustomRoles.Bait:
                    count = RoleCounts[CustomRoles.Bait];
                    break;
                case CustomRoles.Terrorist:
                    count = RoleCounts[CustomRoles.Terrorist];
                    break;
                case CustomRoles.Sidekick:
                    count = RoleCounts[CustomRoles.Sidekick];
                    break;
                case CustomRoles.Vampire:
                    count = RoleCounts[CustomRoles.Vampire];
                    break;
                case CustomRoles.SabotageMaster:
                    count = RoleCounts[CustomRoles.SabotageMaster];
                    break;
                case CustomRoles.MadGuardian:
                    count = RoleCounts[CustomRoles.MadGuardian];
                    break;
                case CustomRoles.Mayor:
                    count = RoleCounts[CustomRoles.Mayor];
                    break;
                case CustomRoles.Opportunist:
                    count = RoleCounts[CustomRoles.Opportunist];
                    break;
                default:
                    return -1;
            }
            return count;
        }
        public static void SetCountFromRole(CustomRoles role, int count) {
            switch(role) {
                case CustomRoles.Jester:
                    RoleCounts[CustomRoles.Jester] = count;
                    break;
                case CustomRoles.Madmate:
                    RoleCounts[CustomRoles.Madmate] = count;
                    break;
                case CustomRoles.Bait:
                    RoleCounts[CustomRoles.Bait] = count;
                    break;
                case CustomRoles.Terrorist:
                    RoleCounts[CustomRoles.Terrorist] = count;
                    break;
                case CustomRoles.Sidekick:
                    RoleCounts[CustomRoles.Sidekick] = count;
                    break;
                case CustomRoles.Vampire:
                    RoleCounts[CustomRoles.Vampire] = count;
                    break;
                case CustomRoles.SabotageMaster:
                    RoleCounts[CustomRoles.SabotageMaster] = count;
                    break;
                case CustomRoles.MadGuardian:
                    RoleCounts[CustomRoles.MadGuardian] = count;
                    break;
                case CustomRoles.Mayor:
                    RoleCounts[CustomRoles.Mayor] = count;
                    break;
                case CustomRoles.Opportunist:
                    RoleCounts[CustomRoles.Opportunist] = count;
                    break;
            }
        }

        public static (string, Color) GetRoleText(PlayerControl player)
        {
            string RoleText = "Invalid Role";
            Color TextColor = Color.red;

            var cRole = player.getCustomRole();
            RoleText = getRoleName(cRole);
            switch (cRole) {
                case CustomRoles.Default:
                    RoleText = getRoleName(player.Data.Role.Role);
                    switch(player.Data.Role.Role) {
                        //通常クルー
                        case RoleTypes.Crewmate:
                            TextColor = Color.white;
                            break;
                        //クルー陣営役職
                        case RoleTypes.Scientist:
                        case RoleTypes.Engineer:
                        case RoleTypes.GuardianAngel:
                            TextColor = Palette.CrewmateBlue;
                            break;
                        //インポスター陣営役職
                        case RoleTypes.Impostor:
                        case RoleTypes.Shapeshifter:
                            TextColor = Palette.ImpostorRed;
                            break;
                    }
                    break;
                case CustomRoles.Jester:
                    TextColor = JesterColor;
                    break;
                case CustomRoles.Madmate:
                    TextColor = Palette.ImpostorRed;
                    break;
                case CustomRoles.MadGuardian:
                    TextColor = Palette.ImpostorRed;
                    break;
                case CustomRoles.Mayor:
                    TextColor = MayorColor;
                    break;
                case CustomRoles.Opportunist:
                    TextColor = Color.green;
                    break;
                case CustomRoles.SabotageMaster:
                    TextColor = Color.blue;
                    break;
                case CustomRoles.Terrorist:
                    TextColor = Color.green;
                    break;
                case CustomRoles.Bait:
                    TextColor = Color.cyan;
                    break;
                case CustomRoles.Vampire:
                    TextColor = VampireColor;
                    break;
                case CustomRoles.Sidekick:
                    TextColor = Palette.ImpostorRed;
                    break;
            }

            return (RoleText, TextColor);
        }
        public static (string, Color) GetRoleTextHideAndSeek(RoleTypes oRole, CustomRoles hRole)
        {
            string text = "Invalid";
            Color color = Color.red;
            switch (oRole)
            {
                case RoleTypes.Impostor:
                    text = "Impostor";
                    color = Palette.ImpostorRed;
                    break;
                case RoleTypes.Shapeshifter:
                    goto case RoleTypes.Impostor;
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
            if (p.Role.TeamType == RoleTeamTypes.Impostor) hasTasks = false;
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
                    if (cRole == CustomRoles.Madmate) hasTasks = false;
                    if (cRole == CustomRoles.Terrorist && ForRecompute) hasTasks = false;
                }
            }
            return hasTasks;
        }
        public static string getTaskText(Il2CppSystem.Collections.Generic.List<GameData.TaskInfo> tasks)
        {
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
            main.SendToAll("現在有効になっている設定の説明:");
            if(main.IsHideAndSeek)
            {
                main.SendToAll(main.getLang(lang.HideAndSeekInfo));
                if(main.RoleCounts[CustomRoles.Fox] > 0 ){ main.SendToAll(main.getLang(lang.FoxInfoLong)); }
                if(main.RoleCounts[CustomRoles.Troll] > 0 ){ main.SendToAll(main.getLang(lang.TrollInfoLong)); }
            }else{
                if(main.SyncButtonMode){ main.SendToAll(main.getLang(lang.SyncButtonModeInfo)); }
                if(main.RoleCounts[CustomRoles.Vampire] > 0) main.SendToAll(main.getLang(lang.VampireInfoLong));
                if(main.RoleCounts[CustomRoles.Sidekick] > 0) main.SendToAll(main.getLang(lang.SidekickInfoLong));
                if(main.RoleCounts[CustomRoles.Madmate] > 0) main.SendToAll(main.getLang(lang.MadmateInfoLong));
                if(main.RoleCounts[CustomRoles.Terrorist] > 0) main.SendToAll(main.getLang(lang.TerroristInfoLong));
                if(main.RoleCounts[CustomRoles.Bait] > 0) main.SendToAll(main.getLang(lang.BaitInfoLong));
                if(main.RoleCounts[CustomRoles.Jester] > 0) main.SendToAll(main.getLang(lang.JesterInfoLong));
                if(main.RoleCounts[CustomRoles.SabotageMaster] > 0) main.SendToAll(main.getLang(lang.SabotageMasterInfoLong));
                if(main.RoleCounts[CustomRoles.Mayor] > 0) main.SendToAll(main.getLang(lang.MayorInfoLong));
                if(main.RoleCounts[CustomRoles.MadGuardian] > 0) main.SendToAll(main.getLang(lang.MadGuardianInfoLong));
                if(main.RoleCounts[CustomRoles.Opportunist] > 0) main.SendToAll(main.getLang(lang.OpportunistInfoLong));
            }
            if(main.NoGameEnd){ main.SendToAll(main.getLang(lang.NoGameEndInfo)); }
        }
        public static Dictionary<byte, string> RealNames;
        public static string getOnOff(bool value) => value ? "ON" : "OFF";
        public static string TextCursor => TextCursorVisible ? "_" : "";
        public static bool TextCursorVisible;
        public static float TextCursorTimer;
        //Enabled Role
        public static Dictionary<CustomRoles, int> RoleCounts = new Dictionary<CustomRoles, int>();
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

        public static bool MadmateCanFixLightsOut;
        public static bool MadGuardianCanSeeBarrier;
        public static SuffixModes currentSuffix;
        //SyncCustomSettingsRPC Sender
        public static void SyncCustomSettingsRPC()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, 80, Hazel.SendOption.Reliable, -1);
            writer.Write(RoleCounts[CustomRoles.Jester]);
            writer.Write(RoleCounts[CustomRoles.Madmate]);
            writer.Write(RoleCounts[CustomRoles.Bait]);
            writer.Write(RoleCounts[CustomRoles.Terrorist]);
            writer.Write(RoleCounts[CustomRoles.Sidekick]);
            writer.Write(RoleCounts[CustomRoles.Vampire]);
            writer.Write(RoleCounts[CustomRoles.SabotageMaster]);
            writer.Write(RoleCounts[CustomRoles.MadGuardian]);
            writer.Write(RoleCounts[CustomRoles.Mayor]);
            writer.Write(RoleCounts[CustomRoles.Opportunist]);
            writer.Write(RoleCounts[CustomRoles.Fox]);
            writer.Write(RoleCounts[CustomRoles.Troll]);


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
            var isAllConpleted = true;
            foreach (var task in Terrorist.Tasks)
            {
                if (!task.Complete) isAllConpleted = false;
            }
            if (isAllConpleted)
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
        public static void SendToAll(string text)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            MessagesToSend.Add(text);
        }
        public static void ApplySuffix() {
            if(!AmongUsClient.Instance.AmHost) return;
            string name = SaveManager.PlayerName;
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
        public static CustomRoles getCustomRole(byte PlayerId) {
            var cRoleFound = main.AllPlayerCustomRoles.TryGetValue(PlayerId, out var cRole);
            if(cRoleFound) return cRole;
            else return CustomRoles.Default;
        }
        public static void NotifyRoles() {
            if(!AmongUsClient.Instance.AmHost) return;
            if(PlayerControl.AllPlayerControls == null) return;
            foreach(var pc in PlayerControl.AllPlayerControls) {
                var found = AllPlayerCustomRoles.TryGetValue(pc.PlayerId, out var role);
                string RoleName = "STRMISS";
                if(found) RoleName = getRoleName(role);
                pc.RpcSetNamePrivate("<size=1.5>" + RoleName + "</size>\r\n" + pc.name, true);
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
            RoleCounts[CustomRoles.Troll] = 0;
            RoleCounts[CustomRoles.Fox] = 0;
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
            MessagesToSend = new List<string>();



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

            currentSuffix = SuffixModes.None;

            TeruteruColor = Config.Bind("Other", "TeruteruColor", false);
            IgnoreWinnerCommand = Config.Bind("Other", "IgnoreWinnerCommand", true);
            WebhookURL = Config.Bind("Other", "WebhookURL", "none");
            AmDebugger = Config.Bind("Other", "AmDebugger", false);

            JapaneseTexts = new Dictionary<lang, string>(){
                //役職解説(短)
                {lang.JesterInfo, "追放されて勝利しろ"},
                {lang.MadmateInfo, "インポスターを助けろ"},
                {lang.MadGuardianInfo, "タスクを完了させ、インポスターを助けろ"},
                {lang.BaitInfo, "クルーのおとりになれ"},
                {lang.TerroristInfo, "タスクを完了させ、自爆しろ"},
                {lang.SidekickInfo, "インポスターの後継者となれ"},
                {lang.BeforeSidekickInfo,"今はキルをすることができない"},
                {lang.AfterSidekickInfo,"クルーメイトに復讐をしろ"},
                {lang.VampireInfo, "全員を噛んで倒せ"},
                {lang.SabotageMasterInfo, "より早くサボタージュを直せ"},
                {lang.MayorInfo, "インポスターを追放しろ"},
                {lang.OpportunistInfo, "とにかく生き残れ"},
                //役職解説(長)
                {lang.JesterInfoLong, "ジェスター:\n会議で追放されたときに単独勝利となる第三陣営の役職。追放されずにゲームが終了するか、キルされると敗北となる。"},
                {lang.MadmateInfoLong, "狂人:\nインポスター陣営に属するが、インポスターが誰なのかはわからない。インポスターからも狂人が誰なのかはわからない。キルやサボタージュは使えないが、通気口を使うことができる。"},
                {lang.MadGuardianInfoLong, "守護狂人:\nインポスター陣営に属するが、インポスターが誰なのかはわからない。インポスターからも守護狂人が誰なのかはわからないが、タスクを完了させるとキルされなくなる。キルやサボタージュ、通気口は使えない。(設定有)"},
                {lang.BaitInfoLong, "ベイト:\nキルされたときに、自分をキルした人に強制的に自分の死体を通報させることができる。"},
                {lang.TerroristInfoLong, "テロリスト:\n自身のタスクを全て完了させた状態で死亡したときに単独勝利となる第三陣営の役職。死因はキルと追放のどちらでもよい。タスクを完了させずに死亡したり、死亡しないまま試合が終了すると敗北する。"},
                {lang.SidekickInfoLong, "相棒:\n初期状態でベントやサボタージュ、変身は可能だが、キルはできない。相棒ではないインポスターが全員死亡すると、相棒もキルが可能となる。"},
                {lang.VampireInfoLong, "吸血鬼:\nキルボタンを押してから一定秒数経って実際にキルが発生する役職。キルをしたときのテレポートは発生せず、キルボタンを押してから設定された秒数が経つまでに会議が始まるとその瞬間にキルが発生する。(設定有)"},
                {lang.SabotageMasterInfoLong, "サボタージュマスター:\n原子炉メルトダウンや酸素妨害、MIRA HQの通信妨害は片方を修理すれば両方が直る。停電は一箇所のレバーに触れると全て直る。ドアを開けるとその部屋の全てのドアが開く。(設定有)"},
                {lang.MayorInfoLong, "メイヤー:\n票を複数持っており、まとめて一人またはスキップに入れることができる。(設定有)"},
                {lang.OpportunistInfoLong, "オポチュニスト:\nゲーム終了時に生き残っていれば追加勝利となる第三陣営の役職。タスクはない。"},
                {lang.FoxInfoLong, "狐(HideAndSeek):トロールを除くいずれかの陣営が勝利したときに生き残っていれば、勝利した陣営に追加で勝利することができる。"},
                {lang.TrollInfoLong, "トロール(HideAndSeek):インポスターにキルされたときに単独勝利となる。この場合、狐が生き残っていても狐は敗北となる。"},
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
                {lang.SabotageMasterSkillLimit, "サボタージュマスターがサボタージュに対して能力を使用できる回数(ドア閉鎖は除く)"},
                {lang.SabotageMasterFixesDoors, "サボタージュマスターが1度に複数のドアを開けることを許可する"},
                {lang.SabotageMasterFixesReactors, "サボタージュマスターが原子炉メルトダウンに対して能力を"},
                {lang.SabotageMasterFixesOxygens, "サボタージュマスターが酸素妨害に対して能力を使える"},
                {lang.SabotageMasterFixesCommunications, "サボタージュマスターがMIRA HQの通信妨害に対して能力を使える"},
                {lang.SabotageMasterFixesElectrical, "サボタージュマスターが停電に対して能力を使える"},
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
                {lang.SidekickInfo, "Be the successor for the Impostors"},
                {lang.BeforeSidekickInfo,"You can not kill now"},
                {lang.AfterSidekickInfo,"Revenge to the Crewmates"},
                {lang.VampireInfo, "Kill everyone with your bites"},
                {lang.SabotageMasterInfo, "Fix sabotages faster"},
                {lang.MayorInfo, "Ban the Impostors"},
                {lang.OpportunistInfo, "Do whatever it takes to survive"},
                //役職解説(長)
                {lang.JesterInfoLong, "Jester:\n会議で追放されたときに単独勝利となる第三陣営の役職。追放されずにゲームが終了するか、キルされると敗北となる。"},
                {lang.MadmateInfoLong, "Madmate:\nインポスター陣営に属するが、Impostorが誰なのかはわからない。ImpostorからもMadmateが誰なのかはわからない。キルやサボタージュは使えないが、通気口を使うことができる。"},
                {lang.MadGuardianInfoLong, "MadGuardian:\nインポスター陣営に属するが、Impostorが誰なのかはわからない。ImpostorからもMadGuardianが誰なのかはわからないが、タスクを完了させるとキルされなくなる。キルやサボタージュ、通気口は使えない。(設定有)"},
                {lang.BaitInfoLong, "Bait:\nキルされたときに、自分をキルした人に強制的に自分の死体を通報させることができる。"},
                {lang.TerroristInfoLong, "Terrorist:\n自身のタスクを全て完了させた状態で死亡したときに単独勝利となる第三陣営の役職。死因はキルと追放のどちらでもよい。タスクを完了させずに死亡したり、死亡しないまま試合が終了すると敗北する。"},
                {lang.SidekickInfoLong, "Sidekick:\n初期状態でベントやサボタージュ、変身は可能だが、キルはできない。SidekickではないImpostorが全員死亡すると、Sidekickもキルが可能となる。"},
                {lang.VampireInfoLong, "Vampire:\nキルボタンを押してから一定秒数経って実際にキルが発生する役職。キルをしたときのテレポートは発生せず、キルボタンを押してから設定された秒数が経つまでに会議が始まるとその瞬間にキルが発生する。(設定有)"},
                {lang.SabotageMasterInfoLong, "SabotageMaster:\n原子炉メルトダウンや酸素妨害、MIRA HQの通信妨害は片方を修理すれば両方が直る。停電は一箇所のレバーに触れると全て直る。ドアを開けるとその部屋の全てのドアが開く。(設定有)"},
                {lang.MayorInfoLong, "Mayor:\n票を複数持っており、まとめて一人またはスキップに入れることができる。(設定有)"},
                {lang.OpportunistInfoLong, "Opportunist:\nゲーム終了時に生き残っていれば追加勝利となる第三陣営の役職。タスクはない。"},
                {lang.FoxInfoLong, "Fox(HideAndSeek):Trollを除くいずれかの陣営が勝利したときに生き残っていれば、勝利した陣営に追加で勝利することができる。"},
                {lang.TrollInfoLong, "Troll(HideAndSeek):Impostorにキルされたときに単独勝利となる。この場合、Foxが生き残っていてもFoxは敗北となる。"},
                //モード名
                {lang.HideAndSeek, "HideAndSeek"},
                {lang.NoGameEnd, "NoGameEnd"},
                {lang.SyncButtonMode, "SyncButtonMode"},
                //モード解説
                {lang.HideAndSeekInfo, "HideAndSeek:会議を開くことはできず、Crewmateはタスク完了、Inpostorは全クルー殺害でのみ勝利することができる。サボタージュ、アドミン、カメラ、待ち伏せなどは禁止事項である。(設定有)"},
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
                {lang.commandError, "Error:%1$"},
                {lang.InvalidArgs, "Invalis Args"},
                {lang.ON, "ON"},
                {lang.OFF, "OFF"},
            };
            EnglishRoleNames = new Dictionary<CustomRoles, string>(){
                {CustomRoles.Default, "Vanilla"},
                {CustomRoles.Jester, "Jester"},
                {CustomRoles.Madmate, "Madmate"},
                {CustomRoles.MadGuardian, "MadGuardian"},
                {CustomRoles.Bait, "Bait"},
                {CustomRoles.Terrorist, "Terrorist"},
                {CustomRoles.Sidekick, "Sidekick"},
                {CustomRoles.Vampire, "Vampire"},
                {CustomRoles.SabotageMaster, "SabotageMaster"},
                {CustomRoles.Mayor, "Mayor"},
                {CustomRoles.Opportunist, "Opportunist"},
                {CustomRoles.Fox, "Fox"},
                {CustomRoles.Troll, "Troll"},
            };
            JapaneseRoleNames = new Dictionary<CustomRoles, string>(){
                {CustomRoles.Default, "Vanilla"},
                {CustomRoles.Jester, "ジェスター"},
                {CustomRoles.Madmate, "狂人"},
                {CustomRoles.MadGuardian, "守護狂人"},
                {CustomRoles.Bait, "ベイト"},
                {CustomRoles.Terrorist, "テロリスト"},
                {CustomRoles.Sidekick, "相棒"},
                {CustomRoles.Vampire, "吸血鬼"},
                {CustomRoles.SabotageMaster, "サボタージュマスター"},
                {CustomRoles.Mayor, "メイヤー"},
                {CustomRoles.Opportunist, "オポチュニスト"},
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
        SidekickInfo,
        BeforeSidekickInfo,
        AfterSidekickInfo,
        VampireInfo,
        SabotageMasterInfo,
        MadGuardianInfo,
        MayorInfo,
        OpportunistInfo,
        FoxInfo,
        TrollInfo,
        //役職解説(長)
        JesterInfoLong,
        MadmateInfoLong,
        BaitInfoLong,
        TerroristInfoLong,
        SidekickInfoLong,
        VampireInfoLong,
        SabotageMasterInfoLong,
        MadGuardianInfoLong,
        MayorInfoLong,
        OpportunistInfoLong,
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
        commandError,
        InvalidArgs,
        ON,
        OFF,
    }
    public enum CustomRoles {
        Default = 0,
        Jester,
        Madmate,
        Bait,
        Terrorist,
        Sidekick,
        Vampire,
        SabotageMaster,
        MadGuardian,
        Mayor,
        Opportunist,
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
