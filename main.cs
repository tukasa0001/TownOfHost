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

[assembly: AssemblyFileVersion(TOHE.Main.PluginVersion)]
[assembly: AssemblyInformationalVersion(TOHE.Main.PluginVersion)]
namespace TOHE
{
    [BepInPlugin(PluginGuid, "TOHE", PluginVersion)]
    [BepInIncompatibility("jp.ykundesu.supernewroles")]
    [BepInProcess("Among Us.exe")]
    public class Main : BasePlugin
    {
        // == プログラム設定 / Program Config ==
        // modの名前 / Mod Name (Default: Town Of Host)
        public static readonly string ModName = "TOHE";
        // modの色 / Mod Color (Default: #00bfff)
        public static readonly string ModColor = "#ffc0cb";
        // 公開ルームを許可する / Allow Public Room (Default: true)
        public static readonly bool AllowPublicRoom = true;
        // フォークID / ForkId (Default: OriginalTOH)
        public static readonly string ForkId = "TOHE";
        // Discordボタンを表示するか / Show Discord Button (Default: true)
        public static readonly bool ShowDiscordButton = false;
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
        // 首页右上角的说明文本
        public static readonly string MainMenuText = "能做出来这个模组，就已经值啦";

        // ==========
        //文件路径
        public static readonly string BANNEDWORDS_FILE_PATH = "./TOHE_DATA/BanWords.txt";
        //Sorry for many Japanese comments.
        public const string PluginGuid = "com.karped1em.townofhostedited";
        public const string PluginVersion = "2.0.2";
        public const int PluginCreate = 2;
        public Harmony Harmony { get; } = new Harmony(PluginGuid);
        public static Version version = Version.Parse(PluginVersion);
        public static BepInEx.Logging.ManualLogSource Logger;
        public static bool hasArgumentException = false;
        public static string ExceptionMessage;
        public static bool ExceptionMessageIsShown = false;
        public static bool AlreadyShowMsgBox = false;
        public static string credentialsText;
        public static NormalGameOptionsV07 NormalOptions => GameOptionsManager.Instance.currentNormalGameOptions;
        public static HideNSeekGameOptionsV07 HideNSeekSOptions => GameOptionsManager.Instance.currentHideNSeekGameOptions;
        //Client Options
        public static ConfigEntry<string> HideName { get; private set; }
        public static ConfigEntry<string> HideColor { get; private set; }
        public static ConfigEntry<bool> AutoStart { get; private set; }
        public static ConfigEntry<bool> DisableTOHE { get; private set; }
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
        public static Dictionary<byte, PlayerState.DeathReason> AfterMeetingDeathPlayers = new();
        public static Dictionary<CustomRoles, String> roleColors;
        public static bool IsFixedCooldown => CustomRoles.Vampire.IsEnable();
        public static float RefixCooldownDelay = 0f;
        public static GameData.PlayerInfo LastVotedPlayerInfo;
        public static string LastVotedPlayer;
        public static List<byte> ResetCamPlayerList;
        public static List<byte> winnerList;
        public static List<int> clientIdList;
        public static List<(string, byte, string)> MessagesToSend;
        public static bool isChatCommand = false;
        public static List<PlayerControl> LoversPlayers = new();
        public static bool isLoversDead = true;
        public static Dictionary<byte, float> AllPlayerKillCooldown = new();

        public static Dictionary<byte, Vent> LastEnteredVent = new();
        public static Dictionary<byte, Vector2> LastEnteredVentLocation = new();

        public static Dictionary<byte, int> HackerUsedCount = new();
        public static Dictionary<byte, List<byte>> PsychicTarget = new();

        public static List<byte> CyberStarDead = new();
        public static List<byte> BoobyTrapBody = new();
        public static Dictionary<byte, byte> KillerOfBoobyTrapBody = new();
        public static Dictionary<byte, string> DetectiveNotify = new();

        public static bool DoBlockNameChange = false;
        public static int updateTime;
        public static bool newLobby = false;
        public static Dictionary<int, string> OriginalName = new();

        public static Dictionary<int, int> SayStartTimes = new();
        public static Dictionary<int, int> SayBanwordsTimes = new();

        /// <summary>
        /// 基本的に速度の代入は禁止.スピードは増減で対応してください.
        /// </summary>
        public static Dictionary<byte, float> AllPlayerSpeed = new();
        public const float MinSpeed = 0.0001f;
        public static List<byte> BrakarVoteFor = new();
        public static Dictionary<byte, (byte, float)> BitPlayers = new();
        public static Dictionary<byte, float> WarlockTimer = new();
        public static Dictionary<byte, float> AssassinTimer = new();
        public static Dictionary<byte, PlayerControl> CursedPlayers = new();
        public static Dictionary<byte, bool> isCurseAndKill = new();
        public static Dictionary<byte, PlayerControl> MarkedPlayers = new();
        public static Dictionary<byte, int> MafiaRevenged = new();
        public static Dictionary<byte, int> GuesserGuessed = new();
        public static Dictionary<byte, int> CapitalismAddTask = new();
        public static Dictionary<byte, int> CapitalismAssignTask = new();
        public static Dictionary<byte, bool> isMarkAndKill = new();
        public static Dictionary<(byte, byte), bool> isDoused = new();
        public static Dictionary<byte, (PlayerControl, float)> ArsonistTimer = new();
        /// <summary>
        /// Key: ターゲットのPlayerId, Value: パペッティアのPlayerId
        /// </summary>
        public static Dictionary<byte, byte> PuppeteerList = new();
        public static Dictionary<byte, byte> SpeedBoostTarget = new();
        public static Dictionary<byte, int> MayorUsedButtonCount = new();
        public static Dictionary<byte, int> ParaUsedButtonCount = new();
        public static Dictionary<byte, int> MarioVentCount = new();
        public static Dictionary<byte, long> VeteranInProtect = new();
        public static Dictionary<byte, long> GrenadierBlinding = new();
        public static int AliveImpostorCount;
        public static int SKMadmateNowCount;
        public static bool isCursed;
        public static bool isMarked;
        public static bool existAntiAdminer;
        public static Dictionary<byte, float> SansKillCooldown = new();
        public static Dictionary<byte, bool> CheckShapeshift = new();
        public static Dictionary<byte, byte> ShapeshiftTarget = new();
        public static Dictionary<(byte, byte), string> targetArrows = new();
        public static Dictionary<byte, Vector2> EscapeeLocation = new();
        public static bool VisibleTasksCount;
        public static string nickName = "";
        public static bool introDestroyed = false;
        public static int DiscussionTime;
        public static int VotingTime;
        public static byte currentDousingTarget;
        public static float DefaultCrewmateVision;
        public static float DefaultImpostorVision;
        public static bool IsInitialRelease = DateTime.Now.Month == 1 && DateTime.Now.Day is 17;
        public static bool SetAutoStartToDisable = false;

        public static Dictionary<byte, CustomRoles> DevRole = new();

        public static IEnumerable<PlayerControl> AllPlayerControls => PlayerControl.AllPlayerControls.ToArray().Where(p => p != null);
        public static IEnumerable<PlayerControl> AllAlivePlayerControls => PlayerControl.AllPlayerControls.ToArray().Where(p => p != null && p.IsAlive());

        public static Main Instance;

        //一些很新的东东

        public static Dictionary<int, byte> LastRPC = new();
        public static string OverrideWelcomeMsg = "";

        public override void Load()
        {
            Instance = this;

            //Client Options
            HideName = Config.Bind("Client Options", "Hide Game Code Name", "TOHE");
            HideColor = Config.Bind("Client Options", "Hide Game Code Color", $"{ModColor}");
            AutoStart = Config.Bind("Client Options", "AutoStart", false);
            DisableTOHE = Config.Bind("Client Options", "DisableTOHE", false);
            DebugKeyInput = Config.Bind("Authentication", "Debug Key", "kpd233");

            Logger = BepInEx.Logging.Logger.CreateLogSource("TOHE");
            TOHE.Logger.Enable();
            TOHE.Logger.Disable("NotifyRoles");
            TOHE.Logger.Disable("SendRPC");
            TOHE.Logger.Disable("ReceiveRPC");
            TOHE.Logger.Disable("SwitchSystem");
            TOHE.Logger.Disable("CustomRpcSender");
            if (!DebugModeManager.AmDebugger)
            {
                //TOHE.Logger.Disable("2018k");
                TOHE.Logger.Disable("SetRole");
                TOHE.Logger.Disable("Info.Role");
                TOHE.Logger.Disable("TaskState.Init");
                TOHE.Logger.Disable("Vote");
                TOHE.Logger.Disable("RpcSetNamePrivate");
                //TOHE.Logger.Disable("SendChat");
                TOHE.Logger.Disable("SetName");
                TOHE.Logger.Disable("AssignRoles");
                //TOHE.Logger.Disable("RepairSystem");
                TOHE.Logger.Disable("MurderPlayer");
                TOHE.Logger.Disable("CheckMurder");
                TOHE.Logger.Disable("PlayerControl.RpcSetRole");
            }
            //TOHE.Logger.isDetail = true;

            // 認証関連-初期化
            DebugKeyAuth = new HashAuth(DebugKeyHash, DebugKeySalt);

            // 認証関連-認証
            DebugModeManager.Auth(DebugKeyAuth, DebugKeyInput.Value);

            BrakarVoteFor = new List<byte>();
            WarlockTimer = new Dictionary<byte, float>();
            AssassinTimer = new Dictionary<byte, float>();
            CursedPlayers = new Dictionary<byte, PlayerControl>();
            MarkedPlayers = new Dictionary<byte, PlayerControl>();
            MafiaRevenged = new Dictionary<byte, int>();
            isDoused = new Dictionary<(byte, byte), bool>();
            ArsonistTimer = new Dictionary<byte, (PlayerControl, float)>();
            MayorUsedButtonCount = new Dictionary<byte, int>();
            HackerUsedCount = new Dictionary<byte, int>();
            ParaUsedButtonCount = new Dictionary<byte, int>();
            VeteranInProtect = new Dictionary<byte, long>();
            GrenadierBlinding = new Dictionary<byte, long>();
            MarioVentCount = new Dictionary<byte, int>();
            MafiaRevenged = new Dictionary<byte, int>();
            GuesserGuessed = new Dictionary<byte, int>();
            CapitalismAddTask = new Dictionary<byte, int>();
            CapitalismAssignTask = new Dictionary<byte, int>();
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

            NameColorManager.Begin();
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
                    //特殊クルー役職
                    {CustomRoles.Bait, "#00f7ff"},
                    {CustomRoles.Luckey, "#b8d7a3" },
                    {CustomRoles.Needy, "#a4dffe"},
                    {CustomRoles.SabotageMaster, "#0000ff"},
                    {CustomRoles.Snitch, "#b8fb4f"},
                    {CustomRoles.Mayor, "#204d42"},
                    {CustomRoles.Paranoia, "#c993f5"},
                    {CustomRoles.Psychic, "#6F698C"},
                    {CustomRoles.Sheriff, "#f8cd46"},
                    {CustomRoles.SuperStar, "#f6f657"},
                    {CustomRoles.CyberStar, "#ee4a55" },
                    {CustomRoles.SpeedBooster, "#00ffff"},
                    {CustomRoles.Doctor, "#80ffdd"},
                    {CustomRoles.Trapper, "#5a8fd0"},
                    {CustomRoles.Dictator, "#df9b00"},
                    {CustomRoles.Detective, "#7160e8" },
                    {CustomRoles.ChivalrousExpert, "#f0e68c"},
                    {CustomRoles.NiceGuesser, "#eede26"},
                    {CustomRoles.Transporter, "#42D1FF"},
                    {CustomRoles.TimeManager, "#6495ed"},
                    {CustomRoles.Veteran, "#a77738"},
                    {CustomRoles.Bodyguard, "#185abd"},
                    {CustomRoles.Counterfeiter, "#e0e0e0"},
                    {CustomRoles.Grenadier, "#3c4a16"},
                    //第三陣営役職
                    {CustomRoles.Arsonist, "#ff6633"},
                    {CustomRoles.Jester, "#ec62a5"},
                    {CustomRoles.Terrorist, "#00ff00"},
                    {CustomRoles.Executioner, "#611c3a"},
                    {CustomRoles.God, "#f96464"},
                    {CustomRoles.Opportunist, "#00ff00"},
                    {CustomRoles.Mario, "#ff6201"},
                    {CustomRoles.Jackal, "#00b4eb"},
                    {CustomRoles.Innocent, "#8f815e"},
                    {CustomRoles.Pelican, "#34c84b"},
                    // GM
                    {CustomRoles.GM, "#ff5b70"},
                    //サブ役職
                    {CustomRoles.NotAssigned, "#ffffff"},
                    {CustomRoles.LastImpostor, "#ff1919"},
                    {CustomRoles.Lovers, "#ff6be4"},
                    {CustomRoles.Ntr, "#00a4ff"},
                    {CustomRoles.Madmate, "#ff1919"},
                    {CustomRoles.Watcher, "#800080"},
                    {CustomRoles.Flashman, "#ff8400"},
                    {CustomRoles.Lighter, "#eee5be"},
                    {CustomRoles.Seer, "#61b26c"},
                    {CustomRoles.Brakar, "#1447af"},
                    {CustomRoles.Oblivious, "#424242"},
                    {CustomRoles.Bewilder, "#c894f5"},
                    {CustomRoles.Workhorse, "#00ffff"},
                    {CustomRoles.Fool, "#e6e7ff"},
                    {CustomRoles.Avanger, "#ffab1b"},
                    {CustomRoles.Youtuber, "#fb749b"},
                    {CustomRoles.Egoist, "#5600ff"},
                    {CustomRoles.Piper, "#a3d7a8"},
                };
                foreach (var role in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>())
                {
                    switch (role.GetRoleType())
                    {
                        case RoleType.Impostor:
                            roleColors.TryAdd(role, "#ff1919");
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (ArgumentException ex)
            {
                TOHE.Logger.Error("エラー:Dictionaryの値の重複を検出しました", "LoadDictionary");
                TOHE.Logger.Exception(ex, "LoadDictionary");
                hasArgumentException = true;
                ExceptionMessage = ex.Message;
                ExceptionMessageIsShown = false;
            }
            TOHE.Logger.Info($"{Application.version}", "AmongUs Version");

            var handler = TOHE.Logger.Handler("GitVersion");
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
    public enum CustomRoles
    {
        //Default
        Crewmate = 0,
        //Impostor(Vanilla)
        Impostor,
        Shapeshifter,
        //Impostor
        BountyHunter,
        FireWorks,
        Mafia,
        SerialKiller,
        ShapeMaster,
        EvilGuesser,
        Minimalism,
        Zombie,
        Sniper,
        Vampire,
        Witch,
        Warlock,
        Assassin,
        Hacker,
        Miner,
        Escapee,
        Mare,
        Puppeteer,
        TimeThief,
        EvilTracker,
        AntiAdminer,
        Sans,
        Bomber,
        BoobyTrap,
        Scavenger,
        Capitalism,
        //Crewmate(Vanilla)
        Engineer,
        GuardianAngel,
        Scientist,
        //Crewmate
        Bait,
        Luckey,
        Needy,
        SuperStar,
        CyberStar,
        Mayor,
        Paranoia,
        Psychic,
        SabotageMaster,
        Sheriff,
        Snitch,
        SpeedBooster,
        Trapper,
        Dictator,
        Doctor,
        Detective,
        ChivalrousExpert,
        NiceGuesser,
        Transporter,
        TimeManager,
        Veteran,
        Bodyguard,
        Counterfeiter,
        Grenadier,
        //Neutral
        Arsonist,
        Jester,
        God,
        Opportunist,
        Mario,
        Terrorist,
        Executioner,
        Jackal,
        Innocent,
        Pelican,
        //GM
        GM,
        // Sub-role after 500
        NotAssigned = 500,
        LastImpostor,
        Lovers,
        Ntr,
        Madmate,
        Watcher,
        Flashman,
        Lighter,
        Seer,
        Brakar,
        Oblivious,
        Bewilder,
        Workhorse,
        Fool,
        Avanger,
        Youtuber,
        Egoist,
        Piper,
    }
    //WinData
    public enum CustomWinner
    {
        Draw = -1,
        Default = -2,
        None = -3,
        Error = -4,
        Impostor = CustomRoles.Impostor,
        Crewmate = CustomRoles.Crewmate,
        Jester = CustomRoles.Jester,
        Terrorist = CustomRoles.Terrorist,
        Lovers = CustomRoles.Lovers,
        Executioner = CustomRoles.Executioner,
        Arsonist = CustomRoles.Arsonist,
        Jackal = CustomRoles.Jackal,
        God = CustomRoles.God,
        Mario = CustomRoles.Mario,
        Innocent = CustomRoles.Innocent,
        Pelican = CustomRoles.Pelican,
        Youtuber = CustomRoles.Youtuber,
        Egoist = CustomRoles.Egoist,
    }
    public enum AdditionalWinners
    {
        None = -1,
        Lovers = CustomRoles.Lovers,
        Opportunist = CustomRoles.Opportunist,
        Executioner = CustomRoles.Executioner,
    }
    public enum SuffixModes
    {
        None = 0,
        TOHE,
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