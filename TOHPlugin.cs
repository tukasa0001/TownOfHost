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
    public const string PluginGuid = "com.discussions.tohtor";
    public const string PluginVersion = "1.0.0";
    public readonly GitVersion CurrentVersion = new();

    public static readonly string ModName = "Town Of Host: The Other Roles";
    public static readonly string ModColor = "#4FF918";

    public static readonly bool ShowDiscordButton = true;
    public static readonly string DiscordInviteUrl = "https://discord.gg/tohtor";

    public static readonly bool DevVersion = true;
    public static readonly string DevVersionStr = "dev 1";

    public Harmony Harmony { get; } = new(PluginGuid);
    public static string CredentialsText;

    public static bool Initialized;

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










    public static NormalGameOptionsV07 NormalOptions => GameOptionsManager.Instance.currentNormalGameOptions;


    public static Dictionary<byte, GitVersion> playerVersion = new();


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



    public override void Load()
    {
        OptionManager = new OptionManager();
        GamemodeManager = new GamemodeManager();

        VisibleTasksCount = false;
        MessagesToSend = new List<(string, byte, string)>();

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