using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using HarmonyLib;
using UnityEngine;

[assembly: AssemblyFileVersionAttribute(TownOfHost.Main.PluginVersion)]
[assembly: AssemblyInformationalVersionAttribute(TownOfHost.Main.PluginVersion)]
namespace TownOfHost
{
    [BepInPlugin(PluginGuid, "Town Of Host", PluginVersion)]
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
        //Sorry for many Japanese comments.
        public const string PluginGuid = "com.emptybottle.townofhost";
        public const string PluginVersion = "3.0.2";
        public Harmony Harmony { get; } = new Harmony(PluginGuid);
        public static Version version = Version.Parse(PluginVersion);
        public static BepInEx.Logging.ManualLogSource Logger;
        public static bool hasArgumentException = false;
        public static string ExceptionMessage;
        public static bool ExceptionMessageIsShown = false;
        public static string credentialsText;
        //Client Options
        public static ConfigEntry<string> HideName { get; private set; }
        public static ConfigEntry<string> HideColor { get; private set; }
        public static ConfigEntry<bool> ForceJapanese { get; private set; }
        public static ConfigEntry<bool> JapaneseRoleName { get; private set; }
        public static ConfigEntry<bool> AmDebugger { get; private set; }
        public static ConfigEntry<string> ShowPopUpVersion { get; private set; }
        public static ConfigEntry<int> MessageWait { get; private set; }

        public static Dictionary<byte, PlayerVersion> playerVersion = new();
        //Preset Name Options
        public static ConfigEntry<string> Preset1 { get; private set; }
        public static ConfigEntry<string> Preset2 { get; private set; }
        public static ConfigEntry<string> Preset3 { get; private set; }
        public static ConfigEntry<string> Preset4 { get; private set; }
        public static ConfigEntry<string> Preset5 { get; private set; }
        //Other Configs
        public static ConfigEntry<bool> IgnoreWinnerCommand { get; private set; }
        public static ConfigEntry<string> WebhookURL { get; private set; }
        public static ConfigEntry<float> LastKillCooldown { get; private set; }
        public static GameOptionsData RealOptionsData;
        public static Dictionary<byte, string> AllPlayerNames;
        public static Dictionary<(byte, byte), string> LastNotifyNames;
        public static Dictionary<byte, CustomRoles> AllPlayerCustomRoles;
        public static Dictionary<byte, CustomRoles> AllPlayerCustomSubRoles;
        public static Dictionary<byte, Color32> PlayerColors = new();
        public static Dictionary<byte, PlayerState.DeathReason> AfterMeetingDeathPlayers = new();
        public static Dictionary<CustomRoles, String> roleColors;
        public static bool IsFixedCooldown => CustomRoles.Vampire.IsEnable();
        public static float RefixCooldownDelay = 0f;
        public static List<byte> ResetCamPlayerList;
        public static List<byte> winnerList;
        public static List<(string, byte)> MessagesToSend;
        public static bool isChatCommand = false;
        public static bool TextCursorVisible;
        public static float TextCursorTimer;
        public static List<PlayerControl> LoversPlayers = new();
        public static bool isLoversDead = true;
        public static Dictionary<byte, float> AllPlayerKillCooldown = new();

        /// <summary>
        /// 基本的に速度の代入は禁止.スピードは増減で対応してください.
        /// </summary>
        public static Dictionary<byte, float> AllPlayerSpeed = new();
        public const float MinSpeed = 0.0001f;
        public static Dictionary<byte, (byte, float)> BitPlayers = new();
        public static Dictionary<byte, float> WarlockTimer = new();
        public static Dictionary<byte, PlayerControl> CursedPlayers = new();
        public static List<PlayerControl> SpelledPlayer = new();
        public static Dictionary<byte, bool> KillOrSpell = new();
        public static Dictionary<byte, bool> isCurseAndKill = new();
        public static Dictionary<(byte, byte), bool> isDoused = new();
        public static Dictionary<byte, (PlayerControl, float)> ArsonistTimer = new();
        public static Dictionary<byte, float> AirshipMeetingTimer = new();
        /// <summary>
        /// Key: ターゲットのPlayerId, Value: パペッティアのPlayerId
        /// </summary>
        public static Dictionary<byte, byte> PuppeteerList = new();
        public static Dictionary<byte, byte> SpeedBoostTarget = new();
        public static Dictionary<byte, int> MayorUsedButtonCount = new();
        public static int AliveImpostorCount;
        public static int SKMadmateNowCount;
        public static bool witchMeeting;
        public static bool isCursed;
        public static Dictionary<byte, bool> CheckShapeshift = new();
        public static Dictionary<(byte, byte), string> targetArrows = new();
        public static bool CustomWinTrigger;
        public static bool VisibleTasksCount;
        public static string nickName = "";
        public static bool introDestroyed = false;
        public static int DiscussionTime;
        public static int VotingTime;
        public static byte currentDousingTarget;
        public static float DefaultCrewmateVision;
        public static float DefaultImpostorVision;

        public static Main Instance;

        public override void Load()
        {
            Instance = this;

            TextCursorTimer = 0f;
            TextCursorVisible = true;

            //Client Options
            HideName = Config.Bind("Client Options", "Hide Game Code Name", "Town Of Host");
            HideColor = Config.Bind("Client Options", "Hide Game Code Color", $"{ModColor}");
            ForceJapanese = Config.Bind("Client Options", "Force Japanese", false);
            JapaneseRoleName = Config.Bind("Client Options", "Japanese Role Name", true);
            Logger = BepInEx.Logging.Logger.CreateLogSource("TownOfHost");
            TownOfHost.Logger.Enable();
            TownOfHost.Logger.Disable("NotifyRoles");
            TownOfHost.Logger.Disable("SendRPC");
            TownOfHost.Logger.Disable("ReceiveRPC");
            TownOfHost.Logger.Disable("SwitchSystem");
            //TownOfHost.Logger.isDetail = true;

            AllPlayerCustomRoles = new Dictionary<byte, CustomRoles>();
            AllPlayerCustomSubRoles = new Dictionary<byte, CustomRoles>();
            CustomWinTrigger = false;
            BitPlayers = new Dictionary<byte, (byte, float)>();
            WarlockTimer = new Dictionary<byte, float>();
            CursedPlayers = new Dictionary<byte, PlayerControl>();
            SpelledPlayer = new List<PlayerControl>();
            isDoused = new Dictionary<(byte, byte), bool>();
            ArsonistTimer = new Dictionary<byte, (PlayerControl, float)>();
            MayorUsedButtonCount = new Dictionary<byte, int>();
            winnerList = new();
            VisibleTasksCount = false;
            MessagesToSend = new List<(string, byte)>();
            currentDousingTarget = 255;

            Preset1 = Config.Bind("Preset Name Options", "Preset1", "Preset_1");
            Preset2 = Config.Bind("Preset Name Options", "Preset2", "Preset_2");
            Preset3 = Config.Bind("Preset Name Options", "Preset3", "Preset_3");
            Preset4 = Config.Bind("Preset Name Options", "Preset4", "Preset_4");
            Preset5 = Config.Bind("Preset Name Options", "Preset5", "Preset_5");
            IgnoreWinnerCommand = Config.Bind("Other", "IgnoreWinnerCommand", true);
            WebhookURL = Config.Bind("Other", "WebhookURL", "none");
            AmDebugger = Config.Bind("Other", "AmDebugger", false);
            ShowPopUpVersion = Config.Bind("Other", "ShowPopUpVersion", "0");
            MessageWait = Config.Bind("Other", "MessageWait", 1);
            LastKillCooldown = Config.Bind("Other", "LastKillCooldown", (float)30);

            NameColorManager.Begin();
            CustomWinnerHolder.Reset();
            Translator.Init();

            hasArgumentException = false;
            ExceptionMessage = "";
            try
            {

                roleColors = new Dictionary<CustomRoles, string>()
                {
                    //バニラ役職
                    {CustomRoles.Crewmate, "#ffffff"},
                    {CustomRoles.Engineer, "#b6f0ff"},
                    {CustomRoles.Scientist, "#b6f0ff"},
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
                    {CustomRoles.NoSubRoleAssigned, "#ffffff"},
                    {CustomRoles.Lovers, "#ffaaaa"}
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
                TownOfHost.Logger.Error(ex.Message, "LoadDictionary");
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

            if (!File.Exists("template.txt"))
            {
                TownOfHost.Logger.Info("Among Us.exeと同じフォルダにtemplate.txtが見つかりませんでした。新規作成します。", "Template");
                try
                {
                    File.WriteAllText(@"template.txt", "test:This is template text.\\nLine breaks are also possible.\ntest:これは定型文です。\\n改行も可能です。");
                }
                catch (Exception ex)
                {
                    TownOfHost.Logger.Error(ex.ToString(), "Template");
                }
            }

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
        //ShapeMaster,
        Sniper,
        Vampire,
        Witch,
        Warlock,
        Mare,
        Puppeteer,
        TimeThief,
        EvilTracker,
        LastImpostor,
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
        NoSubRoleAssigned = 500,
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
}