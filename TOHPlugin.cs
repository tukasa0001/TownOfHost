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
using TownOfHost.ReduxOptions;
using TownOfHost.Roles;
using Reactor;
using Reactor.Networking.Attributes;
using TownOfHost.Gamemodes;
using VentLib;

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
    }

    // == プログラム設定 / Program Config ==
    // modの名前 / Mod Name (Default: Town Of Host)
    public static readonly string ModName = "Town Of Host: The Other Roles";
    // modの色 / Mod Color (Default: #00bfff)
    public static readonly string ModColor = "#4FF918";
    // 公開ルームを許可する / Allow Public Room (Default: true)
    public static readonly bool AllowPublicRoom = true;
    // フォークID / ForkId (Default: OriginalTOH)
    public static readonly string ForkId = "OriginalTOHTOR";
    // Discordボタンを表示するか / Show Discord Button (Default: true)
    public static readonly bool ShowDiscordButton = true;
    // Discordサーバーの招待リンク / Discord Server Invite URL (Default: https://discord.gg/W5ug6hXB9V)
    public static readonly string DiscordInviteUrl = "https://discord.gg/tohtor";
    // ==========
    public const string OriginalForkId = "OriginalTOH"; // Don't Change The Value. / この値を変更しないでください。
    // == 認証設定 / Authentication Config ==
    // デバッグキーの認証インスタンス
    public static HashAuth DebugKeyAuth { get; private set; }
    // デバッグキーのハッシュ値
    public const string DebugKeyHash = "c0fd562955ba56af3ae20d7ec9e64c664f0facecef4b3e366e109306adeae29d";
    // デバッグキーのソルト
    public const string DebugKeySalt = "59687b";
    // デバッグキーのコンフィグ入力
    public static ConfigEntry<string> DebugKeyInput { get; private set; }
    // Dev Version (Default: false)
    public static readonly bool DevVersion = true;

    // ==========
    //Sorry for many Japanese comments.
    public const string PluginGuid = "com.discussions.tohtor";
    public const string PluginVersion = "0.9.4";
    public static readonly string DevVersionStr = "dev 1";
    public Harmony Harmony { get; } = new(PluginGuid);
    public static Version version = Version.Parse(PluginVersion);
    public static BepInEx.Logging.ManualLogSource Logger;

    public static bool HasArgumentException = false;
    public static string CredentialsText;

    public static NormalGameOptionsV07 NormalOptions => GameOptionsManager.Instance.currentNormalGameOptions;
    public static HideNSeekGameOptionsV07 HideNSeekSOptions => GameOptionsManager.Instance.currentHideNSeekGameOptions;
    //Client Options
    public static ConfigEntry<string> HideName { get; private set; }
    public static ConfigEntry<string> HideColor { get; private set; }
    public static ConfigEntry<bool> ForceJapanese { get; private set; }
    public static ConfigEntry<bool> JapaneseRoleName { get; private set; }
    public static ConfigEntry<int> MessageWait { get; private set; }

    public static Dictionary<byte, PlayerVersion> playerVersion = new();
    //Other Configs
    public static ConfigEntry<string> WebhookURL { get; private set; }
    public static ConfigEntry<string> BetaBuildURL { get; private set; }
    public static ConfigEntry<float> LastKillCooldown { get; private set; }
    public static OptionBackupData RealOptionsData;
    public static Dictionary<byte, PlayerStateOLD> PlayerStates = new();
    public static Dictionary<byte, string> AllPlayerNames;
    public static Dictionary<byte, Color32> PlayerColors = new();
    public static Dictionary<byte, PlayerStateOLD.DeathReason> AfterMeetingDeathPlayers = new();
    public static Dictionary<CustomRoles, String> roleColors;
    public static bool IsFixedCooldown => Vampire.Ref<Vampire>().IsEnable();
    [Obsolete("Switching to Gamemode Manager")]
    public static CustomGameMode CurrentGameMode { get; set; }

    public static bool NoGameEnd { get; set; }
    [Obsolete]
    public static bool HasNecronomicon { get; set; }

    //public static Dictionary<byte, Vector2> LastEnteredVentLocation { get; set; }

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
    // TODO fix
    public static List<byte> unreportableBodies = new();


    public override void Load()
    {
        //Client Options
        HideName = Config.Bind("Client Options", "Hide Game Code Name", "Town Of Host");
        HideColor = Config.Bind("Client Options", "Hide Game Code Color", $"{ModColor}");
        ForceJapanese = Config.Bind("Client Options", "Force Japanese", false);
        JapaneseRoleName = Config.Bind("Client Options", "Japanese Role Name", true);
        DebugKeyInput = Config.Bind("Authentication", "Debug Key", "");

        Logger = BepInEx.Logging.Logger.CreateLogSource("TownOfHost");
        global::TownOfHost.Logger.Enable();
        global::TownOfHost.Logger.Disable("NotifyRoles");
        global::TownOfHost.Logger.Disable("SendRPC");
        global::TownOfHost.Logger.Disable("ReceiveRPC");
        global::TownOfHost.Logger.Disable("SwitchSystem");
        global::TownOfHost.Logger.Disable("CustomRpcSender");
        global::TownOfHost.Logger.Disable("SendChat");

        OptionManager = new OptionManager();
        GamemodeManager = new GamemodeManager();

        //TownOfHost.Logger.isDetail = true;

        // 認証関連-初期化
        DebugKeyAuth = new HashAuth(DebugKeyHash, DebugKeySalt);

        // 認証関連-認証
        DebugModeManager.Auth(DebugKeyAuth, DebugKeyInput.Value);

        VisibleTasksCount = false;
        MessagesToSend = new List<(string, byte, string)>();

        WebhookURL = Config.Bind("Other", "WebhookURL", "none");
        BetaBuildURL = Config.Bind("Other", "BetaBuildURL", "");
        MessageWait = Config.Bind("Other", "MessageWait", 1);
        LastKillCooldown = Config.Bind("Other", "LastKillCooldown", (float)30);

        CustomWinnerHolder.Reset();
        Translator.Init();
        BanManager.Init();
        TemplateManager.Init();

        IRandom.SetInstance(new NetRandomWrapper());

        HasArgumentException = false;
        try
        {

            roleColors = new Dictionary<CustomRoles, string>()
            {
                //バニラ役職
                {CustomRoles.Crewmate, "#ffffff"},
                {CustomRoles.Engineer, "#8cffff"},
                {CustomRoles.Scientist, "#8cffff"},
                {CustomRoles.GuardianAngel, "#ffffff"},
                //インポスター、シェイプシフター
                //特殊インポスター役職
                //マッドメイト系役職
                //後で追加
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
                {CustomRoles.Seer, "#61b26c"},
                //第三陣営役職
                {CustomRoles.Arsonist, "#ff6633"},
                {CustomRoles.Jester, "#ec62a5"},
                {CustomRoles.Terrorist, "#00ff00"},
                {CustomRoles.Executioner, "#611c3a"},
                {CustomRoles.Opportunist, "#00ff00"},
                {CustomRoles.SchrodingerCat, "#696969"},
                {CustomRoles.Egoist, "#5600ff"},
                {CustomRoles.EgoSchrodingerCat, "#5600ff"},
                {CustomRoles.Jackal, "#00b4eb"},
                {CustomRoles.JSchrodingerCat, "#00b4eb"},
                //HideAndSeek
                {CustomRoles.HASFox, "#e478ff"},
                {CustomRoles.HASTroll, "#00ff00"},
                // GM
                {CustomRoles.GM, "#ff5b70"},
                //サブ役職
                {CustomRoles.LastImpostor, "#ff0000"},
                {CustomRoles.Lovers, "#ff6be4"},

                {CustomRoles.NotAssigned, "#ffffff"}
            };
        }
        catch (ArgumentException ex)
        {
            global::TownOfHost.Logger.Error("エラー:Dictionaryの値の重複を検出しました", "LoadDictionary");
            global::TownOfHost.Logger.Error(ex.Message, "LoadDictionary");
            HasArgumentException = true;
        }
        global::TownOfHost.Logger.Info($"{Application.version}", "AmongUs Version");

        global::TownOfHost.Logger.Info($"{nameof(ThisAssembly.Git.Branch)}: {ThisAssembly.Git.Branch}", "GitVersion");
        global::TownOfHost.Logger.Info($"{nameof(ThisAssembly.Git.BaseTag)}: {ThisAssembly.Git.BaseTag}", "GitVersion");
        global::TownOfHost.Logger.Info($"{nameof(ThisAssembly.Git.Commit)}: {ThisAssembly.Git.Commit}", "GitVersion");
        global::TownOfHost.Logger.Info($"{nameof(ThisAssembly.Git.Commits)}: {ThisAssembly.Git.Commits}", "GitVersion");
        global::TownOfHost.Logger.Info($"{nameof(ThisAssembly.Git.IsDirty)}: {ThisAssembly.Git.IsDirty}", "GitVersion");
        global::TownOfHost.Logger.Info($"{nameof(ThisAssembly.Git.Sha)}: {ThisAssembly.Git.Sha}", "GitVersion");
        global::TownOfHost.Logger.Info($"{nameof(ThisAssembly.Git.Tag)}: {ThisAssembly.Git.Tag}", "GitVersion");

        ClassInjector.RegisterTypeInIl2Cpp<ErrorText>();

        // Setup, order matters here

        GameOptionTab __ = DefaultTabs.GeneralTab;
        int _ = CustomRoleManager.AllRoles.Count;
        Harmony.PatchAll();
        AddonManager.ImportAddons();

        GamemodeManager.Setup();
        StaticOptions.AddStaticOptions();
        OptionManager.AllHolders.AddRange(OptionManager.Options().SelectMany(opt => opt.GetHoldersRecursive()));
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