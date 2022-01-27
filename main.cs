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
        public const string BetaVersion = "3";
        public const string BetaName = "Mad Guardian Beta";
        public static string VersionSuffix => PluginVersionType == VersionTypes.Beta ? "b #" + BetaVersion : "";
        public Harmony Harmony { get; } = new Harmony(PluginGuid);
        public static BepInEx.Logging.ManualLogSource Logger;
        //Client Options
        public static ConfigEntry<bool> HideCodes {get; private set;}
        public static ConfigEntry<bool> JapaneseRoleName {get; private set;}
        public static ConfigEntry<bool> AmDebugger {get; private set;}
        //Lang-arrangement
        private static Dictionary<lang, string> JapaneseTexts = new Dictionary<lang, string>();
        private static Dictionary<RoleNames, string> JapaneseRoleNames = new Dictionary<RoleNames, string>();
        private static Dictionary<lang, string> EnglishTexts = new Dictionary<lang, string>();
        private static Dictionary<RoleNames, string> EnglishRoleNames = new Dictionary<RoleNames, string>();
        //Lang-Get
        //langのenumに対応した値をリストから持ってくる
        public static string getLang(lang lang)
        {
            var dic = TranslationController.Instance.CurrentLanguage.languageID == SupportedLangs.Japanese ? JapaneseTexts : EnglishTexts;
            var isSuccess = dic.TryGetValue(lang, out var text);
            return isSuccess ? text : "<Not Found:" + lang.ToString() + ">";
        }
        public static string getRoleName(RoleNames role) {
            var dic = TranslationController.Instance.CurrentLanguage.languageID == SupportedLangs.Japanese &&
            JapaneseRoleName.Value == true ? JapaneseRoleNames : EnglishRoleNames;
            var isSuccess = dic.TryGetValue(role, out var text);
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
        public static bool isNekomata(PlayerControl target)
        {
            if (target.Data.Role.Role == RoleTypes.Engineer && currentEngineer == EngineerRoles.Nekomata)
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
        public static bool isMadGuardian(PlayerControl target)
        {
            if (target.Data.Role.Role == RoleTypes.Scientist && currentScientist == ScientistRoles.MadGuardian)
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
                        case ScientistRoles.MadGuardian:
                            RoleText = "Mad Guardian";
                            TextColor = Palette.ImpostorRed;
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
                        case EngineerRoles.Nekomata:
                            RoleText = "Nekomata";
                            TextColor = Color.magenta;
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
                            RoleText = "Invalid Shapeshifter";
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
            if (p.Role.Role == RoleTypes.Scientist && main.currentScientist == ScientistRoles.MadGuardian) hasTasks = false;
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
        public static string getTaskText(Il2CppSystem.Collections.Generic.List<GameData.TaskInfo> tasks)
        {
            string taskText = "";
            int CompletedTaskCount = 0;
            int AllTasksCount = 0;
            foreach (var task in tasks)
            {
                AllTasksCount++;
                if (task.Complete) CompletedTaskCount++;
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
        public static int SabotageMasterSkillLimit = 0;
        public static bool SabotageMasterFixesDoors;
        public static bool SabotageMasterFixesReactors;
        public static bool SabotageMasterFixesOxygens;
        public static bool SabotageMasterFixesCommunications;
        public static bool SabotageMasterFixesElectrical;
        public static int SabotageMasterUsedSkillCount;

        public static bool MadmateCanFixLightsOut;
        public static bool MadGuardianCanSeeBarrier;
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
            writer.Write(SabotageMasterSkillLimit);
            writer.Write(SabotageMasterFixesDoors);
            writer.Write(SabotageMasterFixesReactors);
            writer.Write(SabotageMasterFixesOxygens);
            writer.Write(SabotageMasterFixesCommunications);
            writer.Write(SabotageMasterFixesElectrical);
            writer.Write(SyncButtonMode);
            writer.Write(SyncedButtonCount);
            writer.Write(AllowCloseDoors);
            writer.Write(HideAndSeekKillDelay);
            writer.Write(FoxCount);
            writer.Write(TrollCount);
            writer.Write(IgnoreVent);
            writer.Write(MadmateCanFixLightsOut);
            writer.Write(MadGuardianCanSeeBarrier);
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
            if(name != PlayerControl.LocalPlayer.name && PlayerControl.LocalPlayer.CurrentOutfitType == PlayerOutfitType.Default) PlayerControl.LocalPlayer.RpcSetName(name);
        }

        public override void Load()
        {
            TextCursorTimer = 0f;
            TextCursorVisible = true;

            //Client Options
            HideCodes = Config.Bind("Client Options", "Hide Game Codes", false);
            JapaneseRoleName = Config.Bind("Client Options", "Japanese Role Name", false);

            Logger = BepInEx.Logging.Logger.CreateLogSource("TownOfHost");

            currentWinner = CustomWinner.Default;

            IsHideAndSeek = false;
            AllowCloseDoors = false;
            IgnoreVent = false;
            IgnoreCosmetics = false;
            HideAndSeekKillDelay = 10;
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

            SabotageMasterSkillLimit = 0;
            SabotageMasterFixesDoors = false;
            SabotageMasterFixesReactors = true;
            SabotageMasterFixesOxygens = true;
            SabotageMasterFixesCommunications = true;
            SabotageMasterFixesElectrical = true;

            MadmateCanFixLightsOut = false;
            MadGuardianCanSeeBarrier = false;

            currentSuffix = SuffixModes.None;

            TeruteruColor = Config.Bind("Other", "TeruteruColor", false);
            IgnoreWinnerCommand = Config.Bind("Other", "IgnoreWinnerCommand", true);
            WebhookURL = Config.Bind("Other", "WebhookURL", "none");
            AmDebugger = Config.Bind("Other", "AmDebugger", false);

            JapaneseTexts = new Dictionary<lang, string>(){
                //役職解説(短)
                {lang.JesterInfo, "投票で追放されよう"},
                {lang.MadmateInfo, "インポスターを助けよう"},
                {lang.MadGuardianInfo, "タスクを完了させ、インポスターを助けよう"},
                {lang.BaitInfo, "クルーのおとりになろう"},
                {lang.TerroristInfo, "タスクを完了させ、自爆しよう"},
                {lang.NekomataInfo, "吊られて道連れにしよう"},
                {lang.SidekickInfo, "インポスターを助けよう"},
                {lang.VampireInfo, "全員を噛んで倒そう"},
                {lang.SabotageMasterInfo, "より早くサボタージュを直そう"},
                //役職解説(長)
                {lang.JesterInfoLong, "ジェスター(科学者):投票で追放されたときに単独勝利となる第三陣営の役職。追放されずにゲームが終了するか、キルされると敗北となる。"},
                {lang.MadmateInfoLong, "狂人(エンジニア):インポスター陣営に属するが、狂人からはインポスターが誰なのかはわからない。インポスターからも狂人が誰なのかはわからない。キルやサボタージュはできないが、ベントに入ることができる。"},
                {lang.MadGuardianInfoLong, "守護狂人(科学者):インポスター陣営に属するが、守護狂人からはインポスターが誰なのかはわからない。インポスターからも守護狂人が誰なのかはわからないが、タスクを完了させるとキルされなくなる。キルやサボタージュはできず、ベントに入ることもできない。"},
                {lang.BaitInfoLong, "ベイト(科学者):キルされたときに、自分をキルした人に強制的に自分の死体を通報させることができる。"},
                {lang.TerroristInfoLong, "テロリスト(エンジニア):自身のタスクを全て完了させた状態で死亡したときに単独勝利となる第三陣営の役職。死因はキルと追放のどちらでもよい。タスクを完了させずに死亡したり、死亡しないまま試合が終了すると敗北する。"},
                {lang.NekomataInfoLong, "ネコマタ(エンジニア):クルー陣営。吊られることでランダムに一人道連れにできる。"},
                {lang.SidekickInfoLong, "相棒(シェイプシフター):初期状態でベントやサボタージュ、変身は可能だが、キルはできない。相棒ではないインポスターが全員死亡すると、相棒もキルが可能となる。"},
                {lang.VampireInfoLong, "吸血鬼(インポスター):キルボタンを押してから10秒(変更可能)経って実際にキルが発生する役職。キルをしたときのテレポートは発生しない。また、キルボタンを押してから10秒経つまでに会議が始まるとその瞬間にキルが発生する。"},
                {lang.SabotageMasterInfoLong, "サボタージュマスター(科学者):原子炉メルトダウンや酸素枯渇、MIRA HQの通信妨害は片方を修理すれば両方が直る。停電はレバーに触れると全て直る。ドアを開けるとその部屋の全てのドアが開く。(変更可能)"},
                {lang.FoxInfoLong, "狐(HideAndSeek):トロールを除くいずれかの陣営が勝利したときに生き残っていれば追加勝利となる。"},
                {lang.TrollInfoLong, "トロール(HideAndSeek):インポスターにキルされたときに単独勝利となる。この場合、狐が生き残っていても狐は追加勝利することができない。"},
                //モード名
                {lang.HideAndSeek, "HideAndSeek"},
                {lang.NoGameEnd, "NoGameEnd"},
                {lang.SyncButtonMode, "ボタン回数同期モード"},
                //モード解説
                {lang.HideAndSeekInfo, "HideAndSeek:会議を開くことはできず、クルーはタスク完了、インポスターは全クルー殺害でのみ勝利することができる。サボタージュ(変更可能)、アドミン、カメラ、待ち伏せなどは禁止事項である。"},
                {lang.NoGameEndInfo, "NoGameEnd:勝利判定が存在しないデバッグ用のモード。ホストのSHIFT+L以外でのゲーム終了ができない。"},
                {lang.SyncButtonModeInfo, "ボタン回数同期モード:プレイヤー全員のボタン回数が同期されているモード。"},
                //オプション項目
                {lang.AdvancedRoleOptions, "詳細オプション"},
                {lang.VampireKillDelay, "吸血鬼の殺害までの時間(秒)"},
                {lang.MadmateCanFixLightsOut, "狂人が停電を直すことができる"},
                {lang.MadGuardianCanSeeBarrier, "守護狂人が割れたバリアを見ることができる"},
                {lang.SabotageMasterSkillLimit, "サボタージュマスターの能力の回数制限（ドアは除く）"},
                {lang.SabotageMasterFixesDoors, "サボタージュマスターが複数のドアを直せる"},
                {lang.SabotageMasterFixesReactors, "サボタージュマスターがリアクターを同時に直せる"},
                {lang.SabotageMasterFixesOxygens, "サボタージュマスターが酸素を同時に直せる"},
                {lang.SabotageMasterFixesCommunications, "サボタージュマスターがコミュニケーションを同時に直せる（MiraHQのみ）"},
                {lang.SabotageMasterFixesElectrical, "サボタージュマスターが停電を一度に直せる"},
                {lang.HideAndSeekOptions, "HideAndSeekの設定"},
                {lang.AllowCloseDoors, "ドアを閉めることを許可"},
                {lang.HideAndSeekWaitingTime, "インポスターの待機時間(秒)"},
                {lang.IgnoreCosmetics, "装飾品を禁止する"},
                {lang.IgnoreVent, "通気口の使用を禁止する"},
                {lang.HideAndSeekRoles, "HideAndSeekの役職"},
                {lang.SyncedButtonCount, "合計ボタン使用可能回数(回)"},
                {lang.DisableTasks, "タスクを無効化する"},
                {lang.DisableSwipeCardTask, "カードタスクを無効化する"},
                {lang.DisableSubmitScanTask, "医務室のスキャンタスクを無効化する"},
                {lang.DisableUnlockSafeTask, "金庫タスクを無効化する"},
                {lang.DisableUploadDataTask, "ダウンロードタスクを無効化する"},
                {lang.DisableStartReactorTask, "原子炉起動タスクを無効化する"},
                {lang.SuffixMode, "名前の二行目"},
                //その他
                {lang.commandError, "エラー:%1$"},
                {lang.InvalidArgs, "無効な引数"},
                {lang.ON, "ON"},
                {lang.OFF, "OFF"},
            };
            EnglishTexts = new Dictionary<lang, string>(){
                //役職解説(短)
                {lang.JesterInfo, "Get Voted Out"},
                {lang.MadmateInfo, "Help Impostors"},
                {lang.MadGuardianInfo, "Finish Your Tasks And Help Impostors"},
                {lang.BaitInfo, "Bait Your Enemies"},
                {lang.TerroristInfo, "Finish all tasks, then die"},
                {lang.NekomataInfo, "Die togather"},
                {lang.SidekickInfo, "Help Impostors"},
                {lang.VampireInfo, "Kill all crewmates with your bites"},
                {lang.SabotageMasterInfo, "Fix Sabotages Faster"},
                //役職解説(長)
                {lang.JesterInfoLong, "Jester(Scientist):投票で追放されたときに単独勝利となる第三陣営の役職。追放されずにゲームが終了するか、キルされると敗北となる。"},
                {lang.MadmateInfoLong, "Madmate(Engineer):インポスター陣営に属するが、Madmateからはインポスターが誰なのかはわからない。インポスターからもMadmateが誰なのかはわからない。キルやサボタージュはできないが、ベントに入ることができる。"},
                {lang.MadGuardianInfoLong, "Mad Guardian(Scientist):インポスター陣営に属するが、Mad Guardianからはインポスターが誰なのかはわからない。インポスターからもMad Guardianが誰なのかはわからないが、タスクを完了させるとキルされなくなる。キルやサボタージュはできず、ベントに入ることもできない。"},
                {lang.BaitInfoLong, "Bait(Scientist):キルされたときに、自分をキルした人に強制的に自分の死体を通報させることができる。"},
                {lang.TerroristInfoLong, "Terrorist(Engineer):自身のタスクを全て完了させた状態で死亡したときに単独勝利となる第三陣営の役職。死因はキルと追放のどちらでもよい。タスクを完了させずに死亡したり、死亡しないまま試合が終了すると敗北する。"},
                {lang.NekomataInfoLong, "Nekomata(Engineer):クルー陣営。吊られることでランダムに一人道連れにできる。"},
                {lang.SidekickInfoLong, "Sidekick(Shapeshifter):初期状態でベントやサボタージュ、変身は可能だが、キルはできない。Sidekickではないインポスターが全員死亡すると、Sidekickもキルが可能となる。"},
                {lang.VampireInfoLong, "Vampire(Impostor):キルボタンを押してから10秒経って実際にキルが発生する役職。キルをしたときのテレポートは発生しない。また、キルボタンを押してから10秒経つまでに会議が始まるとその瞬間にキルが発生する。"},
                {lang.SabotageMasterInfoLong, "SabotageMaster(Scientist):原子炉メルトダウンや酸素枯渇、MIRA HQの通信妨害は片方を修理すれば両方が直る。停電はレバーに触れると全て直る。ドアを開けるとその部屋の全てのドアが開く。(変更可能)"},
                {lang.FoxInfoLong, "Fox(HideAndSeek):Trollを除くいずれかの陣営が勝利したときに生き残っていれば追加勝利となる。"},
                {lang.TrollInfoLong, "Troll(HideAndSeek):インポスターにキルされたときに単独勝利となる。この場合、Foxが生き残っていてもFoxは追加勝利することができない。"},
                //モード名
                {lang.HideAndSeek, "HideAndSeek"},
                {lang.NoGameEnd, "NoGameEnd"},
                {lang.SyncButtonMode, "SyncButtonMode"},
                //モード解説
                {lang.HideAndSeekInfo, "HideAndSeek:会議を開くことはできず、クルーはタスク完了、インポスターは全クルー殺害でのみ勝利することができる。サボタージュ、アドミン、カメラ、待ち伏せなどは禁止事項である。"},
                {lang.NoGameEndInfo, "NoGameEnd:勝利判定が存在しないデバッグ用のモード。ホストのSHIFT+L以外でのゲーム終了ができない。"},
                {lang.SyncButtonModeInfo, "SyncButtonMode:プレイヤー全員のボタン回数が同期されているモード。"},
                //オプション項目
                {lang.AdvancedRoleOptions, "Advanced Options"},
                {lang.VampireKillDelay, "Vampire Kill Delay(s)"},
                {lang.SabotageMasterSkillLimit, "Sabotage master fixes sabotage limit(Ignore Doors)"},
                {lang.MadmateCanFixLightsOut, "Madmate Can Fix Lights Out"},
                {lang.MadGuardianCanSeeBarrier, "Mad Guardian Can See Cracked Barrier"},
                {lang.SabotageMasterFixesDoors, "Sabotage master fixes multiple doors"},
                {lang.SabotageMasterFixesReactors, "Sabotage master fixes both reactors"},
                {lang.SabotageMasterFixesOxygens, "Sabotage master fixes both oxygens"},
                {lang.SabotageMasterFixesCommunications, "Sabotage master fixes both communications(MiraHQ only)"},
                {lang.SabotageMasterFixesElectrical, "Sabotage master fixes electrical all at once"},
                {lang.HideAndSeekOptions, "HideAndSeek Options"},
                {lang.AllowCloseDoors, "Allow Close Doors"},
                {lang.HideAndSeekWaitingTime, "Impostor waiting time"},
                {lang.IgnoreCosmetics, "Ignore Cosmetics"},
                {lang.IgnoreVent, "Ignore Using Vents"},
                {lang.HideAndSeekRoles, "HideAndSeek Roles"},
                {lang.SyncedButtonCount, "Max Button Count"},
                {lang.DisableTasks, "Disable Tasks"},
                {lang.DisableSwipeCardTask, "Disable SwipeCard Task"},
                {lang.DisableSubmitScanTask, "Disable SubmitScan Task"},
                {lang.DisableUnlockSafeTask, "Disable UnlockSafe Task"},
                {lang.DisableUploadDataTask, "Disable UploadData Task"},
                {lang.DisableStartReactorTask, "Disable StartReactor Task"},
                {lang.SuffixMode, "Suffix"},
                //その他
                {lang.commandError, "Error:%1$"},
                {lang.InvalidArgs, "Invalis Args"},
                {lang.ON, "ON"},
                {lang.OFF, "OFF"},
            };
            EnglishRoleNames = new Dictionary<RoleNames, string>(){
                {RoleNames.Jester, "Jester"},
                {RoleNames.Madmate, "Madmate"},
                {RoleNames.MadGuardian, "Mad Guardian"},
                {RoleNames.Bait, "Bait"},
                {RoleNames.Terrorist, "Terrorist"},
                {RoleNames.Nekomata, "Nekomata"},
                {RoleNames.Sidekick, "Sidekick"},
                {RoleNames.Vampire, "Vampire"},
                {RoleNames.SabotageMaster, "SabotageMaster"},
                {RoleNames.Fox, "Fox"},
                {RoleNames.Troll, "Troll"},
            };
            JapaneseRoleNames = new Dictionary<RoleNames, string>(){
                {RoleNames.Jester, "ジェスター"},
                {RoleNames.Madmate, "狂人"},
                {RoleNames.MadGuardian, "守護狂人"},
                {RoleNames.Bait, "ベイト"},
                {RoleNames.Nekomata, "ネコマタ"},
                {RoleNames.Sidekick, "相棒"},
                {RoleNames.Vampire, "吸血鬼"},
                {RoleNames.SabotageMaster, "サボタージュマスター"},
                {RoleNames.Fox, "狐"},
                {RoleNames.Troll, "トロール"},
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
        NekomataInfo,
        SidekickInfo,
        VampireInfo,
        SabotageMasterInfo,
        MadGuardianInfo,
        FoxInfo,
        TrollInfo,
        //役職解説(長)
        JesterInfoLong,
        MadmateInfoLong,
        BaitInfoLong,
        TerroristInfoLong,
        NekomataInfoLong,
        SidekickInfoLong,
        VampireInfoLong,
        SabotageMasterInfoLong,
        MadGuardianInfoLong,
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
        MadmateCanFixLightsOut,
        MadGuardianCanSeeBarrier,
        SabotageMasterFixesDoors,
        SabotageMasterSkillLimit,
        SabotageMasterFixesReactors,
        SabotageMasterFixesOxygens,
        SabotageMasterFixesCommunications,
        SabotageMasterFixesElectrical,
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
        commandError,
        InvalidArgs,
        ON,
        OFF,
    }
    public enum RoleNames {
        Jester = 0,
        Madmate,
        Bait,
        Terrorist,
        Nekomata,
        Sidekick,
        Vampire,
        SabotageMaster,
        MadGuardian,
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
        SabotageMaster,
        MadGuardian
    }
    public enum EngineerRoles
    {
        Default = 0,
        Madmate,
        Terrorist,
        Nekomata
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
