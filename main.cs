using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using HarmonyLib;

[assembly: AssemblyFileVersionAttribute(TownOfHost.Main.PluginVersion)]
[assembly: AssemblyInformationalVersionAttribute(TownOfHost.Main.PluginVersion)]
namespace TownOfHost
{
    [BepInPlugin(PluginGuid, "Town Of Host", PluginVersion)]
    [BepInProcess("Among Us.exe")]
    public class Main : BasePlugin
    {
        //Sorry for many Japanese comments.
        public const string PluginGuid = "com.emptybottle.townofhost";
        public const string PluginVersion = "2.1.0";
        public Harmony Harmony { get; } = new Harmony(PluginGuid);
        public static Version version = Version.Parse(PluginVersion);
        public static BepInEx.Logging.ManualLogSource Logger;
        public static bool hasArgumentException = false;
        public static string ExceptionMessage;
        public static bool ExceptionMessageIsShown = false;
        public static string credentialsText;
        //Client Options
        public static ConfigEntry<bool> HideCodes { get; private set; }
        public static ConfigEntry<string> HideName { get; private set; }
        public static ConfigEntry<string> HideColor { get; private set; }
        public static ConfigEntry<bool> ForceJapanese { get; private set; }
        public static ConfigEntry<bool> JapaneseRoleName { get; private set; }
        public static ConfigEntry<bool> AmDebugger { get; private set; }
        public static ConfigEntry<string> ShowPopUpVersion { get; private set; }
        public static ConfigEntry<int> MessageWait { get; private set; }

        public static LanguageUnit EnglishLang { get; private set; }
        public static Dictionary<byte, PlayerVersion> playerVersion = new();
        //Other Configs
        public static ConfigEntry<bool> IgnoreWinnerCommand { get; private set; }
        public static ConfigEntry<string> WebhookURL { get; private set; }
        public static CustomWinner currentWinner;
        public static HashSet<AdditionalWinners> additionalwinners = new();
        public static GameOptionsData RealOptionsData;
        public static Dictionary<byte, string> AllPlayerNames;
        public static Dictionary<(byte, byte), string> LastNotifyNames;
        public static Dictionary<byte, CustomRoles> AllPlayerCustomRoles;
        public static Dictionary<byte, CustomRoles> AllPlayerCustomSubRoles;
        public static Dictionary<byte, bool> SelfGuard;
        public static Dictionary<byte, bool> BlockKilling;
        public static Dictionary<byte, float> SheriffShotLimit;
        public static Dictionary<byte, PlayerState.DeathReason> AfterMeetingDeathPlayers = new();
        public static Dictionary<CustomRoles, String> roleColors;
        //これ変えたらmod名とかの色が変わる
        public static string modColor = "#00bfff";
        public static bool IsFixedCooldown => CustomRoles.Vampire.IsEnable();
        public static float RefixCooldownDelay = 0f;
        public static int BeforeFixMeetingCooldown = 10;
        public static List<byte> ResetCamPlayerList;
        public static List<byte> winnerList;
        public static List<(string, byte)> MessagesToSend;
        public static bool isChatCommand = false;
        public static string TextCursor => TextCursorVisible ? "_" : "";
        public static bool TextCursorVisible;
        public static float TextCursorTimer;
        public static List<PlayerControl> LoversPlayers = new();
        public static bool isLoversDead = true;
        public static Dictionary<byte, float> AllPlayerKillCooldown = new();
        public static Dictionary<byte, float> AllPlayerSpeed = new();
        public static Dictionary<byte, (byte, float)> BitPlayers = new();
        public static Dictionary<byte, float> SerialKillerTimer = new();
        public static Dictionary<byte, float> BountyTimer = new();
        public static Dictionary<byte, float> WarlockTimer = new();
        public static Dictionary<byte, PlayerControl> BountyTargets;
        public static Dictionary<byte, bool> isTargetKilled = new();
        public static Dictionary<byte, PlayerControl> CursedPlayers = new();
        public static List<PlayerControl> SpelledPlayer = new();
        public static Dictionary<byte, bool> KillOrSpell = new();
        public static Dictionary<byte, bool> isCurseAndKill = new();
        public static Dictionary<(byte, byte), bool> isDoused = new();
        public static Dictionary<byte, (PlayerControl, float)> ArsonistTimer = new();
        public static Dictionary<byte, float> AirshipMeetingTimer = new();
        public static Dictionary<byte, byte> ExecutionerTarget = new(); //Key : Executioner, Value : target
        public static Dictionary<byte, byte> PuppeteerList = new(); // Key: targetId, Value: PuppeteerId
        public static Dictionary<byte, byte> SpeedBoostTarget = new();
        public static Dictionary<byte, int> MayorUsedButtonCount = new();
        public static Dictionary<byte, int> TimeThiefKillCount = new();
        public static int AliveImpostorCount;
        public static int SKMadmateNowCount;
        public static bool witchMeeting;
        public static bool isCursed;
        public static bool isShipStart;
        public static Dictionary<byte, bool> CheckShapeshift = new();
        public static Dictionary<(byte, byte), string> targetArrows = new();
        public static byte WonTrollID;
        public static byte ExiledJesterID;
        public static byte WonTerroristID;
        public static byte WonExecutionerID;
        public static byte WonArsonistID;
        public static bool CustomWinTrigger;
        public static bool VisibleTasksCount;
        public static string nickName = "";
        public static bool introDestroyed = false;
        public static int DiscussionTime;
        public static int VotingTime;
        public static byte currentDousingTarget;

        public static Main Instance;

        public override void Load()
        {
            Instance = this;

            TextCursorTimer = 0f;
            TextCursorVisible = true;

            //Client Options
            HideCodes = Config.Bind("Client Options", "Hide Game Codes", false);
            HideName = Config.Bind("Client Options", "Hide Game Code Name", "Town Of Host");
            HideColor = Config.Bind("Client Options", "Hide Game Code Color", $"{modColor}");
            ForceJapanese = Config.Bind("Client Options", "Force Japanese", false);
            JapaneseRoleName = Config.Bind("Client Options", "Japanese Role Name", true);
            Logger = BepInEx.Logging.Logger.CreateLogSource("TownOfHost");
            TownOfHost.Logger.Enable();
            TownOfHost.Logger.Disable("NotifyRoles");
            TownOfHost.Logger.Disable("SendRPC");
            TownOfHost.Logger.Disable("ReceiveRPC");
            TownOfHost.Logger.Disable("SwitchSystem");
            //TownOfHost.Logger.isDetail = true;

            currentWinner = CustomWinner.Default;
            additionalwinners = new HashSet<AdditionalWinners>();

            AllPlayerCustomRoles = new Dictionary<byte, CustomRoles>();
            AllPlayerCustomSubRoles = new Dictionary<byte, CustomRoles>();
            CustomWinTrigger = false;
            BitPlayers = new Dictionary<byte, (byte, float)>();
            SerialKillerTimer = new Dictionary<byte, float>();
            BountyTimer = new Dictionary<byte, float>();
            WarlockTimer = new Dictionary<byte, float>();
            BountyTargets = new Dictionary<byte, PlayerControl>();
            CursedPlayers = new Dictionary<byte, PlayerControl>();
            SpelledPlayer = new List<PlayerControl>();
            isDoused = new Dictionary<(byte, byte), bool>();
            ArsonistTimer = new Dictionary<byte, (PlayerControl, float)>();
            ExecutionerTarget = new Dictionary<byte, byte>();
            MayorUsedButtonCount = new Dictionary<byte, int>();
            winnerList = new();
            VisibleTasksCount = false;
            MessagesToSend = new List<(string, byte)>();
            currentDousingTarget = 255;

            IgnoreWinnerCommand = Config.Bind("Other", "IgnoreWinnerCommand", true);
            WebhookURL = Config.Bind("Other", "WebhookURL", "none");
            AmDebugger = Config.Bind("Other", "AmDebugger", false);
            ShowPopUpVersion = Config.Bind("Other", "ShowPopUpVersion", "0");
            MessageWait = Config.Bind("Other", "MessageWait", 1);

            NameColorManager.Begin();

            Translator.Init();

            BlockKilling = new Dictionary<byte, bool>();

            hasArgumentException = false;
            ExceptionMessage = "";
            try
            {

                roleColors = new Dictionary<CustomRoles, string>(){
                //バニラ役職
                {CustomRoles.Crewmate, "#ffffff"},
                {CustomRoles.Engineer, "#b6f0ff"},
                {CustomRoles.Scientist, "#b6f0ff"},
                {CustomRoles.GuardianAngel, "#ffffff"},
                {CustomRoles.Impostor, "#ff0000"},
                {CustomRoles.Shapeshifter, "#ff0000"},
                //特殊インポスター役職
                {CustomRoles.Vampire, "#ff0000"},
                {CustomRoles.Mafia, "#ff0000"},
                {CustomRoles.EvilWatcher, "#ff0000"}, //ウォッチャーの派生
                {CustomRoles.BountyHunter, "#ff0000"},
                {CustomRoles.Witch, "#ff0000"},
                {CustomRoles.ShapeMaster, "#ff0000"},
                {CustomRoles.Warlock, "#ff0000"},
                {CustomRoles.SerialKiller, "#ff0000"},
                {CustomRoles.Mare, "#ff0000"},
                {CustomRoles.Puppeteer, "#ff0000"},
                {CustomRoles.FireWorks, "#ff0000"},
                {CustomRoles.TimeThief, "#ff0000"},
                {CustomRoles.Sniper, "#ff0000"},
                {CustomRoles.SlaveDriver, "#ff0000"},
                //マッドメイト系役職
                {CustomRoles.Madmate, "#ff0000"},
                {CustomRoles.SKMadmate, "#ff0000"},
                {CustomRoles.MadGuardian, "#ff0000"},
                {CustomRoles.MadSnitch, "#ff0000"},
                {CustomRoles.MSchrodingerCat, "#ff0000"}, //シュレディンガーの猫の派生
                //両陣営可能役職
                {CustomRoles.Watcher, "#800080"},
                //特殊クルー役職
                {CustomRoles.NiceWatcher, "#800080"}, //ウォッチャーの派生
                {CustomRoles.Bait, "#00f7ff"},
                {CustomRoles.SabotageMaster, "#0000ff"},
                {CustomRoles.Snitch, "#b8fb4f"},
                {CustomRoles.Mayor, "#204d42"},
                {CustomRoles.Sheriff, "#f8cd46"},
                {CustomRoles.Lighter, "#eee5be"},
                {CustomRoles.SpeedBooster, "#00ffff"},
                {CustomRoles.Doctor, "#80ffdd"},
                {CustomRoles.Trapper, "#5a8fd0"},
                {CustomRoles.Dictator, "#df9b00"},
                {CustomRoles.CSchrodingerCat, "#ffffff"}, //シュレディンガーの猫の派生
                //第三陣営役職
                {CustomRoles.Arsonist, "#ff6633"},
                {CustomRoles.Jester, "#ec62a5"},
                {CustomRoles.Terrorist, "#00ff00"},
                {CustomRoles.Executioner, "#611c3a"},
                {CustomRoles.Opportunist, "#00ff00"},
                {CustomRoles.SchrodingerCat, "#696969"},
                {CustomRoles.EgoSchrodingerCat, "#5600ff"}, //シュレディンガーの猫の派生
                {CustomRoles.Egoist, "#5600ff"},
                //HideAndSeek
                {CustomRoles.HASFox, "#e478ff"},
                {CustomRoles.HASTroll, "#00ff00"},
                //サブ役職
                {CustomRoles.NoSubRoleAssigned, "#ffffff"},
                {CustomRoles.Lovers, "#ffaaaa"},
            };
            }
            catch (ArgumentException ex)
            {
                TownOfHost.Logger.Error("エラー:Dictionaryの値の重複を検出しました", "LoadDictionary");
                TownOfHost.Logger.Error(ex.Message, "LoadDictionary");
                hasArgumentException = true;
                ExceptionMessage = ex.Message;
                ExceptionMessageIsShown = false;
            }
            TownOfHost.Logger.Info($"{nameof(ThisAssembly.Git.Branch)}: {ThisAssembly.Git.Branch}", "GitVersion");
            TownOfHost.Logger.Info($"{nameof(ThisAssembly.Git.BaseTag)}: {ThisAssembly.Git.BaseTag}", "GitVersion");
            TownOfHost.Logger.Info($"{nameof(ThisAssembly.Git.Commit)}: {ThisAssembly.Git.Commit}", "GitVersion");
            TownOfHost.Logger.Info($"{nameof(ThisAssembly.Git.Commits)}: {ThisAssembly.Git.Commits}", "GitVersion");
            TownOfHost.Logger.Info($"{nameof(ThisAssembly.Git.IsDirty)}: {ThisAssembly.Git.IsDirty}", "GitVersion");
            TownOfHost.Logger.Info($"{nameof(ThisAssembly.Git.Sha)}: {ThisAssembly.Git.Sha}", "GitVersion");
            TownOfHost.Logger.Info($"{nameof(ThisAssembly.Git.Tag)}: {ThisAssembly.Git.Tag}", "GitVersion");

            Harmony.PatchAll();
        }

        [HarmonyPatch(typeof(TranslationController), nameof(TranslationController.Initialize))]
        class TranslationControllerInitializePatch
        {
            public static void Postfix(TranslationController __instance)
            {
                var english = __instance.Languages.Where(lang => lang.languageID == SupportedLangs.English).FirstOrDefault();
                EnglishLang = new LanguageUnit(english);
            }
        }
    }
    public enum CustomRoles
    {
        //Default
        Crewmate = 0,
        //Impostor(Vanilla)
        Impostor,
        Shapeshifter,
        //Impostor
        BountyHunter,
        EvilWatcher,
        FireWorks,
        Mafia,
        SerialKiller,
        ShapeMaster,
        SlaveDriver,
        Sniper,
        Vampire,
        Witch,
        Warlock,
        Mare,
        Puppeteer,
        TimeThief,
        //Madmate
        MadGuardian,
        Madmate,
        MadSnitch,
        SKMadmate,
        MSchrodingerCat,//インポスター陣営のシュレディンガーの猫
        //両陣営
        Watcher,
        //Crewmate(Vanilla)
        Engineer,
        GuardianAngel,
        Scientist,
        //Crewmate
        Bait,
        Lighter,
        Mayor,
        NiceWatcher,
        SabotageMaster,
        Sheriff,
        Snitch,
        SpeedBooster,
        Trapper,
        Dictator,
        Doctor,
        CSchrodingerCat,//クルー陣営のシュレディンガーの猫
        //Neutral
        Arsonist,
        Egoist,
        EgoSchrodingerCat,//エゴイスト陣営のシュレディンガーの猫
        Jester,
        Opportunist,
        SchrodingerCat,//第三陣営のシュレディンガーの猫
        Terrorist,
        Executioner,
        //HideAndSeek
        HASFox,
        HASTroll,
        // Sub-roll after 500
        NoSubRoleAssigned = 500,
        Lovers,
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
        Lovers,
        Executioner,
        Arsonist,
        Egoist,
        HASTroll
    }
    public enum AdditionalWinners
    {
        None = 0,
        Opportunist,
        SchrodingerCat,
        Executioner,
        HASFox
    }
    /*public enum CustomRoles : byte
    {
        Default = 0,
        HASTroll = 1,
        HASHox = 2
    }*/
    public enum SuffixModes
    {
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
        SelfVote,
        Skip
    }
}