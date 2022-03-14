﻿using System.Text.RegularExpressions;
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
        public static Dictionary<byte, string> RealNames;
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
