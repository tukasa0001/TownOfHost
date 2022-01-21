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
        public const string BetaVersion = "1";
        public const string BetaName = "Hide And Seek Beta";
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
        //Lang-arrangement
        private static Dictionary<lang, string> langTexts = new Dictionary<lang, string>();
        public static Dictionary<string, string> roleTexts = new Dictionary<string, string>();
        public static Dictionary<string, string> modeTexts = new Dictionary<string, string>();
        //Lang-Get
        //langのenumに対応した値をリストから持ってくる
        public static string getLang(lang lang)
        {
            var isSuccess = langTexts.TryGetValue(lang, out var text);
            return isSuccess ? text : "<Not Found:" + lang.ToString() + ">";
        }
        //Other Configs
        public static ConfigEntry<bool> TeruteruColor { get; private set; }
        public static ConfigEntry<bool> IgnoreWinnerCommand { get; private set; }
        public static ConfigEntry<string> WebhookURL { get; private set; }
        public static CustomWinner currentWinner;
        public static bool IsHideAndSeek;
        public static bool AllowCloseDoors;
        public static bool IgnoreCosmetics;
        public static int HideAndSeekKillDelay;
        public static float HideAndSeekKillDelayTimer;
        public static float HideAndSeekImpVisionMin;
        public static int FoxCount;
        public static int TrollCount;
        public static Dictionary<byte,HideAndSeekRoles> HideAndSeekRoleList;
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
            if (target.Data.Role.Role == RoleTypes.Scientist && currentScientist == ScientistRole.Jester)
                return true;
            return false;
        }
        public static bool isMadmate(PlayerControl target)
        {
            if (target.Data.Role.Role == RoleTypes.Engineer && currentEngineer == EngineerRole.Madmate)
                return true;
            return false;
        }
        public static bool isBait(PlayerControl target)
        {
            if (target.Data.Role.Role == RoleTypes.Scientist && currentScientist == ScientistRole.Bait)
                return true;
            return false;
        }
        public static bool isTerrorist(PlayerControl target)
        {
            if (target.Data.Role.Role == RoleTypes.Engineer && currentEngineer == EngineerRole.Terrorist)
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

        public static void ToggleRole(ScientistRole role)
        {
            currentScientist = role == currentScientist ? ScientistRole.Default : role;
        }
        public static void ToggleRole(EngineerRole role)
        {
            currentEngineer = role == currentEngineer ? EngineerRole.Default : role;
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
                        case ScientistRole.Default:
                            RoleText = "Scientist";
                            TextColor = Palette.CrewmateBlue;
                            break;
                        case ScientistRole.Jester:
                            RoleText = "Jester";
                            TextColor = JesterColor();
                            break;
                        case ScientistRole.Bait:
                            RoleText = "Bait";
                            TextColor = Color.cyan;
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
                        case EngineerRole.Default:
                            RoleText = "Engineer";
                            TextColor = Palette.CrewmateBlue;
                            break;
                        case EngineerRole.Madmate:
                            RoleText = "Madmate";
                            TextColor = Palette.ImpostorRed;
                            break;
                        case EngineerRole.Terrorist:
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
        public static (string, Color) GetRoleTextHideAndSeek(RoleTypes oRole, HideAndSeekRoles hRole) {
            string text = "Invalid";
            Color color = Color.red;
            switch(oRole) {
                case RoleTypes.Impostor:
                    text = "Impostor";
                    color = Palette.ImpostorRed;
                    break;
                case RoleTypes.Shapeshifter:
                    goto case RoleTypes.Impostor;
                default:
                    switch(hRole) {
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
            return (text,color);
        }
        public static bool hasTasks(GameData.PlayerInfo p)
        {
            var hasTasks = true;
            if(p.Disconnected) hasTasks = false;
            if(p.Role.Role == RoleTypes.Scientist && main.currentScientist == ScientistRole.Jester) hasTasks = false;
            if(p.Role.Role == RoleTypes.Engineer && main.currentEngineer == EngineerRole.Madmate) hasTasks = false;
            if(p.Role.Role == RoleTypes.Engineer && main.currentEngineer == EngineerRole.Terrorist) hasTasks = false;
            if(p.Role.TeamType == RoleTeamTypes.Impostor) hasTasks = false;
            if(main.IsHideAndSeek) {
                if(p.IsDead) hasTasks = false;
                var hasRole = main.HideAndSeekRoleList.TryGetValue(p.PlayerId, out var role);
                if(hasRole) {
                    if(role == HideAndSeekRoles.Fox ||
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
        public static ScientistRole currentScientist;
        public static EngineerRole currentEngineer;
        public static ImpostorRoles currentImpostor;
        public static ShapeshifterRoles currentShapeshifter;
        public static Dictionary<byte, (byte, float)> BitPlayers = new Dictionary<byte, (byte, float)>();
        public static byte ExiledJesterID;
        public static byte WonTerroristID;
        public static bool CustomWinTrigger;
        public static bool VisibleTasksCount;
        public static int VampireKillDelay = 10;
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
            writer.Write(VampireKillDelay);
            writer.Write(SyncButtonMode);
            writer.Write(SyncedButtonCount);
            writer.Write(AllowCloseDoors);
            writer.Write(HideAndSeekKillDelay);
            writer.Write(FoxCount);
            writer.Write(TrollCount);
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

            Logger = BepInEx.Logging.Logger.CreateLogSource("TownOfHost");

            currentWinner = CustomWinner.Default;

            IsHideAndSeek = false;
            AllowCloseDoors = false;
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

            currentScientist = ScientistRole.Default;
            currentEngineer = EngineerRole.Default;
            currentImpostor = ImpostorRoles.Default;
            currentShapeshifter = ShapeshifterRoles.Default;

            DisableSwipeCard = false;
            DisableSubmitScan = false;
            DisableUnlockSafe = false;
            DisableUploadData = false;

            VampireKillDelay = 10;

            TeruteruColor = Config.Bind("Other", "TeruteruColor", false);
            IgnoreWinnerCommand = Config.Bind("Other", "IgnoreWinnerCommand", true);
            WebhookURL = Config.Bind("Other", "WebhookURL", "none");

            langTexts = new Dictionary<lang, string>(){
                {lang.Jester, Jester.Value},
                {lang.Madmate, Madmate.Value},
                {lang.roleEnabled, RoleEnabled.Value},
                {lang.roleDisabled, RoleDisabled.Value},
                {lang.commandError, CommandError.Value},
                {lang.InvalidArgs, InvalidArgs.Value},
                {lang.roleListStart, roleListStart.Value},
                {lang.ON, ON.Value},
                {lang.OFF, OFF.Value},
                {lang.JesterInfo, JesterInfo.Value},
                {lang.MadmateInfo, MadmateInfo.Value},
                {lang.Bait, Bait.Value},
                {lang.BaitInfo, BaitInfo.Value},
                {lang.Terrorist, Terrorist.Value},
                {lang.TerroristInfo, TerroristInfo.Value},
                {lang.Sidekick, Sidekick.Value},
                {lang.SidekickInfo, SidekickInfo.Value},
                {lang.Vampire, Vampire.Value},
                {lang.VampireInfo, VampireInfo.Value}
            };

            roleTexts = new Dictionary<string, string>(){
                {"jester", "Jester(Scientist):投票で追放されたときに単独勝利となる第三陣営の役職。追放されずにゲームが終了するか、キルされると敗北となる。"},
                {"madmate", "Madmate(Engineer):インポスター陣営に属するが、Madmateからはインポスターが誰なのかはわからない。インポスターからもMadmateが誰なのかはわからない。キルやサボタージュはできないが、ベントに入ることができる。"},
                {"bait", "Bait(Scientist):キルされたときに、自分をキルした人に強制的に自分の死体を通報させることができる。"},
                {"terrorist", "Terrorist(Engineer):自身のタスクを全て完了させた状態で死亡したときに単独勝利となる第三陣営の役職。死因はキルと追放のどちらでもよい。タスクを完了させずに死亡したり、死亡しないまま試合が終了すると敗北する。"},
                {"sidekick", "Sidekick(Shapeshifter):初期状態でベントやサボタージュ、変身は可能だが、キルはできない。Sidekickではないインポスターが全員死亡すると、Sidekickもキルが可能となる。"},
                {"vampire", "Vampire(Impostor):キルボタンを押してから10秒経って実際にキルが発生する役職。キルをしたときのテレポートは発生しない。また、キルボタンを押してから10秒経つまでに会議が始まるとその瞬間にキルが発生する。"},
                {"fox", "Fox(HideAndSeek):Trollを除くいずれかの陣営が勝利したときに生き残っていれば追加勝利となる。"},
                {"troll", "Troll(HideAndSeek):インポスターにキルされたときに単独勝利となる。この場合、Foxが生き残っていてもFoxは追加勝利することができない"}
            };

            modeTexts = new Dictionary<string, string>(){
                {"hideandseek", "HideAndSeek:会議を開くことはできず、クルーはタスク完了、インポスターは全クルー殺害でのみ勝利することができる。サボタージュ、アドミン、カメラ、待ち伏せなどは禁止事項である。"},
                {"nogameend", "NoGameEnd:勝利判定が存在しないデバッグ用のモード。ホストのSHIFT+L以外でのゲーム終了ができない。"},
                {"syncbuttonmode", "SyncButtonMode:プレイヤー全員のボタン回数が同期されているモード。"}
            };

            Harmony.PatchAll();
        }
    }
    //Lang-enum
    public enum lang
    {
        Jester = 0,
        Madmate,
        roleEnabled,
        roleDisabled,
        commandError,
        InvalidArgs,
        roleListStart,
        ON,
        OFF,
        JesterInfo,
        MadmateInfo,
        Bait,
        BaitInfo,
        Terrorist,
        TerroristInfo,
        Sidekick,
        SidekickInfo,
        Vampire,
        VampireInfo
    }
    //WinData
    public enum CustomWinner
    {
        Draw = 0,
        Default,
        Jester,
        Terrorist
    }
    public enum ScientistRole
    {
        Default = 0,
        Jester,
        Bait
    }
    public enum EngineerRole
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
    public enum HideAndSeekRoles:byte {
        Default = 0,
        Troll = 1,
        Fox = 2
    }
    public enum VersionTypes {
        Released = 0,
        Beta = 1
    }
}
