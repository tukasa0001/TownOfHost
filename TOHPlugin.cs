using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP;
using TOHTOR.Addons;
using TOHTOR.Roles;
using TOHTOR.Gamemodes;
using TOHTOR.Managers;
using TOHTOR.Managers.Date;
using TOHTOR.Options;
using TOHTOR.Roles.Internals.Attributes;
using TOHTOR.RPC;
using VentLib;
using VentLib.Logging;
using VentLib.Networking.Handshake;
using VentLib.Networking.RPC;
using VentLib.Options;
using VentLib.Options.Game;
using VentLib.Options.Game.Tabs;
using VentLib.Utilities;
using VentLib.Utilities.Collections;
using VentLib.Utilities.Extensions;
using VentLib.Version;
using VentLib.Version.Git;
using Version = VentLib.Version.Version;

[assembly: AssemblyFileVersion(TOHTOR.TOHPlugin.PluginVersion)]
[assembly: AssemblyInformationalVersion(TOHTOR.TOHPlugin.PluginVersion)]
namespace TOHTOR;

[BepInPlugin(PluginGuid, "TOHTOR", PluginVersion)]
[BepInProcess("Among Us.exe")]
public class TOHPlugin : BasePlugin, IGitVersionEmitter
{
    public const string PluginGuid = "com.discussions.tohtor";
    public const string PluginVersion = "1.0.0";
    public readonly GitVersion CurrentVersion = new GitVersion();

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
        Vents.VersionControl.AddVersionReceiver(ReceiveVersion);
    }


    public static NormalGameOptionsV07 NormalOptions => GameOptionsManager.Instance.currentNormalGameOptions;


    public static Dictionary<byte, Version> PlayerVersion = new();


    public static float RefixCooldownDelay = 0f;
    public static List<byte> ResetCamPlayerList = null!;
    public static Dictionary<byte, float> AllPlayerKillCooldown = new();

    /// <summary>
    /// Key: ターゲットのPlayerId, Value: パペッティアのPlayerId
    /// </summary>
    public static int SKMadmateNowCount;
    public static bool VisibleTasksCount;

    public static PluginDataManager PluginDataManager;
    public static GamemodeManager GamemodeManager;
    public static TOHPlugin Instance = null!;

    public static GameOptionTab TestTab = new("Test Tab", () => Utils.LoadSprite("TOHTOR.assets.Tabs.TabIcon_MiscRoles.png"));


    public override void Load()
    {
        GameOptionController.Enable();
        GamemodeManager = new GamemodeManager();
        PluginDataManager = new PluginDataManager();

        VisibleTasksCount = false;

        VentLogger.Fatal($"Test: {new[] { "a", "b", "c", "d" }.Indexed().StrJoin()})");

        BanManager.Init();

        VentLogger.Info($"{Application.version}", "AmongUs Version");
        VentLogger.Info(CurrentVersion.ToString(), "GitVersion");

        // Setup, order matters here

        int _ = CustomRoleManager.AllRoles.Count;
        StaticEditor.Register(Assembly.GetExecutingAssembly());
        Harmony.PatchAll(Assembly.GetExecutingAssembly());
        AddonManager.ImportAddons();

        GamemodeManager.Setup();
        StaticOptions.AddStaticOptions();
        ShowerPages.InitPages();
        //OptionManager.AllHolders.AddRange(OptionManager.Options().SelectMany(opt => opt.GetHoldersRecursive()));
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

    private static void ReceiveVersion(Version version, PlayerControl player)
    {
        PlayerVersion[player.PlayerId] = version;

        if (version is not NoVersion)
        {
            ModRPC rpc = Vents.FindRPC((uint)ModCalls.SendOptionPreview)!;
            rpc.Send(new[] { player.GetClientId() }, new BatchList<Option>(VentLib.Options.OptionManager.GetManager().GetOptions()));
        }

        if (PluginDataManager.TemplateManager.TryFormat(player, "lobby-join", out string message)) Utils.SendMessage(message, player.PlayerId);
    }
}