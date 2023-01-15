using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;
using TownOfHost.Addons;
using TownOfHost.Options;
using TownOfHost.Roles;
using Reactor;
using Reactor.Networking.Attributes;
using TownOfHost.Gamemodes;
using TownOfHost.Managers;
using VentLib;
using VentLib.Logging;

[assembly: AssemblyFileVersion(TownOfHost.TOHPlugin.PluginVersion)]
[assembly: AssemblyInformationalVersion(TownOfHost.TOHPlugin.PluginVersion)]
namespace TownOfHost;

[BepInPlugin(PluginGuid, "Town Of Host", PluginVersion)]
[BepInProcess("Among Us.exe")]
[BepInDependency(ReactorPlugin.Id)]
[ReactorModFlags(Reactor.Networking.ModFlags.None)]
public class TOHPlugin : BasePlugin
{
    public TOHPlugin()
    {
        Instance = this;
        VentFramework.Initialize();
        VentLogger.Configuration.SetLevel(LogLevel.All);
    }

    // == プログラム設定 / Program Config ==
    public static readonly string ModName = "Town Of Host: The Other Roles";
    public static readonly string ModColor = "#4FF918";
    public static readonly bool AllowPublicRoom = true;
    public static readonly string ForkId = "OriginalTOHTOR";
    public static readonly bool ShowDiscordButton = true;
    public static readonly string DiscordInviteUrl = "https://discord.gg/tohtor";
    // ==========
    public const string OriginalForkId = "OriginalTOH"; // Don't Change The Value. / この値を変更しないでください。

    // デバッグキーのコンフィグ入力
    public static ConfigEntry<string> DebugKeyInput { get; private set; }
    public static readonly bool DevVersion = true;


    public const string PluginGuid = "com.discussions.tohtor";
    public const string PluginVersion = "1.0.0";
    public static readonly string DevVersionStr = "dev 1";
    public Harmony Harmony { get; } = new(PluginGuid);
    public static Version version = Version.Parse(PluginVersion);
    public static BepInEx.Logging.ManualLogSource Logger;

    public static string CredentialsText;

    public static NormalGameOptionsV07 NormalOptions => GameOptionsManager.Instance.currentNormalGameOptions;
    public static HideNSeekGameOptionsV07 HideNSeekSOptions => GameOptionsManager.Instance.currentHideNSeekGameOptions;
    //Client Options
    public static ConfigEntry<string> HideName { get; private set; }
    public static ConfigEntry<string> HideColor { get; private set; }
    public static ConfigEntry<int> MessageWait { get; private set; }

    public static Dictionary<byte, PlayerVersion> playerVersion = new();
    //Other Configs
    public static ConfigEntry<string> WebhookURL { get; private set; }
    public static ConfigEntry<string> BetaBuildURL { get; private set; }
    public static Dictionary<byte, string> AllPlayerNames;
    public static Dictionary<byte, Color32> PlayerColors = new();
    public static bool IsFixedCooldown => Vampire.Ref<Vampire>().IsEnable();

    public static bool NoGameEnd { get; set; }
    [Obsolete]
    public static bool HasNecronomicon { get; set; }

    public static bool Initialized;

    public static float RefixCooldownDelay = 0f;
    public static List<byte> ResetCamPlayerList;
    public static List<(string, byte, string)> MessagesToSend;
    public static bool isChatCommand = false;
    public static Dictionary<byte, float> AllPlayerKillCooldown = new();

    /// <summary>
    /// Key: ターゲットのPlayerId, Value: パペッティアのPlayerId
    /// </summary>
    public static int SKMadmateNowCount;
    public static bool VisibleTasksCount;


    public static GamemodeManager GamemodeManager;
    public static OptionManager OptionManager;
    public static TOHPlugin Instance;
    public static List<byte> unreportableBodies = new();


    public override void Load()
    {
        //Client Options
        HideName = Config.Bind("Client Options", "Hide Game Code Name", "Town Of Host");
        HideColor = Config.Bind("Client Options", "Hide Game Code Color", $"{ModColor}");
        DebugKeyInput = Config.Bind("Authentication", "Debug Key", "");

        Logger = BepInEx.Logging.Logger.CreateLogSource("TownOfHost");

        OptionManager = new OptionManager();
        GamemodeManager = new GamemodeManager();

        VisibleTasksCount = false;
        MessagesToSend = new List<(string, byte, string)>();

        WebhookURL = Config.Bind("Other", "WebhookURL", "none");
        BetaBuildURL = Config.Bind("Other", "BetaBuildURL", "");
        MessageWait = Config.Bind("Other", "MessageWait", 1);

        BanManager.Init();
        TemplateManager.Init();

        VentLogger.Info($"{Application.version}", "AmongUs Version");

        VentLogger.Info($"{nameof(ThisAssembly.Git.Branch)}: {ThisAssembly.Git.Branch}", "GitVersion");
        VentLogger.Info($"{nameof(ThisAssembly.Git.BaseTag)}: {ThisAssembly.Git.BaseTag}", "GitVersion");
        VentLogger.Info($"{nameof(ThisAssembly.Git.Commit)}: {ThisAssembly.Git.Commit}", "GitVersion");
        VentLogger.Info($"{nameof(ThisAssembly.Git.Commits)}: {ThisAssembly.Git.Commits}", "GitVersion");
        VentLogger.Info($"{nameof(ThisAssembly.Git.IsDirty)}: {ThisAssembly.Git.IsDirty}", "GitVersion");
        VentLogger.Info($"{nameof(ThisAssembly.Git.Sha)}: {ThisAssembly.Git.Sha}", "GitVersion");
        VentLogger.Info($"{nameof(ThisAssembly.Git.Tag)}: {ThisAssembly.Git.Tag}", "GitVersion");

        // Setup, order matters here

        GameOptionTab __ = DefaultTabs.GeneralTab;
        int _ = CustomRoleManager.AllRoles.Count;
        Harmony.PatchAll();
        AddonManager.ImportAddons();

        GamemodeManager.Setup();
        StaticOptions.AddStaticOptions();
        OptionManager.AllHolders.AddRange(OptionManager.Options().SelectMany(opt => opt.GetHoldersRecursive()));
        Initialized = true;
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
    //ShapeMaster,
    Sniper,
    Vampire,
    Witch,
    Warlock,
    Mare,
    Puppeteer,
    TimeThief,
    EvilTracker,
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
    Seer,
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
    Jackal,
    JSchrodingerCat,//ジャッカル陣営のシュレディンガーの猫
    //HideAndSeek
    HASFox,
    HASTroll,
    //GM
    GM,
    // Sub-roll after 500
    NotAssigned = 500,
    LastImpostor,
    Lovers,
}
//WinData
public enum CustomWinner
{
    Draw = -1,
    Default = -2,
    None = -3,
    Impostor = CustomRoles.Impostor,
    Crewmate = CustomRoles.Crewmate,
    Jester = CustomRoles.Jester,
    Terrorist = CustomRoles.Terrorist,
    Lovers = CustomRoles.Lovers,
    Executioner = CustomRoles.Executioner,
    Arsonist = CustomRoles.Arsonist,
    Egoist = CustomRoles.Egoist,
    Jackal = CustomRoles.Jackal,
    HASTroll = CustomRoles.HASTroll,
}
public enum AdditionalWinners
{
    None = -1,
    Opportunist = CustomRoles.Opportunist,
    SchrodingerCat = CustomRoles.SchrodingerCat,
    Executioner = CustomRoles.Executioner,
    HASFox = CustomRoles.HASFox,
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
    Recording,
    RoomHost,
    OriginalName
}
public enum VoteMode
{
    Default,
    Suicide,
    SelfVote,
    Skip
}

public enum TieMode
{
    Default,
    All,
    Random
}