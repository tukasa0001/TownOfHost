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
using TownOfHost.Addons;
using TownOfHost.Options;
using TownOfHost.Roles;
using TownOfHost.Gamemodes;
using TownOfHost.Managers;
using VentLib;
using VentLib.Logging;
using VentLib.Version;
using VentLib.Version.Git;
using VentLib.Version.Handshake;
using Version = VentLib.Version.Version;

[assembly: AssemblyFileVersion(TownOfHost.TOHPlugin.PluginVersion)]
[assembly: AssemblyInformationalVersion(TownOfHost.TOHPlugin.PluginVersion)]
namespace TownOfHost;

[BepInPlugin(PluginGuid, "Town Of Host", PluginVersion)]
[BepInProcess("Among Us.exe")]
public class TOHPlugin : BasePlugin, IGitVersionEmitter
{
    public readonly GitVersion CurrentVersion = new();

    public TOHPlugin()
    {
        Instance = this;
        Vents.Initialize();
        Vents.VersionControl.For(this);
        Vents.VersionControl.AddVersionReceiver(
            (ver, player) => playerVersion[player.PlayerId] = (GitVersion)ver,
            ReceiveExecutionFlag.OnSuccessfulHandshake);

        VentLogger.Configuration.SetLevel(LogLevel.Trace);
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
    public static BepInEx.Logging.ManualLogSource Logger;

    public static string CredentialsText;

    public static NormalGameOptionsV07 NormalOptions => GameOptionsManager.Instance.currentNormalGameOptions;
    public static HideNSeekGameOptionsV07 HideNSeekSOptions => GameOptionsManager.Instance.currentHideNSeekGameOptions;
    //Client Options
    public static ConfigEntry<string> HideName { get; private set; }
    public static ConfigEntry<string> HideColor { get; private set; }
    public static ConfigEntry<int> MessageWait { get; private set; }

    public static Dictionary<byte, GitVersion> playerVersion = new();
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

        VentLogger.Info(CurrentVersion.ToString(), "GitVersion");

        // Setup, order matters here

        GameOptionTab __ = DefaultTabs.GeneralTab;
        int _ = CustomRoleManager.AllRoles.Count;
        Harmony.PatchAll(Assembly.GetExecutingAssembly());
        AddonManager.ImportAddons();

        GamemodeManager.Setup();
        StaticOptions.AddStaticOptions();
        OptionManager.AllHolders.AddRange(OptionManager.Options().SelectMany(opt => opt.GetHoldersRecursive()));
        Initialized = true;
    }

    public GitVersion Version() => CurrentVersion;

    public HandshakeResult HandshakeFilter(Version handshake)
    {
        if (handshake is NoVersion) return HandshakeResult.FailDoNothing;
        if (handshake is not GitVersion git) return HandshakeResult.DisableRPC;
        if (git.MajorVersion != CurrentVersion.MajorVersion && git.MinorVersion != CurrentVersion.MinorVersion) return HandshakeResult.FailDoNothing;
        return HandshakeResult.PassDoNothing;
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