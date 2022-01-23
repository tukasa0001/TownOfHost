using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using System;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnhollowerBaseLib;
using Hazel;
//herro
namespace TownOfHost
{
    [BepInPlugin(PluginGuid, "Town Of Host", PluginVersion)]
    [BepInProcess("Among Us.exe")]
    public class main : BasePlugin
    {
        //Sorry for many Japanese comments.
        public const string PluginGuid = "com.emptybottle.townofhost";
        public const string PluginVersion = "1.4";
        public const VersionTypes PluginVersionType = VersionTypes.Beta;
        public const string BetaVersion = "2";
        public const string BetaName = "Sabotage Master Beta";
        public static string VersionSuffix => PluginVersionType == VersionTypes.Beta ? "b #" + BetaVersion : "";
        public Harmony Harmony { get; } = new Harmony(PluginGuid);
        public static BepInEx.Logging.ManualLogSource Logger;
        //Lang-Config
        //これらのconfigの値がlangTextsリストに入る
        public static ConfigEntry<string> Japanese { get; private set; }
        public static ConfigEntry<string> Jester { get; private set; }
        public static ConfigEntry<string> Madmate { get; private set; }
        public static ConfigEntry<string> RoleEnabled { get; private set; }
        public static ConfigEntry<string> RoleDisabled { get; private set; }
        public static ConfigEntry<string> CommandError { get; private set; }
        public static ConfigEntry<string> InvalidArgs { get; private set; }
        public static ConfigEntry<string> roleListStart { get; private set; }
        public static ConfigEntry<string> ON { get; private set; }
        public static ConfigEntry<string> OFF { get; private set; }
        public static ConfigEntry<string> JesterInfo { get; private set; }
        public static ConfigEntry<string> MadmateInfo { get; private set; }
        public static ConfigEntry<string> Bait { get; private set; }
        public static ConfigEntry<string> BaitInfo { get; private set; }
        public static ConfigEntry<string> Terrorist { get; private set; }
        public static ConfigEntry<string> TerroristInfo { get; private set; }
        public static ConfigEntry<string> Sidekick { get; private set; }
        public static ConfigEntry<string> SidekickInfo { get; private set; }
        public static ConfigEntry<string> Vampire { get; private set; }
        public static ConfigEntry<string> VampireInfo { get; private set; }
        //Client Options
        public static ConfigEntry<bool> HideCodes {get; private set;}
        public static ConfigEntry<bool> JapaneseRoleName {get; private set;}
        //Lang-arrangement
        private static Dictionary<lang, string> JapaneseTexts = new Dictionary<lang, string>();
        private static Dictionary<RoleNames, string> JapaneseRoleNames = new Dictionary<RoleNames, string>();
        private static Dictionary<lang, string> EnglishTexts = new Dictionary<lang, string>();
        private static Dictionary<RoleNames, string> EnglishRoleNames = new Dictionary<RoleNames, string>();
        public static Dictionary<string, string> roleTexts = new Dictionary<string, string>();
        public static Dictionary<string, string> modeTexts = new Dictionary<string, string>();
        //Lang-Get
        //langのenumに対応した値をリストから持ってくる
        public static string getLang(lang lang)
        {
            var isSuccess = JapaneseTexts.TryGetValue(lang, out var text);
            return isSuccess ? text : "<Not Found:" + lang.ToString() + ">";
        }
        public static string getRoleName(RoleNames role) {
            var isSuccess = JapaneseRoleNames.TryGetValue(role, out var text);
            return isSuccess ? text : "<Not Found:" + role.ToString() + ">";
        }
        //Other Configs
        public static ConfigEntry<bool> TeruteruColor { get; private set; }
        public static ConfigEntry<bool> IgnoreWinnerCommand { get; private set; }
        public static ConfigEntry<string> WebhookURL { get; private set; }
        public static CustomWinner currentWinner;
        public static bool IsHideAndSeek;
        public static bool AllowCloseDoors;
        public static bool IgnoreVent;
        public static bool IgnoreCosmetics;
        public static int HideAndSeekKillDelay;
        public static float HideAndSeekKillDelayTimer;
        public static float HideAndSeekImpVisionMin;
        public static int FoxCount;
        public static int TrollCount;
        public static Dictionary<byte, HideAndSeekRoles> HideAndSeekRoleList;
        public static bool SyncButtonMode;
        public static int SyncedButtonCount;
        public static int UsedButtonCount;
        public static bool NoGameEnd;
        public static bool OptionControllerIsEnable;
        //タスク無効化
        public static bool DisableSwipeCard;
        public static bool DisableSubmitScan;
        public static bool DisableUnlockSafe;
        public static bool DisableUploadData;
        public static bool DisableStartReactor;
        //色がTeruteruモードとJesterモードがある
        public static Color JesterColor()
        {
            if (TeruteruColor.Value)
                return new Color(0.823f, 0.411f, 0.117f);
            else
                return new Color(0.925f, 0.384f, 0.647f);
        }
        public static Color VampireColor = new Color(0.65f, 0.34f, 0.65f);
        //これ変えたらmod名とかの色が変わる
        public static string modColor = "#00bfff";
        public static bool isFixedCooldown => currentImpostor == ImpostorRoles.Vampire;
        public static float BeforeFixCooldown = 15f;
        public static float RefixCooldownDelay = 0f;
        public static int BeforeFixMeetingCooldown = 10;
        public static string winnerList;
        public static List<string> MessagesToSend;
        public static bool isJester(PlayerControl target)
        {
            if (target.Data.Role.Role == RoleTypes.Scientist && currentScientist == ScientistRoles.Jester)
                return true;
            return false;
        }
        public static bool isMadmate(PlayerControl target)
        {
            if (target.Data.Role.Role == RoleTypes.Engineer && currentEngineer == EngineerRoles.Madmate)
                return true;
            return false;
        }
        public static bool isBait(PlayerControl target)
        {
            if (target.Data.Role.Role == RoleTypes.Scientist && currentScientist == ScientistRoles.Bait)
                return true;
            return false;
        }
        public static bool isTerrorist(PlayerControl target)
        {
            if (target.Data.Role.Role == RoleTypes.Engineer && currentEngineer == EngineerRoles.Terrorist)
                return true;
            return false;
        }
        public static bool isSidekick(PlayerControl target)
        {
            if (target.Data.Role.Role == RoleTypes.Shapeshifter && currentShapeshifter == ShapeshifterRoles.Sidekick)
                return true;
            return false;
        }
        public static bool isVampire(PlayerControl target)
        {
            if (target.Data.Role.Role == RoleTypes.Impostor && currentImpostor == ImpostorRoles.Vampire)
                return true;
            return false;
        }
        public static bool isSabotageMaster(PlayerControl target)
        {
            if (target.Data.Role.Role == RoleTypes.Scientist && currentScientist == ScientistRoles.SabotageMaster)
                return true;
            return false;
        }

        public static void ToggleRole(ScientistRoles role)
        {
            currentScientist = role == currentScientist ? ScientistRoles.Default : role;
        }
        public static void ToggleRole(EngineerRoles role)
        {
            currentEngineer = role == currentEngineer ? EngineerRoles.Default : role;
        }
        public static void ToggleRole(ShapeshifterRoles role)
        {
            currentShapeshifter = role == currentShapeshifter ? ShapeshifterRoles.Default : role;
        }
        public static void ToggleRole(ImpostorRoles role)
        {
            currentImpostor = role == currentImpostor ? ImpostorRoles.Default : role;
        }

        public static (string, Color) GetRoleText(RoleTypes role)
        {
            string RoleText = "Invalid";
            Color TextColor = Color.red;
            switch (role)
            {
                case RoleTypes.Crewmate:
                    RoleText = "Crewmate";
                    TextColor = Color.white;
                    break;
                case RoleTypes.Scientist:
                    switch (currentScientist)
                    {
                        case ScientistRoles.Default:
                            RoleText = "Scientist";
                            TextColor = Palette.CrewmateBlue;
                            break;
                        case ScientistRoles.Jester:
                            RoleText = "Jester";
                            TextColor = JesterColor();
                            break;
                        case ScientistRoles.Bait:
                            RoleText = "Bait";
                            TextColor = Color.cyan;
                            break;
                        case ScientistRoles.SabotageMaster:
                            RoleText = "Sabotage Master";
                            TextColor = Color.blue;
                            break;
                        default:
                            RoleText = "Invalid Scientist";
                            TextColor = Color.red;
                            break;
                    }
                    break;
                case RoleTypes.Engineer:
                    switch (currentEngineer)
                    {
                        case EngineerRoles.Default:
                            RoleText = "Engineer";
                            TextColor = Palette.CrewmateBlue;
                            break;
                        case EngineerRoles.Madmate:
                            RoleText = "Madmate";
                            TextColor = Palette.ImpostorRed;
                            break;
                        case EngineerRoles.Terrorist:
                            RoleText = "Terrorist";
                            TextColor = Color.green;
                            break;
                        default:
                            RoleText = "Invalid Engineer";
                            TextColor = Color.red;
                            break;
                    }
                    break;
                case RoleTypes.Impostor:
                    switch (currentImpostor)
                    {
                        case ImpostorRoles.Default:
                            RoleText = "Impostor";
                            TextColor = Palette.ImpostorRed;
                            break;
                        case ImpostorRoles.Vampire:
                            RoleText = "Vampire";
                            TextColor = VampireColor;
                            break;
                        default:
                            RoleText = "Invalid Impostor";
                            TextColor = Color.red;
                            break;
                    }
                    break;
                case RoleTypes.Shapeshifter:
                    switch (currentShapeshifter)
                    {
                        case ShapeshifterRoles.Default:
                            RoleText = "Shapeshifter";
                            TextColor = Palette.ImpostorRed;
                            break;
                        case ShapeshifterRoles.Sidekick:
                            RoleText = "Sidekick";
                            TextColor = Palette.ImpostorRed;
                            break;
                        default:
                            RoleText = "Invalid Scientist";
                            TextColor = Color.red;
                            break;
                    }
                    break;
                case RoleTypes.GuardianAngel:
                    RoleText = "GuardianAngel";
                    TextColor = Palette.CrewmateBlue;
                    break;
            }
            return (RoleText, TextColor);
        }
        public static (string, Color) GetRoleTextHideAndSeek(RoleTypes oRole, HideAndSeekRoles hRole)
        {
            string text = "Invalid";
            Color color = Color.red;
            switch (oRole)
            {
                case RoleTypes.Impostor:
                    text = "Impostor";
                    color = Palette.ImpostorRed;
                    break;
                case RoleTypes.Shapeshifter:
                    goto case RoleTypes.Impostor;
                default:
                    switch (hRole)
                    {
                        case HideAndSeekRoles.Default:
                            text = "Crewmate";
                            color = Color.white;
                            break;
                        case HideAndSeekRoles.Fox:
                            text = "Fox";
                            color = Color.magenta;
                            break;
                        case HideAndSeekRoles.Troll:
                            text = "Troll";
                            color = Color.green;
                            break;
                    }
                    break;
            }
            return (text, color);
        }
        public static bool hasTasks(GameData.PlayerInfo p, bool ForRecompute = true)
        {
            var hasTasks = true;
            if (p.Disconnected) hasTasks = false;
            if (p.Role.Role == RoleTypes.Scientist && main.currentScientist == ScientistRoles.Jester) hasTasks = false;
            if (p.Role.Role == RoleTypes.Engineer && main.currentEngineer == EngineerRoles.Madmate) hasTasks = false;
            if (p.Role.Role == RoleTypes.Engineer && main.currentEngineer == EngineerRoles.Terrorist && ForRecompute) hasTasks = false;
            if (p.Role.TeamType == RoleTeamTypes.Impostor) hasTasks = false;
            if (main.IsHideAndSeek)
            {
                if (p.IsDead) hasTasks = false;
                var hasRole = main.HideAndSeekRoleList.TryGetValue(p.PlayerId, out var role);
                if (hasRole)
                {
                    if (role == HideAndSeekRoles.Fox ||
                    role == HideAndSeekRoles.Troll) hasTasks = false;
                }
            }
            return hasTasks;
        }
        public static string getTaskText(Il2CppSystem.Collections.Generic.List<PlayerTask> tasks)
        {
            string taskText = "";
            int CompletedTaskCount = 0;
            int AllTasksCount = 0;
            foreach (var task in tasks)
            {
                AllTasksCount++;
                if (task.IsComplete) CompletedTaskCount++;
            }
            taskText = CompletedTaskCount + "/" + AllTasksCount;
            return taskText;
        }
        public static string getOnOff(bool value) => value ? "ON" : "OFF";
        public static string TextCursor => TextCursorVisible ? "_" : "";
        public static bool TextCursorVisible;
        public static float TextCursorTimer;
        //Enabled Role
        public static ScientistRoles currentScientist;
        public static EngineerRoles currentEngineer;
        public static ImpostorRoles currentImpostor;
        public static ShapeshifterRoles currentShapeshifter;
        public static Dictionary<byte, (byte, float)> BitPlayers = new Dictionary<byte, (byte, float)>();
        public static byte ExiledJesterID;
        public static byte WonTerroristID;
        public static bool CustomWinTrigger;
        public static bool VisibleTasksCount;
        public static int VampireKillDelay = 10;
        public static bool SabotageMasterFixesDoors;
        public static SuffixModes currentSuffix;
        //SyncCustomSettingsRPC Sender
        public static void SyncCustomSettingsRPC()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, 80, Hazel.SendOption.Reliable, -1);
            writer.Write((byte)currentScientist);
            writer.Write((byte)currentEngineer);
            writer.Write((byte)currentImpostor);
            writer.Write((byte)currentShapeshifter);
            writer.Write(IsHideAndSeek);
            writer.Write(NoGameEnd);
            writer.Write(DisableSwipeCard);
            writer.Write(DisableSubmitScan);
            writer.Write(DisableUnlockSafe);
            writer.Write(DisableUploadData);
            writer.Write(DisableStartReactor);
            writer.Write(VampireKillDelay);
            writer.Write(SyncButtonMode);
            writer.Write(SyncedButtonCount);
            writer.Write(AllowCloseDoors);
            writer.Write(HideAndSeekKillDelay);
            writer.Write(FoxCount);
            writer.Write(TrollCount);
            writer.Write(IgnoreVent);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void PlaySoundRPC(byte PlayerID, Sounds sound)
        {
            if (AmongUsClient.Instance.AmHost)
                RPCProcedure.PlaySound(PlayerID, sound);
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.PlaySound, Hazel.SendOption.Reliable, -1);
            writer.Write(PlayerID);
            writer.Write((byte)sound);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void CheckTerroristWin(GameData.PlayerInfo Terrorist)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            var isAllConpleted = true;
            foreach (var task in Terrorist.Tasks)
            {
                if (!task.Complete) isAllConpleted = false;
            }
            if (isAllConpleted)
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.TerroristWin, Hazel.SendOption.Reliable, -1);
                writer.Write(Terrorist.PlayerId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPCProcedure.TerroristWin(Terrorist.PlayerId);
            }
        }
        public static void ExileAsync(PlayerControl player)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.Exiled, Hazel.SendOption.Reliable, -1);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
            player.Exiled();
        }
        public static void SendToAll(string text)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            MessagesToSend.Add(text);
        }
        public static void ApplySuffix() {
            if(!AmongUsClient.Instance.AmHost) return;
            string name = SaveManager.PlayerName;
            if(!AmongUsClient.Instance.IsGameStarted) {
                switch(currentSuffix) {
                    case SuffixModes.None:
                        break;
                    case SuffixModes.TOH:
                        name += "\r\n<color=" + modColor + ">TOH v" + PluginVersion + VersionSuffix + "</color>";
                        break;
                    case SuffixModes.Streaming:
                        name += "\r\n配信中";
                        break;
                    case SuffixModes.Recording:
                        name += "\r\n録画中";
                        break;
                }
            }
            if(name != PlayerControl.LocalPlayer.name) PlayerControl.LocalPlayer.RpcSetName(name);
        }
        public override void Load()
        {
            TextCursorTimer = 0f;
            TextCursorVisible = true;

            Japanese = Config.Bind("Info", "Japanese", "日本語");
            Jester = Config.Bind("Lang", "JesterName", "Jester");
            Madmate = Config.Bind("Lang", "MadmateName", "Madmate");
            RoleEnabled = Config.Bind("Lang", "RoleEnabled", "%1$ is Enabled.");
            RoleDisabled = Config.Bind("Lang", "RoleDisabled", "%1$ is Disabled.");
            CommandError = Config.Bind("Lang", "CommandError", "Error:%1$");
            InvalidArgs = Config.Bind("Lang", "InvalidArgs", "Invaild Arguments");
            roleListStart = Config.Bind("Lang", "roleListStart", "Role List");
            ON = Config.Bind("Lang", "ON", "ON");
            OFF = Config.Bind("Lang", "OFF", "OFF");
            JesterInfo = Config.Bind("Lang", "JesterInfo", "Get Voted Out To Win");
            MadmateInfo = Config.Bind("Lang", "MadmateInfo", "Help Impostors To Win");
            Bait = Config.Bind("Lang", "BaitName", "Bait");
            BaitInfo = Config.Bind("Lang", "BaitInfo", "Bait Your Enemies");
            Terrorist = Config.Bind("Lang", "TerroristName", "Terrorist");
            TerroristInfo = Config.Bind("Lang", "TerroristInfo", "Finish your tasks, then die");
            Sidekick = Config.Bind("Lang", "SidekickName", "Sidekick");
            SidekickInfo = Config.Bind("Lang", "SidekickInfo", "You are Sidekick");
            Vampire = Config.Bind("Lang", "VampireName", "Vampire");
            VampireInfo = Config.Bind("Lang", "VampireInfo", "Kill all crewmates with your bites");

            //Client Options
            HideCodes = Config.Bind("Client Options", "Hide Game Codes", false);
            JapaneseRoleName = Config.Bind("Client Options", "Japanese Role Name", false);

            Logger = BepInEx.Logging.Logger.CreateLogSource("TownOfHost");

            currentWinner = CustomWinner.Default;

            IsHideAndSeek = false;
            AllowCloseDoors = false;
            IgnoreVent = false;
            IgnoreCosmetics = false;
            HideAndSeekKillDelay = 30;
            HideAndSeekKillDelayTimer = 0f;
            HideAndSeekImpVisionMin = 0.25f;
            TrollCount = 0;
            FoxCount = 0;
            HideAndSeekRoleList = new Dictionary<byte, HideAndSeekRoles>();

            SyncButtonMode = false;
            SyncedButtonCount = 10;
            UsedButtonCount = 0;
            

            NoGameEnd = false;
            CustomWinTrigger = false;
            OptionControllerIsEnable = false;
            BitPlayers = new Dictionary<byte, (byte, float)>();
            winnerList = "";
            VisibleTasksCount = false;
            MessagesToSend = new List<string>();

            currentScientist = ScientistRoles.Default;
            currentEngineer = EngineerRoles.Default;
            currentImpostor = ImpostorRoles.Default;
            currentShapeshifter = ShapeshifterRoles.Default;

            DisableSwipeCard = false;
            DisableSubmitScan = false;
            DisableUnlockSafe = false;
            DisableUploadData = false;
            DisableStartReactor = false;

            VampireKillDelay = 10;

            SabotageMasterFixesDoors = false;

            currentSuffix = SuffixModes.None;

            TeruteruColor = Config.Bind("Other", "TeruteruColor", false);
            IgnoreWinnerCommand = Config.Bind("Other", "IgnoreWinnerCommand", true);
            WebhookURL = Config.Bind("Other", "WebhookURL", "none");

            JapaneseTexts = new Dictionary<lang, string>(){
                //役職解説(短)
                {lang.JesterInfo, JesterInfo.Value},
                {lang.MadmateInfo, MadmateInfo.Value},
                {lang.BaitInfo, BaitInfo.Value},
                {lang.TerroristInfo, TerroristInfo.Value},
                {lang.SidekickInfo, SidekickInfo.Value},
                {lang.VampireInfo, VampireInfo.Value},
                {lang.SabotageMasterInfo, "Fix Sabotages Faster"},
                //役職解説(長)
                {lang.JesterInfoLong, "Jester(Scientist):投票で追放されたときに単独勝利となる第三陣営の役職。追放されずにゲームが終了するか、キルされると敗北となる。"},
                {lang.MadmateInfoLong, "Madmate(Engineer):インポスター陣営に属するが、Madmateからはインポスターが誰なのかはわからない。インポスターからもMadmateが誰なのかはわからない。キルやサボタージュはできないが、ベントに入ることができる。"},
                {lang.BaitInfoLong, "Bait(Scientist):キルされたときに、自分をキルした人に強制的に自分の死体を通報させることができる。"},
                {lang.TerroristInfoLong, "Terrorist(Engineer):自身のタスクを全て完了させた状態で死亡したときに単独勝利となる第三陣営の役職。死因はキルと追放のどちらでもよい。タスクを完了させずに死亡したり、死亡しないまま試合が終了すると敗北する。"},
                {lang.SidekickInfoLong, "Sidekick(Shapeshifter):初期状態でベントやサボタージュ、変身は可能だが、キルはできない。Sidekickではないインポスターが全員死亡すると、Sidekickもキルが可能となる。"},
                {lang.VampireInfoLong, "Vampire(Impostor):キルボタンを押してから10秒経って実際にキルが発生する役職。キルをしたときのテレポートは発生しない。また、キルボタンを押してから10秒経つまでに会議が始まるとその瞬間にキルが発生する。"},
                {lang.FoxInfoLong, "Fox(HideAndSeek):Trollを除くいずれかの陣営が勝利したときに生き残っていれば追加勝利となる。"},
                {lang.TrollInfoLong, "Troll(HideAndSeek):インポスターにキルされたときに単独勝利となる。この場合、Foxが生き残っていてもFoxは追加勝利することができない"},
                //モード名
                {lang.HideAndSeek, "HideAndSeek"},
                {lang.NoGameEnd, "NoGameEnd"},
                {lang.SyncButtonMode, "ボタン回数同期モード"},
                //モード解説
                {lang.HideAndSeekInfo, "HideAndSeek:会議を開くことはできず、クルーはタスク完了、インポスターは全クルー殺害でのみ勝利することができる。サボタージュ、アドミン、カメラ、待ち伏せなどは禁止事項である。"},
                {lang.NoGameEndInfo, "NoGameEnd:勝利判定が存在しないデバッグ用のモード。ホストのSHIFT+L以外でのゲーム終了ができない。"},
                {lang.SyncButtonModeInfo, "SyncButtonMode:プレイヤー全員のボタン回数が同期されているモード。"},
                //オプション項目
                {lang.AdvancedRoleOptions, "詳細オプション"},
                {lang.VampireKillDelay, "ヴァンパイアの殺害までの時間(秒)"},
                {lang.SabotageMasterFixesDoors, "サボタージュマスターが複数のドアを直す"},
                {lang.HideAndSeekOptions, "HideAndSeekの設定"},
                {lang.AllowCloseDoors, "ドア閉めを許可"},
                {lang.HideAndSeekWaitingTime, "インポスターの待機時間"},
                {lang.IgnoreCosmetics, "装飾品を禁止"},
                {lang.IgnoreVent, "ベント使用を禁止"},
                {lang.HideAndSeekRoles, "鬼ごっこの役職"},
                {lang.SyncedButtonCount, "合計ボタン使用可能回数"},
                {lang.DisableSwipeCardTask, "カードタスクを無効化"},
                {lang.DisableSubmitScanTask, "医務室のスキャンタスクを無効化"},
                {lang.DisableUnlockSafeTask, "金庫タスクを無効化"},
                {lang.DisableUploadDataTask, "ダウンロードタスクを無効化"},
                {lang.DisableStartReactorTask, "原子炉起動タスクを無効化"},
                {lang.SuffixMode, "名前の二行目"},
                //その他
                {lang.roleEnabled, RoleEnabled.Value},
                {lang.roleDisabled, RoleDisabled.Value},
                {lang.commandError, CommandError.Value},
                {lang.InvalidArgs, InvalidArgs.Value},
                {lang.roleListStart, roleListStart.Value},
                {lang.ON, ON.Value},
                {lang.OFF, OFF.Value},
            };

            Harmony.PatchAll();
        }
    }
    //Lang-enum
    public enum lang
    {
        //役職解説(短)
        JesterInfo = 0,
        MadmateInfo,
        BaitInfo,
        TerroristInfo,
        SidekickInfo,
        VampireInfo,
        SabotageMasterInfo,
        FoxInfo,
        TrollInfo,
        //役職解説(長)
        JesterInfoLong,
        MadmateInfoLong,
        BaitInfoLong,
        TerroristInfoLong,
        SidekickInfoLong,
        VampireInfoLong,
        SabotageMasterInfoLong,
        FoxInfoLong,
        TrollInfoLong,
        //モード名
        HideAndSeek,
        SyncButtonMode,
        NoGameEnd,
        DisableTasks,
        //モード解説
        HideAndSeekInfo,
        SyncButtonModeInfo,
        NoGameEndInfo,
        //オプション項目
        AdvancedRoleOptions,
        VampireKillDelay,
        SabotageMasterFixesDoors,
        HideAndSeekOptions,
        AllowCloseDoors,
        HideAndSeekWaitingTime,
        IgnoreCosmetics,
        IgnoreVent,
        HideAndSeekRoles,
        SyncedButtonCount,
        DisableSwipeCardTask,
        DisableSubmitScanTask,
        DisableUnlockSafeTask,
        DisableUploadDataTask,
        DisableStartReactorTask,
        SuffixMode,
        //その他
        roleEnabled,
        roleDisabled,
        commandError,
        InvalidArgs,
        roleListStart,
        ON,
        OFF,
    }
    public enum RoleNames {
        Jester = 0,
        Madmate,
        Bait,
        Terrorist,
        Sidekick,
        Vampire,
        SabotageMaster,
        Fox,
        Troll
    }
    //WinData
    public enum CustomWinner
    {
        Draw = 0,
        Default,
        Jester,
        Terrorist
    }
    public enum ScientistRoles
    {
        Default = 0,
        Jester,
        Bait,
        SabotageMaster
    }
    public enum EngineerRoles
    {
        Default = 0,
        Madmate,
        Terrorist
    }
    public enum ImpostorRoles
    {
        Default = 0,
        Vampire
    }
    public enum ShapeshifterRoles
    {
        Default = 0,
        Sidekick
    }
    public enum HideAndSeekRoles : byte
    {
        Default = 0,
        Troll = 1,
        Fox = 2
    }
    public enum SuffixModes {
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
}
