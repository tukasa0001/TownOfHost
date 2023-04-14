using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AmongUs.GameOptions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using HarmonyLib;
using UnhollowerRuntimeLib;
using UnityEngine;

using TownOfHost.Roles.Core;

[assembly: AssemblyFileVersionAttribute(TownOfHost.Main.PluginVersion)]
[assembly: AssemblyInformationalVersionAttribute(TownOfHost.Main.PluginVersion)]
namespace TownOfHost
{
    [BepInPlugin(PluginGuid, "Town Of Host", PluginVersion)]
    [BepInIncompatibility("jp.ykundesu.supernewroles")]
    [BepInProcess("Among Us.exe")]
    public class Main : BasePlugin
    {
        // == プログラム設定 / Program Config ==
        // modの名前 / Mod Name (Default: Town Of Host)
        public static readonly string ModName = "Town Of Host";
        // modの色 / Mod Color (Default: #00bfff)
        public static readonly string ModColor = "#00bfff";
        // 公開ルームを許可する / Allow Public Room (Default: true)
        public static readonly bool AllowPublicRoom = true;
        // フォークID / ForkId (Default: OriginalTOH)
        public static readonly string ForkId = "OriginalTOH";
        // Discordボタンを表示するか / Show Discord Button (Default: true)
        public static readonly bool ShowDiscordButton = true;
        // Discordサーバーの招待リンク / Discord Server Invite URL (Default: https://discord.gg/W5ug6hXB9V)
        public static readonly string DiscordInviteUrl = "https://discord.gg/W5ug6hXB9V";
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

        // ==========
        //Sorry for many Japanese comments.
        public const string PluginGuid = "com.emptybottle.townofhost";
        public const string PluginVersion = "4.1.2";
        public Harmony Harmony { get; } = new Harmony(PluginGuid);
        public static Version version = Version.Parse(PluginVersion);
        public static BepInEx.Logging.ManualLogSource Logger;
        public static bool hasArgumentException = false;
        public static string ExceptionMessage;
        public static bool ExceptionMessageIsShown = false;
        public static string credentialsText;
        public static NormalGameOptionsV07 NormalOptions => GameOptionsManager.Instance.currentNormalGameOptions;
        public static HideNSeekGameOptionsV07 HideNSeekSOptions => GameOptionsManager.Instance.currentHideNSeekGameOptions;
        //Client Options
        public static ConfigEntry<string> HideName { get; private set; }
        public static ConfigEntry<string> HideColor { get; private set; }
        public static ConfigEntry<bool> ForceJapanese { get; private set; }
        public static ConfigEntry<bool> JapaneseRoleName { get; private set; }
        public static ConfigEntry<int> MessageWait { get; private set; }

        public static Dictionary<byte, PlayerVersion> playerVersion = new();
        //Preset Name Options
        public static ConfigEntry<string> Preset1 { get; private set; }
        public static ConfigEntry<string> Preset2 { get; private set; }
        public static ConfigEntry<string> Preset3 { get; private set; }
        public static ConfigEntry<string> Preset4 { get; private set; }
        public static ConfigEntry<string> Preset5 { get; private set; }
        //Other Configs
        public static ConfigEntry<string> WebhookURL { get; private set; }
        public static ConfigEntry<string> BetaBuildURL { get; private set; }
        public static ConfigEntry<float> LastKillCooldown { get; private set; }
        public static ConfigEntry<float> LastShapeshifterCooldown { get; private set; }
        public static OptionBackupData RealOptionsData;
        public static Dictionary<byte, PlayerState> PlayerStates = new();
        public static Dictionary<byte, string> AllPlayerNames;
        public static Dictionary<(byte, byte), string> LastNotifyNames;
        public static Dictionary<byte, Color32> PlayerColors = new();
        public static Dictionary<byte, CustomDeathReason> AfterMeetingDeathPlayers = new();
        public static Dictionary<CustomRoles, String> roleColors;
        public static List<byte> ResetCamPlayerList;
        public static List<byte> winnerList;
        public static List<int> clientIdList;
        public static List<(string, byte, string)> MessagesToSend;
        public static bool isChatCommand = false;
        public static List<PlayerControl> LoversPlayers = new();
        public static bool isLoversDead = true;
        public static Dictionary<byte, float> AllPlayerKillCooldown = new();

        /// <summary>
        /// 基本的に速度の代入は禁止.スピードは増減で対応してください.
        /// </summary>
        public static Dictionary<byte, float> AllPlayerSpeed = new();
        public const float MinSpeed = 0.0001f;
        public static Dictionary<(byte, byte), bool> isDoused = new();
        public static Dictionary<byte, (PlayerControl, float)> ArsonistTimer = new();
        public static int AliveImpostorCount;
        public static int SKMadmateNowCount;
        public static Dictionary<byte, bool> CheckShapeshift = new();
        public static Dictionary<byte, byte> ShapeshiftTarget = new();
        public static bool VisibleTasksCount;
        public static string nickName = "";
        public static bool introDestroyed = false;
        public static byte currentDousingTarget;
        public static float DefaultCrewmateVision;
        public static float DefaultImpostorVision;
        public static bool IsChristmas = DateTime.Now.Month == 12 && DateTime.Now.Day is 24 or 25;
        public static bool IsInitialRelease = DateTime.Now.Month == 12 && DateTime.Now.Day is 4;

        public static IEnumerable<PlayerControl> AllPlayerControls => PlayerControl.AllPlayerControls.ToArray().Where(p => p != null);
        public static IEnumerable<PlayerControl> AllAlivePlayerControls => PlayerControl.AllPlayerControls.ToArray().Where(p => p != null && p.IsAlive());

        public static Main Instance;

        public override void Load()
        {
            Instance = this;

            //Client Options
            HideName = Config.Bind("Client Options", "Hide Game Code Name", "Town Of Host");
            HideColor = Config.Bind("Client Options", "Hide Game Code Color", $"{ModColor}");
            ForceJapanese = Config.Bind("Client Options", "Force Japanese", false);
            JapaneseRoleName = Config.Bind("Client Options", "Japanese Role Name", true);
            DebugKeyInput = Config.Bind("Authentication", "Debug Key", "");

            Logger = BepInEx.Logging.Logger.CreateLogSource("TownOfHost");
            TownOfHost.Logger.Enable();
            TownOfHost.Logger.Disable("NotifyRoles");
            TownOfHost.Logger.Disable("SendRPC");
            TownOfHost.Logger.Disable("ReceiveRPC");
            TownOfHost.Logger.Disable("SwitchSystem");
            TownOfHost.Logger.Disable("CustomRpcSender");
            //TownOfHost.Logger.isDetail = true;

            // 認証関連-初期化
            DebugKeyAuth = new HashAuth(DebugKeyHash, DebugKeySalt);

            // 認証関連-認証
            DebugModeManager.Auth(DebugKeyAuth, DebugKeyInput.Value);

            isDoused = new Dictionary<(byte, byte), bool>();
            ArsonistTimer = new Dictionary<byte, (PlayerControl, float)>();
            winnerList = new();
            VisibleTasksCount = false;
            MessagesToSend = new List<(string, byte, string)>();
            currentDousingTarget = 255;

            Preset1 = Config.Bind("Preset Name Options", "Preset1", "Preset_1");
            Preset2 = Config.Bind("Preset Name Options", "Preset2", "Preset_2");
            Preset3 = Config.Bind("Preset Name Options", "Preset3", "Preset_3");
            Preset4 = Config.Bind("Preset Name Options", "Preset4", "Preset_4");
            Preset5 = Config.Bind("Preset Name Options", "Preset5", "Preset_5");
            WebhookURL = Config.Bind("Other", "WebhookURL", "none");
            BetaBuildURL = Config.Bind("Other", "BetaBuildURL", "");
            MessageWait = Config.Bind("Other", "MessageWait", 1);
            LastKillCooldown = Config.Bind("Other", "LastKillCooldown", (float)30);
            LastShapeshifterCooldown = Config.Bind("Other", "LastShapeshifterCooldown", (float)30);

            CustomWinnerHolder.Reset();
            Translator.Init();
            BanManager.Init();
            TemplateManager.Init();

            IRandom.SetInstance(new NetRandomWrapper());

            hasArgumentException = false;
            ExceptionMessage = "";
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
                    {CustomRoles.SabotageMaster, "#0000ff"},
                    {CustomRoles.Snitch, "#b8fb4f"},
                    {CustomRoles.Dictator, "#df9b00"},
                    {CustomRoles.CSchrodingerCat, "#ffffff"}, //シュレディンガーの猫の派生
                    {CustomRoles.TimeManager, "#6495ed"},
                    //ニュートラル役職
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
                    {CustomRoles.LastImpostor, "#ff1919"},
                    {CustomRoles.Lovers, "#ff6be4"},
                    {CustomRoles.Workhorse, "#00ffff"},

                    {CustomRoles.NotAssigned, "#ffffff"}
                };
                foreach (var role in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>())
                {
                    switch (role.GetCustomRoleTypes())
                    {
                        case CustomRoleTypes.Impostor:
                        case CustomRoleTypes.Madmate:
                            roleColors.TryAdd(role, "#ff1919");
                            break;
                        default:
                            break;
                    }
                }
                var type = typeof(RoleBase);
                var roleClassArray =
                CustomRoleManager.AllRolesClassType = Assembly.GetAssembly(type)
                    .GetTypes()
                    .Where(x => x.IsSubclassOf(type)).ToArray();

                foreach (var roleClassType in roleClassArray)
                    roleClassType.GetField("RoleInfo")?.GetValue(type);
            }
            catch (ArgumentException ex)
            {
                TownOfHost.Logger.Error("エラー:Dictionaryの値の重複を検出しました", "LoadDictionary");
                TownOfHost.Logger.Exception(ex, "LoadDictionary");
                hasArgumentException = true;
                ExceptionMessage = ex.Message;
                ExceptionMessageIsShown = false;
            }
            TownOfHost.Logger.Info($"{Application.version}", "AmongUs Version");

            var handler = TownOfHost.Logger.Handler("GitVersion");
            handler.Info($"{nameof(ThisAssembly.Git.Branch)}: {ThisAssembly.Git.Branch}");
            handler.Info($"{nameof(ThisAssembly.Git.BaseTag)}: {ThisAssembly.Git.BaseTag}");
            handler.Info($"{nameof(ThisAssembly.Git.Commit)}: {ThisAssembly.Git.Commit}");
            handler.Info($"{nameof(ThisAssembly.Git.Commits)}: {ThisAssembly.Git.Commits}");
            handler.Info($"{nameof(ThisAssembly.Git.IsDirty)}: {ThisAssembly.Git.IsDirty}");
            handler.Info($"{nameof(ThisAssembly.Git.Sha)}: {ThisAssembly.Git.Sha}");
            handler.Info($"{nameof(ThisAssembly.Git.Tag)}: {ThisAssembly.Git.Tag}");

            ClassInjector.RegisterTypeInIl2Cpp<ErrorText>();

            Harmony.PatchAll();
        }
    }
    public enum CustomDeathReason
    {
        Kill,
        Vote,
        Suicide,
        Spell,
        FollowingSuicide,
        Bite,
        Bombed,
        Misfire,
        Torched,
        Sniped,
        Revenge,
        Execution,
        Disconnected,
        Fall,
        etc = -1
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
}
