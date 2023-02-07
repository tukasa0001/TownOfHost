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
        public static readonly string MainMenuText = "一人开发 做着玩玩  更新随缘  感谢支持";

        // ==========
        //文件路径
        public static readonly string BANNEDWORDS_FILE_PATH = "./TOH_DATA/BanWords.txt"; // File.Exists("./TOH_DATA/BanWords.txt") ? "./TOH_DATA/BanWords.txt" : "./TOH_DATA/bannedwords.txt";
        //Sorry for many Japanese comments.
        public const string PluginGuid = "com.karped1em.townofhost";
        public const string PluginVersion = "1.1.0";
        public const int PluginCreate = 5;
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

        public static int updateTime;
        public static bool newLobby = false;

        public static Dictionary<int, int> SayStartTimes = new();
        public static Dictionary<int, int> SayBanwordsTimes = new();

        /// <summary>
        /// 基本的に速度の代入は禁止.スピードは増減で対応してください.
        /// </summary>
        public static Dictionary<byte, float> AllPlayerSpeed = new();
        public const float MinSpeed = 0.0001f;
        public static Dictionary<byte, (byte, float)> BitPlayers = new();
        public static Dictionary<byte, float> WarlockTimer = new();
        public static Dictionary<byte, float> AssassinTimer = new();
        public static Dictionary<byte, PlayerControl> CursedPlayers = new();
        public static Dictionary<byte, bool> isCurseAndKill = new();
        public static Dictionary<byte, PlayerControl> MarkedPlayers = new();
        public static Dictionary<byte, int> MafiaRevenged = new();
        public static Dictionary<byte, int> GuesserGuessed = new();
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
        public static bool IsChristmas = DateTime.Now.Month == 12 && DateTime.Now.Day is 24 or 25;
        public static bool IsInitialRelease = DateTime.Now.Month == 12 && DateTime.Now.Day is 4;
        public static bool SetAutoStartToDisable = false;

        public static IEnumerable<PlayerControl> AllPlayerControls => PlayerControl.AllPlayerControls.ToArray().Where(p => p != null);
        public static IEnumerable<PlayerControl> AllAlivePlayerControls => PlayerControl.AllPlayerControls.ToArray().Where(p => p != null && p.IsAlive());

        public static Main Instance;

        //一些很新的东东

        public static Dictionary<int, byte> LastRPC = new();

        public override void Load()
        {
            Instance = this;

            //Client Options
            HideName = Config.Bind("Client Options", "Hide Game Code Name", "TOHE");
            HideColor = Config.Bind("Client Options", "Hide Game Code Color", $"{ModColor}");
            AutoStart = Config.Bind("Client Options", "AutoStart", false);
            DisableTOHE = Config.Bind("Client Options", "DisableTOHE", false);
            DebugKeyInput = Config.Bind("Authentication", "Debug Key", "kpd233");

            Logger = BepInEx.Logging.Logger.CreateLogSource("TownOfHost");
            TownOfHost.Logger.Enable();
            TownOfHost.Logger.Disable("NotifyRoles");
            TownOfHost.Logger.Disable("SendRPC");
            TownOfHost.Logger.Disable("ReceiveRPC");
            TownOfHost.Logger.Disable("SwitchSystem");
            TownOfHost.Logger.Disable("CustomRpcSender");
            if (!DebugModeManager.AmDebugger)
            {
                //TownOfHost.Logger.Disable("2018k");
                TownOfHost.Logger.Disable("SetRole");
                TownOfHost.Logger.Disable("Info.Role");
                TownOfHost.Logger.Disable("TaskState.Init");
                TownOfHost.Logger.Disable("PlayerVote");
                TownOfHost.Logger.Disable("Vote");
                TownOfHost.Logger.Disable("RpcSetNamePrivate");
                //TownOfHost.Logger.Disable("SendChat");
                TownOfHost.Logger.Disable("SetName");
                TownOfHost.Logger.Disable("AssignRoles");
                //TownOfHost.Logger.Disable("RepairSystem");
                TownOfHost.Logger.Disable("MurderPlayer");
                TownOfHost.Logger.Disable("CheckMurder");
                TownOfHost.Logger.Disable("PlayerControl.RpcSetRole");
            }
            //TownOfHost.Logger.isDetail = true;

            // 認証関連-初期化
            DebugKeyAuth = new HashAuth(DebugKeyHash, DebugKeySalt);

            // 認証関連-認証
            DebugModeManager.Auth(DebugKeyAuth, DebugKeyInput.Value);

            BitPlayers = new Dictionary<byte, (byte, float)>();
            WarlockTimer = new Dictionary<byte, float>();
            AssassinTimer = new Dictionary<byte, float>();
            CursedPlayers = new Dictionary<byte, PlayerControl>();
            MarkedPlayers = new Dictionary<byte, PlayerControl>();
            MafiaRevenged = new Dictionary<byte, int>();
            isDoused = new Dictionary<(byte, byte), bool>();
            ArsonistTimer = new Dictionary<byte, (PlayerControl, float)>();
            MayorUsedButtonCount = new Dictionary<byte, int>();
            HackerUsedCount = new Dictionary<byte, int>();
            ParaUsedButtonCount= new Dictionary<byte, int>();
            MarioVentCount = new Dictionary<byte, int>();
            MafiaRevenged = new Dictionary<byte, int>();
            GuesserGuessed = new Dictionary<byte, int>();
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
                    //インポスター、シェイプシフター
                    //特殊インポスター役職
                    //マッドメイト系役職
                        //後で追加
                    //両陣営可能役職
                    {CustomRoles.Watcher, "#800080"},
                    //特殊クルー役職
                    {CustomRoles.NiceWatcher, "#800080"}, //ウォッチャーの派生
                    {CustomRoles.Bait, "#00f7ff"},
                    {CustomRoles.Luckey, "#b8d7a3" },
                    {CustomRoles.Needy, "#a4dffe"},
                    {CustomRoles.SabotageMaster, "#0000ff"},
                    {CustomRoles.Snitch, "#b8fb4f"},
                    {CustomRoles.Mayor, "#204d42"},
                    {CustomRoles.Paranoia, "#c993f5"},
                    {CustomRoles.Psychic, "#6F698C"},
                    {CustomRoles.Sheriff, "#f8cd46"},
                    {CustomRoles.Lighter, "#eee5be"},
                    {CustomRoles.SuperStar, "#f6f657"},
                    {CustomRoles.CyberStar, "#ee4a55" },
                    {CustomRoles.Plumber,"#962d00" },
                    {CustomRoles.SpeedBooster, "#00ffff"},
                    {CustomRoles.Doctor, "#80ffdd"},
                    {CustomRoles.Trapper, "#5a8fd0"},
                    {CustomRoles.Dictator, "#df9b00"},
                    {CustomRoles.CSchrodingerCat, "#ffffff"}, //シュレディンガーの猫の派生
                    {CustomRoles.Seer, "#61b26c"},
                    {CustomRoles.Detective, "#7160e8" },
                    {CustomRoles.ChivalrousExpert, "#f0e68c"},
                    {CustomRoles.NiceGuesser, "#eede26"},
                    //第三陣営役職
                    {CustomRoles.Arsonist, "#ff6633"},
                    {CustomRoles.Jester, "#ec62a5"},
                    {CustomRoles.Terrorist, "#00ff00"},
                    {CustomRoles.Executioner, "#611c3a"},
                    {CustomRoles.God, "#f96464"},
                    {CustomRoles.Opportunist, "#00ff00"},
                    {CustomRoles.OpportunistKiller, "#51802c"},
                    {CustomRoles.Mario, "#ff6201"},
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
                    {CustomRoles.Ntr, "#00a4ff"},

                    {CustomRoles.NotAssigned, "#ffffff"},
                };
                foreach (var role in Enum.GetValues(typeof(CustomRoles)).Cast<CustomRoles>())
                {
                    switch (role.GetRoleType())
                    {
                        case RoleType.Impostor:
                            roleColors.TryAdd(role, "#ff0000");
                            break;
                        case RoleType.Madmate:
                            roleColors.TryAdd(role, "#ff0000");
                            break;
                        default:
                            break;
                    }
                }
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

            TownOfHost.Logger.Info($"{nameof(ThisAssembly.Git.Branch)}: {ThisAssembly.Git.Branch}", "GitVersion");
            TownOfHost.Logger.Info($"{nameof(ThisAssembly.Git.BaseTag)}: {ThisAssembly.Git.BaseTag}", "GitVersion");
            TownOfHost.Logger.Info($"{nameof(ThisAssembly.Git.Commit)}: {ThisAssembly.Git.Commit}", "GitVersion");
            TownOfHost.Logger.Info($"{nameof(ThisAssembly.Git.Commits)}: {ThisAssembly.Git.Commits}", "GitVersion");
            TownOfHost.Logger.Info($"{nameof(ThisAssembly.Git.IsDirty)}: {ThisAssembly.Git.IsDirty}", "GitVersion");
            TownOfHost.Logger.Info($"{nameof(ThisAssembly.Git.Sha)}: {ThisAssembly.Git.Sha}", "GitVersion");
            TownOfHost.Logger.Info($"{nameof(ThisAssembly.Git.Tag)}: {ThisAssembly.Git.Tag}", "GitVersion");

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
        EvilWatcher,
        FireWorks,
        Mafia,
        SerialKiller,
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
        Luckey,
        Needy,
        Lighter,
        SuperStar,
        CyberStar,
        Plumber,
        Mayor,
        Paranoia,
        Psychic,
        NiceWatcher,
        SabotageMaster,
        Sheriff,
        Snitch,
        SpeedBooster,
        Trapper,
        Dictator,
        Doctor,
        Seer,
        Detective,
        CSchrodingerCat,//クルー陣営のシュレディンガーの猫
        ChivalrousExpert,
        NiceGuesser,
        //Neutral
        Arsonist,
        Egoist,
        EgoSchrodingerCat,//エゴイスト陣営のシュレディンガーの猫
        Jester,
        God,
        OpportunistKiller,
        Opportunist,
        Mario,
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
        Ntr,
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
        God = CustomRoles.God,
        Mario = CustomRoles.Mario,
        HASTroll = CustomRoles.HASTroll,
    }
    public enum AdditionalWinners
    {
        None = -1,
        Opportunist = CustomRoles.Opportunist,
        OpportunistKiller = CustomRoles.OpportunistKiller,
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