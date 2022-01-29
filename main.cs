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
using System.Linq;
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
        private static Dictionary<CustomRoles, string> JapaneseRoleNames = new Dictionary<CustomRoles, string>();
        private static Dictionary<lang, string> EnglishTexts = new Dictionary<lang, string>();
        private static Dictionary<CustomRoles, string> EnglishRoleNames = new Dictionary<CustomRoles, string>();
        //Lang-Get
        //langのenumに対応した値をリストから持ってくる
        public static string getLang(lang lang)
        {
            var dic = TranslationController.Instance.CurrentLanguage.languageID == SupportedLangs.Japanese ? JapaneseTexts : EnglishTexts;
            var isSuccess = dic.TryGetValue(lang, out var text);
            return isSuccess ? text : "<Not Found:" + lang.ToString() + ">";
        }
        public static string getRoleName(CustomRoles role) {
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
        public static Color MayorColor = new Color(1f, 0f, 1f);
        public static Color VampireColor = new Color(0.65f, 0.34f, 0.65f);
        //これ変えたらmod名とかの色が変わる
        public static string modColor = "#00bfff";
        public static bool isFixedCooldown => EnabledCustomRoles.Contains(CustomRoles.Vampire);
        public static float BeforeFixCooldown = 15f;
        public static float RefixCooldownDelay = 0f;
        public static int BeforeFixMeetingCooldown = 10;
        public static string winnerList;
        public static List<string> MessagesToSend;
        public static bool isJester(PlayerControl target)
        {
            if (target.getCustomRole() == CustomRoles.Jester)
                return true;
            return false;
        }
        public static bool isMadmate(PlayerControl target)
        {
            if (target.getCustomRole() == CustomRoles.Madmate)
                return true;
            return false;
        }
        public static bool isBait(PlayerControl target)
        {
            if (target.getCustomRole() == CustomRoles.Bait)
                return true;
            return false;
        }
        public static bool isTerrorist(PlayerControl target)
        {
            if (target.getCustomRole() == CustomRoles.Terrorist)
                return true;
            return false;
        }
        public static bool isSidekick(PlayerControl target)
        {
            if (target.getCustomRole() == CustomRoles.Sidekick)
                return true;
            return false;
        }
        public static bool isVampire(PlayerControl target)
        {
            if (target.getCustomRole() == CustomRoles.Vampire)
                return true;
            return false;
        }
        public static bool isSabotageMaster(PlayerControl target)
        {
            if (target.getCustomRole() == CustomRoles.SabotageMaster)
                return true;
            return false;
        }
        public static bool isMadGuardian(PlayerControl target)
        {
            if (target.getCustomRole() == CustomRoles.MadGuardian)
                return true;
            return false;
        }
        public static bool isMayor(PlayerControl target)
        {
            if (target.getCustomRole() == CustomRoles.Mayor)
                return true;
            return false;
        }
        public static bool isOpportunist(PlayerControl target)
        {
            if (target.getCustomRole() == CustomRoles.Opportunist)
                return true;
            return false;
        }

        public static void ToggleRole(CustomRoles role)
        {
            if(EnabledCustomRoles.Contains(role))
                EnabledCustomRoles.Remove(role);
            else EnabledCustomRoles.Add(role);
        }

        public static (string, Color) GetRoleText(PlayerControl player)
        {
            string RoleText = "Invalid Role";
            Color TextColor = Color.red;

            var cRole = player.getCustomRole();
            switch (cRole) {
                case CustomRoles.Default:
                    RoleText = "Invalid Vanilla Role";
                    switch(player.Data.Role.Role) {
                        case RoleTypes.Crewmate:
                            RoleText = "Crewmate";
                            TextColor = Color.white;
                            break;
                        case RoleTypes.Scientist:
                            RoleText = "Scientist";
                            TextColor = Palette.CrewmateBlue;
                            break;
                        case RoleTypes.Engineer:
                            RoleText = "Engineer";
                            TextColor = Palette.CrewmateBlue;
                            break;
                        case RoleTypes.GuardianAngel:
                            RoleText = "Guardian Angel";
                            TextColor = Palette.CrewmateBlue;
                            break;
                        case RoleTypes.Impostor:
                            RoleText = "Impostor";
                            TextColor = Palette.ImpostorRed;
                            break;
                        case RoleTypes.Shapeshifter:
                            RoleText = "Shapeshifter";
                            TextColor = Palette.ImpostorRed;
                            break;
                    }
                    break;
                case CustomRoles.Jester:
                    RoleText = "Jester";
                    TextColor = JesterColor();
                    break;
                case CustomRoles.Madmate:
                    RoleText = "Madmate";
                    TextColor = Palette.ImpostorRed;
                    break;
                case CustomRoles.MadGuardian:
                    RoleText = "Mad Guardian";
                    TextColor = Palette.ImpostorRed;
                    break;
                case CustomRoles.Mayor:
                    RoleText = "Mayor";
                    TextColor = MayorColor;
                    break;
                case CustomRoles.Opportunist:
                    RoleText = "Opportunist";
                    TextColor = Color.green;
                    break;
                case CustomRoles.SabotageMaster:
                    RoleText = "Sabotage Master";
                    TextColor = Color.blue;
                    break;
                case CustomRoles.Terrorist:
                    RoleText = "Terrorist";
                    TextColor = Color.green;
                    break;
                case CustomRoles.Bait:
                    RoleText = "Bait";
                    TextColor = Color.cyan;
                    break;
                case CustomRoles.Vampire:
                    RoleText = "Vampire";
                    TextColor = VampireColor;
                    break;
                case CustomRoles.Sidekick:
                    RoleText = "Sidekick";
                    TextColor = Palette.ImpostorRed;
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
            if (main.IsHideAndSeek)
            {
                if (p.IsDead) hasTasks = false;
                var hasRole = main.HideAndSeekRoleList.TryGetValue(p.PlayerId, out var role);
                if (hasRole)
                {
                    if (role == HideAndSeekRoles.Fox ||
                    role == HideAndSeekRoles.Troll) hasTasks = false;
                }
            } else {
                if (p.Role.TeamType == RoleTeamTypes.Impostor) hasTasks = false;

                var cRoleFound = AllPlayerCustomRoles.TryGetValue(p.PlayerId, out var cRole);
                if(cRoleFound) {
                    if (cRole == CustomRoles.Jester) hasTasks = false;
                    if (cRole == CustomRoles.MadGuardian && ForRecompute) hasTasks = false;
                    if (cRole == CustomRoles.Opportunist) hasTasks = false;
                    if (cRole == CustomRoles.Madmate) hasTasks = false;
                    if (cRole == CustomRoles.Terrorist && ForRecompute) hasTasks = false;
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
        public static List<CustomRoles> EnabledCustomRoles;
        public static Dictionary<byte, CustomRoles> AllPlayerCustomRoles;
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
        public static int MayorAdditionalVote;

        public static bool MadmateCanFixLightsOut;
        public static bool MadGuardianCanSeeBarrier;
        public static SuffixModes currentSuffix;
        //SyncCustomSettingsRPC Sender
        public static void SyncCustomSettingsRPC()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, 80, Hazel.SendOption.Reliable, -1);
            //有効な役職の送信
            var EnabledCustomRolesIds = new List<byte>();
            EnabledCustomRoles.ForEach(role => EnabledCustomRolesIds.Add((byte)role));
            writer.WriteBytesAndSize(EnabledCustomRolesIds.ToArray());
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
            writer.Write(MayorAdditionalVote);
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
        public static PlayerControl getPlayerById(int PlayerId) {
            var player = PlayerControl.AllPlayerControls.ToArray().Where(pc => pc.PlayerId == PlayerId).FirstOrDefault();
            return player;
        }
        public static CustomRoles getCustomRole(byte PlayerId) {
            var cRoleFound = main.AllPlayerCustomRoles.TryGetValue(PlayerId, out var cRole);
            if(cRoleFound) return cRole;
            else return CustomRoles.Default;
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

            EnabledCustomRoles = new List<CustomRoles>();
            AllPlayerCustomRoles = new Dictionary<byte, CustomRoles>();

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

            MayorAdditionalVote = 1;

            currentSuffix = SuffixModes.None;

            TeruteruColor = Config.Bind("Other", "TeruteruColor", false);
            IgnoreWinnerCommand = Config.Bind("Other", "IgnoreWinnerCommand", true);
            WebhookURL = Config.Bind("Other", "WebhookURL", "none");
            AmDebugger = Config.Bind("Other", "AmDebugger", false);

            JapaneseTexts = new Dictionary<lang, string>(){
                //役職解説(短)
                {lang.JesterInfo, "追放されて勝利しろ"},
                {lang.MadmateInfo, "インポスターを助けろ"},
                {lang.MadGuardianInfo, "タスクを完了させ、インポスターを助けろ"},
                {lang.BaitInfo, "クルーのおとりになれ"},
                {lang.TerroristInfo, "タスクを完了させ、自爆しろ"},
                {lang.SidekickInfo, "インポスターの後継者となれ"},
                {lang.BeforeSidekickInfo,"今はキルをすることができない"},
                {lang.AfterSidekickInfo,"クルーメイトに復讐をしろ"},
                {lang.VampireInfo, "全員を噛んで倒せ"},
                {lang.SabotageMasterInfo, "より早くサボタージュを直せ"},
                {lang.MayorInfo, "二回投票できる"},
                {lang.OpportunistInfo, "とにかく生き残れ"},
                //役職解説(長)
                {lang.JesterInfoLong, "ジェスター(科学者):会議で追放されたときに単独勝利となる第三陣営の役職。追放されずにゲームが終了するか、キルされると敗北となる。"},
                {lang.MadmateInfoLong, "狂人(エンジニア):インポスター陣営に属するが、インポスターが誰なのかはわからない。インポスターからも狂人が誰なのかはわからない。キルやサボタージュは使えないが、通気口を使うことができる。"},
                {lang.MadGuardianInfoLong, "守護狂人(科学者):インポスター陣営に属するが、インポスターが誰なのかはわからない。インポスターからも守護狂人が誰なのかはわからないが、タスクを完了させるとキルされなくなる。キルやサボタージュ、通気口は使えない。"},
                {lang.BaitInfoLong, "ベイト(科学者):キルされたときに、自分をキルした人に強制的に自分の死体を通報させることができる。"},
                {lang.TerroristInfoLong, "テロリスト(エンジニア):自身のタスクを全て完了させた状態で死亡したときに単独勝利となる第三陣営の役職。死因はキルと追放のどちらでもよい。タスクを完了させずに死亡したり、死亡しないまま試合が終了すると敗北する。"},
                {lang.SidekickInfoLong, "相棒(シェイプシフター):初期状態でベントやサボタージュ、変身は可能だが、キルはできない。相棒ではないインポスターが全員死亡すると、相棒もキルが可能となる。"},
                {lang.VampireInfoLong, "吸血鬼(インポスター):キルボタンを押してから一定秒数経って実際にキルが発生する役職。キルをしたときのテレポートは発生せず、キルボタンを押してから設定された秒数が経つまでに会議が始まるとその瞬間にキルが発生する。(設定有)"},
                {lang.SabotageMasterInfoLong, "サボタージュマスター(科学者):原子炉メルトダウンや酸素妨害、MIRA HQの通信妨害は片方を修理すれば両方が直る。停電は1箇所のレバーに触れると全て直る。ドアを開けるとその部屋の全てのドアが開く。(設定有)"},
                {lang.MayorInfoLong, "メイヤー(科学者):票を複数持っており、まとめて一人に入れることができる。"},
                {lang.OpportunistInfoLong, "オポチュニスト(科学者):第三陣営でタスクはなく、ゲーム終了時に生きていれば追加勝利"},
                {lang.FoxInfoLong, "狐(HideAndSeek):トロールを除くいずれかの陣営が勝利したときに生き残っていれば、勝利した陣営に追加で勝利することができる。"},
                {lang.TrollInfoLong, "トロール(HideAndSeek):インポスターにキルされたときに単独勝利となる。この場合、狐が生き残っていても狐は敗北する。"},
                //モード名
                {lang.HideAndSeek, "HideAndSeek"},
                {lang.NoGameEnd, "NoGameEnd"},
                {lang.SyncButtonMode, "ボタン回数同期モード"},
                //モード解説
                {lang.HideAndSeekInfo, "HideAndSeek:会議を開くことはできず、クルーはタスク完了、インポスターは全クルー殺害でのみ勝利することができる。サボタージュ、アドミン、カメラ、待ち伏せなどは禁止事項である。(設定有)"},
                {lang.NoGameEndInfo, "NoGameEnd:勝利判定が存在しないデバッグ用のモード。ホストのSHIFT+L以外でのゲーム終了ができない。"},
                {lang.SyncButtonModeInfo, "ボタン回数同期モード:プレイヤー全員のボタン回数が同期されているモード。"},
                //オプション項目
                {lang.AdvancedRoleOptions, "詳細設定"},
                {lang.VampireKillDelay, "吸血鬼の殺害までの時間(秒)"},
                {lang.MadmateCanFixLightsOut, "狂人が停電を直すことができる"},
                {lang.MadGuardianCanSeeBarrier, "守護狂人が自身の割れたバリアを見ることができる"},
                {lang.SabotageMasterSkillLimit, "サボタージュマスターがサボタージュに対して能力を使用できる回数(ドア閉鎖は除く)"},
                {lang.SabotageMasterFixesDoors, "サボタージュマスターが1度に複数のドアを開けることを許可する"},
                {lang.SabotageMasterFixesReactors, "サボタージュマスターが原子炉メルトダウンに対して能力を"},
                {lang.SabotageMasterFixesOxygens, "サボタージュマスターが酸素妨害に対して能力を使える"},
                {lang.SabotageMasterFixesCommunications, "サボタージュマスターがMIRA HQの通信妨害に対して能力を使える"},
                {lang.SabotageMasterFixesElectrical, "サボタージュマスターが停電に対して能力を使える"},
                {lang.MayorAdditionalVote, "メイヤーの追加投票の個数"},
                {lang.HideAndSeekOptions, "HideAndSeekの設定"},
                {lang.AllowCloseDoors, "ドア閉鎖を許可する"},
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
                {lang.JesterInfo, "Get voted out to win"},
                {lang.MadmateInfo, "Help the Impostors"},
                {lang.MadGuardianInfo, "Finish your tasks and Help the Impostors"},
                {lang.BaitInfo, "Be a decoy for the Crewmates"},
                {lang.TerroristInfo, "Die after finishing your tasks"},
                {lang.SidekickInfo, "Be the successor for the Impostors"},
                {lang.BeforeSidekickInfo,"You can not kill now"},
                {lang.AfterSidekickInfo,"Revenge to the Crewmates"},
                {lang.VampireInfo, "Kill Everyone with your bites"},
                {lang.SabotageMasterInfo, "Fix sabotages faster"},
                {lang.MayorInfo, "You Can Vote Two Times"},
                {lang.OpportunistInfo, "Do Whatever It Takes To Survive"},
                //役職解説(長)
                {lang.JesterInfoLong, "Jester(Scientist):投票で追放されたときに単独勝利となる第三陣営の役職。追放されずにゲームが終了するか、キルされると敗北となる。"},
                {lang.MadmateInfoLong, "Madmate(Engineer):インポスター陣営に属するが、Impostorが誰なのかはわからない。ImpostorからもMadmateが誰なのかはわからない。キルやサボタージュは使えないが、通気口を使うことができる。"},
                {lang.MadGuardianInfoLong, "MadGuardian(Scientist):インポスター陣営に属するが、Impostorが誰なのかはわからない。ImpostorからもMadGuardianが誰なのかはわからないが、タスクを完了させるとキルされなくなる。キルやサボタージュ、通気口は使えない。"},
                {lang.BaitInfoLong, "Bait(Scientist):キルされたときに、自分をキルした人に強制的に自分の死体を通報させることができる。"},
                {lang.TerroristInfoLong, "Terrorist(Engineer):自身のタスクを全て完了させた状態で死亡したときに単独勝利となる第三陣営の役職。死因はキルと追放のどちらでもよい。タスクを完了させずに死亡したり、死亡しないまま試合が終了すると敗北する。"},
                {lang.SidekickInfoLong, "Sidekick(Shapeshifter):初期状態でベントやサボタージュ、変身は可能だが、キルはできない。Sidekickではないインポスターが全員死亡すると、Sidekickもキルが可能となる。"},
                {lang.VampireInfoLong, "Vampire(Impostor):キルボタンを押してから一定秒数経って実際にキルが発生する役職。キルをしたときのテレポートは発生せず、キルボタンを押してから設定された秒数が経つまでに会議が始まるとその瞬間にキルが発生する。(設定有)"},
                {lang.SabotageMasterInfoLong, "SabotageMaster(Scientist):原子炉メルトダウンや酸素妨害、MIRA HQの通信妨害は片方を修理すれば両方が直る。停電は1箇所のレバーに触れると全て直る。ドアを開けるとその部屋の全てのドアが開く。(設定有)"},
                {lang.MayorInfoLong, "Mayor(Scientist):票を複数持っており、まとめて一人に入れることができる。"},
                {lang.OpportunistInfoLong, "Opportunist(Scientist):第三陣営でタスクはなく、ゲーム終了時に生きていれば追加勝利"},
                {lang.FoxInfoLong, "Fox(HideAndSeek):Trollを除くいずれかの陣営が勝利したときに生き残っていれば、勝利した陣営に追加で勝利することができる。"},
                {lang.TrollInfoLong, "Troll(HideAndSeek):インポスターにキルされたときに単独勝利となる。この場合、狐が生き残っていても狐は敗北する。。"},
                //モード名
                {lang.HideAndSeek, "HideAndSeek"},
                {lang.NoGameEnd, "NoGameEnd"},
                {lang.SyncButtonMode, "SyncButtonMode"},
                //モード解説
                {lang.HideAndSeekInfo, "HideAndSeek:会議を開くことはできず、Crewmateはタスク完了、Inpostorは全クルー殺害でのみ勝利することができる。サボタージュ、アドミン、カメラ、待ち伏せなどは禁止事項である。(設定有)"},
                {lang.NoGameEndInfo, "NoGameEnd:勝利判定が存在しないデバッグ用のモード。ホストのSHIFT+L以外でのゲーム終了ができない。"},
                {lang.SyncButtonModeInfo, "SyncButtonMode:プレイヤー全員のボタン回数が同期されているモード。"},
                //オプション項目
                {lang.AdvancedRoleOptions, "Advanced Options"},
                {lang.VampireKillDelay, "Vampire Kill Delay(s)"},
                {lang.SabotageMasterSkillLimit, "SabotageMaster Fixes Sabotage Limit(Ignore Closing Doors)"},
                {lang.MadmateCanFixLightsOut, "Madmate Can Fix Lights Out"},
                {lang.MadGuardianCanSeeBarrier, "MadGuardian Can See Own Cracked Barrier"},
                {lang.SabotageMasterFixesDoors, "SabotageMaster Can Fixes Multiple Doors"},
                {lang.SabotageMasterFixesReactors, "SabotageMaster Can Fixes Both Reactors"},
                {lang.SabotageMasterFixesOxygens, "SabotageMaster Can Fixes Both O2"},
                {lang.SabotageMasterFixesCommunications, "SabotageMaster Can Fixes Both Communications In MIRA HQ"},
                {lang.SabotageMasterFixesElectrical, "SabotageMaster Can Fixes Lights Out All At Once"},
                {lang.MayorAdditionalVote, "Mayor Additional Votes Count"},
                {lang.HideAndSeekOptions, "HideAndSeek Options"},
                {lang.AllowCloseDoors, "Allow Closing Doors"},
                {lang.HideAndSeekWaitingTime, "Impostor Waiting Time"},
                {lang.IgnoreCosmetics, "Ignore Cosmetics"},
                {lang.IgnoreVent, "Ignore Using Vents"},
                {lang.HideAndSeekRoles, "HideAndSeek Roles"},
                {lang.SyncedButtonCount, "Max Button Count"},
                {lang.DisableTasks, "Disable Tasks"},
                {lang.DisableSwipeCardTask, "Disable SwipeCard Tasks"},
                {lang.DisableSubmitScanTask, "Disable SubmitScan Tasks"},
                {lang.DisableUnlockSafeTask, "Disable UnlockSafe Tasks"},
                {lang.DisableUploadDataTask, "Disable UploadData Tasks"},
                {lang.DisableStartReactorTask, "Disable StartReactor Tasks"},
                {lang.SuffixMode, "Suffix"},
                //その他
                {lang.commandError, "Error:%1$"},
                {lang.InvalidArgs, "Invalis Args"},
                {lang.ON, "ON"},
                {lang.OFF, "OFF"},
            };
            EnglishRoleNames = new Dictionary<CustomRoles, string>(){
                {CustomRoles.Jester, "Jester"},
                {CustomRoles.Madmate, "Madmate"},
                {CustomRoles.MadGuardian, "MadGuardian"},
                {CustomRoles.Bait, "Bait"},
                {CustomRoles.Terrorist, "Terrorist"},
                {CustomRoles.Sidekick, "Sidekick"},
                {CustomRoles.Vampire, "Vampire"},
                {CustomRoles.SabotageMaster, "SabotageMaster"},
                {CustomRoles.Mayor, "Mayor"},
                {CustomRoles.Opportunist, "Opportunist"},
                {CustomRoles.Fox, "Fox"},
                {CustomRoles.Troll, "Troll"},
            };
            JapaneseRoleNames = new Dictionary<CustomRoles, string>(){
                {CustomRoles.Jester, "ジェスター"},
                {CustomRoles.Madmate, "狂人"},
                {CustomRoles.MadGuardian, "守護狂人"},
                {CustomRoles.Bait, "ベイト"},
                {CustomRoles.Terrorist, "テロリスト"},
                {CustomRoles.Sidekick, "相棒"},
                {CustomRoles.Vampire, "吸血鬼"},
                {CustomRoles.SabotageMaster, "サボタージュマスター"},
                {CustomRoles.Mayor, "メイヤー"},
                {CustomRoles.Opportunist, "オポチュニスト"},
                {CustomRoles.Fox, "狐"},
                {CustomRoles.Troll, "トロール"},
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
        BeforeSidekickInfo,
        AfterSidekickInfo,
        VampireInfo,
        SabotageMasterInfo,
        MadGuardianInfo,
        MayorInfo,
        OpportunistInfo,
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
        MadGuardianInfoLong,
        MayorInfoLong,
        OpportunistInfoLong,
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
        MayorAdditionalVote,
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
    public enum CustomRoles {
        Default = 0,
        Jester,
        Madmate,
        Bait,
        Terrorist,
        Sidekick,
        Vampire,
        SabotageMaster,
        MadGuardian,
        Mayor,
        Opportunist,
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
