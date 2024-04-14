using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assets.CoreScripts;
using HarmonyLib;
using Hazel;
using System.Text.RegularExpressions;

using TownOfHostForE.Roles.Core;
using static TownOfHostForE.Translator;
using TownOfHostForE.Roles.Crewmate;
using TownOfHostForE.Roles.AddOns.Common;
using AmongUs.GameOptions;
using Rewired;
using static System.Int32;
using TownOfHostForE.GameMode;
using LibCpp2IL.MachO;

namespace TownOfHostForE
{
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
    class ChatCommands
    {
        public static List<string> ChatHistory = new();
        private static Dictionary<CustomRoles, string> roleCommands;

        //ロビーリセット一式
        public static int LobbyLimit = 15;
        public static bool StartButtonReset = false;
        public static bool ResetVersionCheckFlag = false;
        public static List<byte> OtherVersionPlayerId = new();

        public static bool Prefix(ChatController __instance)
        {
            // クイックチャットなら横流し
            if (__instance.quickChatField.Visible)
            {
                return true;
            }
            // 入力欄に何も書かれてなければブロック
            if (__instance.freeChatField.textArea.text == "")
            {
                return false;
            }
            __instance.timeSinceLastMessage = 3f;
            var text = __instance.freeChatField.textArea.text;
            if (ChatHistory.Count == 0 || ChatHistory[^1] != text) ChatHistory.Add(text);
            ChatControllerUpdatePatch.CurrentHistorySelection = ChatHistory.Count;
            string[] args = text.Split(' ');
            string subArgs = "";
            var canceled = false;
            var cancelVal = "";
            Main.isChatCommand = true;
            Logger.Info(text, "SendChat");

            if (GuessManager.GuesserMsg(PlayerControl.LocalPlayer, text)) canceled = true;

            var tag = !PlayerControl.LocalPlayer.Data.IsDead ? "SendChatHost" : "SendChatDeadHost";
            if (text.StartsWith("試合結果:") || text.StartsWith("キル履歴:")) tag = "SendSystemChat";
            VoiceReader.ReadHost(text, tag);

            switch (args[0])
            {
                case "/dump":
                    canceled = true;
                    Utils.DumpLog();
                    break;
                case "/v":
                case "/version":
                    canceled = true;
                    string version_text = "";
                    foreach (var kvp in Main.playerVersion.OrderBy(pair => pair.Key))
                    {
                        version_text += $"{kvp.Key}:{Utils.GetPlayerById(kvp.Key)?.Data?.PlayerName}:{kvp.Value.forkId}/{kvp.Value.version}({kvp.Value.tag})\n";
                    }
                    if (version_text != "") HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, version_text);
                    break;
                default:
                    Main.isChatCommand = false;
                    break;
            }
            if (AmongUsClient.Instance.AmHost)
            {
                Main.isChatCommand = true;

                if (!GameStates.IsLobby)
                {
                    var cRole = PlayerControl.LocalPlayer.GetCustomRole();
                    if (!cRole.IsNotAssignRoles())
                    {
                        PlayerControl.LocalPlayer.GetRoleClass().OnReceiveChat(PlayerControl.LocalPlayer, text);
                    }
                    Ojou.OjouOnReceiveChat(PlayerControl.LocalPlayer, args[0]);
                    Chu2Byo.Chu2OnReceiveChat(PlayerControl.LocalPlayer, args[0]);
                    WordLimit.OnReceiveChat(PlayerControl.LocalPlayer, args[0]);
                }
                canceled = BetWinTeams.BetOnReceiveChat(PlayerControl.LocalPlayer, text);
                switch (args[0])
                {
                    case "/win":
                    case "/winner":
                        canceled = true;
                        Utils.SendMessage("Winner: " + string.Join(",", Main.winnerList.Select(b => Main.AllPlayerNames[b])));
                        break;

                    case "/l":
                    case "/lastresult":
                        canceled = true;
                        Utils.ShowLastResult();
                        break;

                    case "/kl":
                    case "/killlog":
                        canceled = true;
                        Utils.ShowKillLog();
                        break;

                    case "/r":
                    case "/rename":
                        canceled = true;
                        Main.nickName = args.Length > 1 ? Main.nickName = args[1] : "";
                        break;

                    case "/hn":
                    case "/hidename":
                        canceled = true;
                        Main.HideName.Value = args.Length > 1 ? args.Skip(1).Join(delimiter: " ") : Main.HideName.DefaultValue.ToString();
                        GameStartManagerPatch.HideName.text = Main.HideName.Value;
                        break;

                    case "/n":
                    case "/now":
                        canceled = true;
                        subArgs = args.Length < 2 ? "" : args[1];
                        switch (subArgs)
                        {
                            case "r":
                            case "roles":
                                Utils.ShowActiveRoles();
                                break;
                            default:
                                Utils.ShowActiveSettings();
                                break;
                        }
                        break;
                    case "/SetWordLimit":
                    case "/swl":
                        canceled = true;
                        WordLimit.SetLimitWord(args[1]);
                        Utils.SendMessage($"制限ワード「{args[1]}」を適用しました！\n制限モードを有効にしてお楽しみください。");
                        break;
                    case "/w":
                        canceled = true;
                        if (!GameStates.IsInGame) break;
                        subArgs = args.Length < 2 ? "" : args[1];
                        switch (subArgs)
                        {
                            case "crewmate":
                                GameManager.Instance.enabled = false;
                                CustomWinnerHolder.WinnerTeam = CustomWinner.Crewmate;
                                foreach (var player in Main.AllPlayerControls.Where(pc => pc.Is(CustomRoleTypes.Crewmate)))
                                {
                                    CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                                }
                                GameEndChecker.StartEndGame(GameOverReason.HumansByTask);
                                break;
                            case "impostor":
                                GameManager.Instance.enabled = false;
                                CustomWinnerHolder.WinnerTeam = CustomWinner.Impostor;
                                foreach (var player in Main.AllPlayerControls.Where(pc => pc.Is(CustomRoleTypes.Impostor) || pc.Is(CustomRoleTypes.Madmate)))
                                {
                                    CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                                }
                                GameEndChecker.StartEndGame(GameOverReason.ImpostorByKill);
                                break;
                            case "none":
                                GameManager.Instance.enabled = false;
                                CustomWinnerHolder.WinnerTeam = CustomWinner.None;
                                GameEndChecker.StartEndGame(GameOverReason.ImpostorByKill);
                                break;
                            case "jackal":
                                GameManager.Instance.enabled = false;
                                CustomWinnerHolder.WinnerTeam = CustomWinner.Jackal;
                                foreach (var player in Main.AllPlayerControls.Where(pc => pc.Is(CustomRoles.Jackal) || pc.Is(CustomRoles.JSchrodingerCat)))
                                {
                                    CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                                }
                                GameEndChecker.StartEndGame(GameOverReason.ImpostorByKill);
                                break;
                            case "animals":
                                GameManager.Instance.enabled = false;
                                CustomWinnerHolder.WinnerTeam = CustomWinner.Animals;
                                foreach (var player in Main.AllPlayerControls.Where(pc => pc.Is(CustomRoles.Coyote) ||
                                                                                    pc.Is(CustomRoles.Vulture) ||
                                                                                    pc.Is(CustomRoles.Badger) ||
                                                                                    pc.Is(CustomRoles.Braki) ||
                                                                                    pc.Is(CustomRoles.Leopard) ||
                                                                                    pc.Is(CustomRoles.Nyaoha) ||
                                                                                    pc.Is(CustomRoles.AOjouSama) ||
                                                                                    pc.Is(CustomRoles.ASchrodingerCat)))
                                {
                                    CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                                }
                                GameEndChecker.StartEndGame(GameOverReason.ImpostorByKill);
                                break;

                            default:
                                __instance.AddChat(PlayerControl.LocalPlayer, "crewmate | impostor | jackal | animals | none");
                                cancelVal = "/w";
                                break;
                        }
                        break;

                    case "/dis":
                        canceled = true;
                        subArgs = args.Length < 2 ? "" : args[1];
                        switch (subArgs)
                        {
                            case "crewmate":
                                GameManager.Instance.enabled = false;
                                GameManager.Instance.RpcEndGame(GameOverReason.HumansDisconnect, false);
                                break;

                            case "impostor":
                                GameManager.Instance.enabled = false;
                                GameManager.Instance.RpcEndGame(GameOverReason.ImpostorDisconnect, false);
                                break;

                            default:
                                __instance.AddChat(PlayerControl.LocalPlayer, "crewmate | impostor");
                                cancelVal = "/dis";
                                break;
                        }
                        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Admin, 0);
                        break;

                    case "/h":
                    case "/help":
                        canceled = true;
                        subArgs = args.Length < 2 ? "" : args[1];
                        switch (subArgs)
                        {
                            case "r":
                            case "roles":
                                subArgs = args.Length < 3 ? "" : args[2];
                                GetRolesInfo(subArgs);
                                break;

                            case "a":
                            case "addons":
                                subArgs = args.Length < 3 ? "" : args[2];
                                switch (subArgs)
                                {
                                    case "lastimpostor":
                                    case "limp":
                                        Utils.SendMessage(Utils.GetRoleName(CustomRoles.LastImpostor) + GetString("LastImpostorInfoLong"));
                                        break;

                                    default:
                                        Utils.SendMessage($"{GetString("Command.h_args")}:\n lastimpostor(limp)");
                                        break;
                                }
                                break;

                            case "m":
                            case "modes":
                                subArgs = args.Length < 3 ? "" : args[2];
                                switch (subArgs)
                                {
                                    case "hideandseek":
                                    case "has":
                                        Utils.SendMessage(GetString("HideAndSeekInfo"));
                                        break;

                                    case "nogameend":
                                    case "nge":
                                        Utils.SendMessage(GetString("NoGameEndInfo"));
                                        break;

                                    case "syncbuttonmode":
                                    case "sbm":
                                        Utils.SendMessage(GetString("SyncButtonModeInfo"));
                                        break;

                                    case "randommapsmode":
                                    case "rmm":
                                        Utils.SendMessage(GetString("RandomMapsModeInfo"));
                                        break;

                                    default:
                                        Utils.SendMessage($"{GetString("Command.h_args")}:\n hideandseek(has), nogameend(nge), syncbuttonmode(sbm), randommapsmode(rmm)");
                                        break;
                                }
                                break;

                            case "n":
                            case "now":
                                Utils.ShowActiveSettingsHelp();
                                break;
                            case "b":
                            case "bet":
                                //判定自体は霊界の奴に持たせる
                                break;

                            default:
                                Utils.ShowHelp();
                                break;
                        }
                        break;

                    case "/m":
                    case "/myrole":
                        canceled = true;
                        if (GameStates.IsInGame)
                        {
                            var role = PlayerControl.LocalPlayer.GetCustomRole();
                            HudManager.Instance.Chat.AddChat(
                                PlayerControl.LocalPlayer,
                                role.GetRoleInfo()?.Description?.FullFormatHelp ??
                                // roleInfoがない役職
                                GetString(role.ToString()) + PlayerControl.LocalPlayer.GetRoleInfo(true));
                        }
                        break;

                    case "/t":
                    case "/template":
                        canceled = true;
                        if (args.Length > 1) TemplateManager.SendTemplate(args[1]);
                        else HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, $"{GetString("ForExample")}:\n{args[0]} test");
                        break;

                    case "/mw":
                    case "/messagewait":
                        canceled = true;
                        if (args.Length > 1 && float.TryParse(args[1], out float sec))
                        {
                            Main.MessageWait.Value = sec;
                            Utils.SendMessage(string.Format(GetString("Message.SetToSeconds"), sec), 0);
                        }
                        else Utils.SendMessage($"{GetString("Message.MessageWaitHelp")}\n{GetString("ForExample")}:\n{args[0]} 3", 0);
                        break;

                    case "/say":
                        canceled = true;
                        if (args.Length > 1)
                            Utils.SendMessage(args.Skip(1).Join(delimiter: " "), title: $"<color=#ff0000>{GetString("MessageFromTheHost")}</color>");
                        break;

                    case "/exile":
                        canceled = true;
                        if (args.Length < 2 || !int.TryParse(args[1], out int id)) break;
                        Utils.GetPlayerById(id)?.RpcExileV2();
                        break;

                    case "/kill":
                        canceled = true;
                        if (args.Length < 2 || !int.TryParse(args[1], out int id2)) break;
                        Utils.GetPlayerById(id2)?.RpcMurderPlayer(Utils.GetPlayerById(id2));
                        break;

                    case "/vo":
                    case "/voice":
                        canceled = true;
                        if (args.Length > 1 && args[1] == "reset")
                        {
                            VoiceReader.ResetVoiceNo();
                        }
                        else if (args.Length > 1 && args[1] == "random")
                        {
                            VoiceReader.SetRandomVoiceNo();
                        }
                        else if (args.Length > 1 && int.TryParse(args[1], out int voiceNo))
                        {
                            var name = VoiceReader.SetHostVoiceNo(voiceNo);
                            if (name != null && name != "")
                                Utils.SendMessage($"ホスト の読上げを {name} に変更しました");
                        }
                        else
                            Utils.SendMessage(VoiceReader.GetVoiceIdxMsg());
                        break;

                    //case "/mp":
                    //    // Unfortunately server holds this - need to do more trickery
                    //    canceled = true;
                    //    if (AmongUsClient.Instance.CanBan())
                    //    {
                    //        if (!TryParse(text[4..], out LobbyLimit))
                    //        {
                    //            __instance.AddChat(PlayerControl.LocalPlayer, "使い方\n/mp {最大人数}");
                    //        }
                    //        else
                    //        {
                    //            if (LobbyLimit > 15)
                    //            {
                    //                __instance.AddChat(PlayerControl.LocalPlayer, $"プレイヤー最大人数は15人以下にしてください。");
                    //            }
                    //            else if (LobbyLimit != GameManager.Instance.LogicOptions.currentGameOptions.MaxPlayers)
                    //            {
                    //                GameManager.Instance.LogicOptions.currentGameOptions.SetInt(Int32OptionNames.MaxPlayers, LobbyLimit);
                    //                FastDestroyableSingleton<GameStartManager>.Instance.LastPlayerCount = LobbyLimit;
                    //                RoomOption.RpcSyncOption(GameManager.Instance.LogicOptions.currentGameOptions);
                    //                __instance.AddChat(PlayerControl.LocalPlayer, $"ロビーの最大人数を{LobbyLimit}人に変更しました！");
                    //            }
                    //            else
                    //            {
                    //                __instance.AddChat(PlayerControl.LocalPlayer, $"プレイヤー最小人数は {LobbyLimit}です。");
                    //            }
                    //        }
                    //    }
                    //    break;
                    case "/rrs":
                        canceled = true;
                        if (ResetVersionCheckFlag)
                        {
                            StartButtonReset = true;

                            Main.playerVersion.Clear();
                            foreach (var playerId in OtherVersionPlayerId)
                            {
                                //削除対象且つ、ホストのIDとは違う。
                                if (Main.playerVersion.ContainsKey(playerId) && playerId != PlayerControl.LocalPlayer.PlayerId)
                                {
                                    Main.playerVersion.Remove(playerId);
                                }
                            }
                            __instance.AddChat(PlayerControl.LocalPlayer, $"スタートボタンの表示をリセットしました");
                        }
                        break;

                    default:
                        Main.isChatCommand = false;
                        break;
                }
            }
            if (canceled)
            {
                Logger.Info("Command Canceled", "ChatCommand");
                __instance.freeChatField.textArea.Clear();
                __instance.freeChatField.textArea.SetText(cancelVal);
                //__instance.quickChatMenu.ResetGlyphs();
            }
            return !canceled;
        }

        public static void GetRolesInfo(string role)
        {
            // 初回のみ処理
            if (roleCommands == null)
            {
#pragma warning disable IDE0028  // Dictionary初期化の簡素化をしない
                roleCommands = new Dictionary<CustomRoles, string>();

                // GM
                roleCommands.Add(CustomRoles.GM, "ゲームマスター");

                // Impostor役職
                roleCommands.Add((CustomRoles)(-1), $"== {GetString("Impostor")} ==");  // 区切り用
                ConcatCommands(CustomRoleTypes.Impostor);

                // Madmate役職
                roleCommands.Add((CustomRoles)(-2), $"== {GetString("Madmate")} ==");  // 区切り用
                ConcatCommands(CustomRoleTypes.Madmate);
                roleCommands.Add(CustomRoles.SKMadmate, "サイドキックマッドメイト");

                // Crewmate役職
                roleCommands.Add((CustomRoles)(-3), $"== {GetString("Crewmate")} ==");  // 区切り用
                ConcatCommands(CustomRoleTypes.Crewmate);

                // Neutral役職
                roleCommands.Add((CustomRoles)(-4), $"== {GetString("Neutral")} ==");  // 区切り用
                ConcatCommands(CustomRoleTypes.Neutral);

                // Animals役職
                roleCommands.Add((CustomRoles)(-5), $"== {GetString("Animals")} ==");  // 区切り用
                ConcatCommands(CustomRoleTypes.Animals);

                // 属性
                roleCommands.Add((CustomRoles)(-6), $"== {GetString("Addons")} ==");  // 区切り用
                roleCommands.Add(CustomRoles.LastImpostor, "ラストインポスター");
                roleCommands.Add(CustomRoles.Lovers, "ラバーズ");
                roleCommands.Add(CustomRoles.Workhorse, "ワークホース");
                roleCommands.Add(CustomRoles.CompreteCrew, "コンプリートクルー");
                roleCommands.Add(CustomRoles.AddWatch, "ウォッチング");
                roleCommands.Add(CustomRoles.Sunglasses, "サングラス");
                roleCommands.Add(CustomRoles.AddLight, "ライティング");
                roleCommands.Add(CustomRoles.AddSeer, "シーイング");
                roleCommands.Add(CustomRoles.Autopsy, "オートプシー");
                roleCommands.Add(CustomRoles.VIP, "VIP");
                roleCommands.Add(CustomRoles.Clumsy, "クラムシー");
                roleCommands.Add(CustomRoles.Revenger, "リベンジャー");
                roleCommands.Add(CustomRoles.Management, "マネジメント");
                roleCommands.Add(CustomRoles.InfoPoor, "インフォプアー");
                roleCommands.Add(CustomRoles.Sending, "センディング");
                roleCommands.Add(CustomRoles.TieBreaker, "タイブレーカー");
                roleCommands.Add(CustomRoles.NonReport, "ノンレポート");
                roleCommands.Add(CustomRoles.Loyalty, "ロイヤルティ");
                roleCommands.Add(CustomRoles.PlusVote, "プラスボート");
                roleCommands.Add(CustomRoles.Guarding, "ガーディング");
                roleCommands.Add(CustomRoles.AddBait, "ベイティング");
                roleCommands.Add(CustomRoles.Refusing, "リフュージング");
                roleCommands.Add(CustomRoles.Chu2Byo, "中二病");
                roleCommands.Add(CustomRoles.Gambler, "ギャンブラー");

                // HAS
                roleCommands.Add((CustomRoles)(-7), $"== {GetString("HideAndSeek")} ==");  // 区切り用
                roleCommands.Add(CustomRoles.HASFox, "hfo");
                roleCommands.Add(CustomRoles.HASTroll, "htr");

                // HAS
                roleCommands.Add((CustomRoles)(-8), $"== {GetString("SuperBombParty")} ==");  // 区切り用
#pragma warning restore IDE0028
            }

            foreach (var r in roleCommands)
            {
                var roleName = r.Key.ToString();
                var roleShort = r.Value;

                if (String.Compare(role, roleName, true) == 0 || String.Compare(role, roleShort, true) == 0)
                {
                    var roleInfo = r.Key.GetRoleInfo();
                    if (roleInfo != null && roleInfo.Description != null)
                    {
                        Utils.SendMessage(roleInfo.Description.FullFormatHelp, removeTags: false);
                    }
                    // RoleInfoがない役職は従来の処理
                    else
                    {
                        Utils.SendMessage(GetString(roleName) + GetString($"{roleName}InfoLong"));
                    }
                    return;
                }
            }
            Utils.SendMessage(GetString("Message.HelpRoleNone"));
        }
        private static void ConcatCommands(CustomRoleTypes roleType)
        {
            var roles = CustomRoleManager.AllRolesInfo.Values.Where(role => role.CustomRoleType == roleType);
            foreach (var role in roles)
            {
                if (role.ChatCommand is null) continue;

                roleCommands[role.RoleName] = role.ChatCommand;
            }
        }

        public static string FixRoleNameInput(string text)
        {
            //日本語には関係ないね
            //text = text.Replace("着", "者").Trim().ToLower();
            return text switch
            {
                "GM" or "gm" or "ゲームマスター" => GetString("GM"),

                //インポスター
                "バウンティハンター" => GetString("BountyHunter"),
                "イビルトラッカー" => GetString("EvilTracker"),
                "イビルウォッチャー" => GetString("EvilWatcher"),
                "花火職人" => GetString("FireWorks"),
                "メアー" => GetString("Mare"),
                "パペッティア" => GetString("Puppeteer"),
                "シリアルキラー" => GetString("SerialKiller"),
                "スナイパー" => GetString("Sniper"),
                "タイムシーフ" => GetString("TimeThief"),
                "吸血鬼" or "ヴァンパイア" => GetString("Vampire"),
                "ウォーロック" => GetString("Warlock"),
                "魔女" or "ウィッチ" => GetString("Witch"),
                "マフィア" => GetString("Mafia"),
                "アンチアドミナー" => GetString("AntiAdminer"),
                "イビル猫又" => GetString("Evilneko"),
                "呪狼" => GetString("CursedWolf"),
                "グリーディア" => GetString("Greedier"),
                "アンビショナー" => GetString("Ambitioner"),
                "スカベンジャー" => GetString("Scavenger"),
                "イビルディバイナー" => GetString("EvilDiviner"),
                "テレパシスターズ" => GetString("Telepathisters"),
                "シェイプキラー" => GetString("ShapeKiller"),
                "爆裂魔" => GetString("SuicideBomber"),
                "シンデレラ" => GetString("Cinderella"),
                "シェイプマスター" => GetString("ShapeMaster"),
                "イビルゲッサー" => GetString("EvilGuesser"),
                "トークティブ" => GetString("Talktive"),
                "テレポーター" => GetString("Teleporter"),
                "ラストインポスター" => GetString("LastImpostor"),
                "シェイプシフター" => GetString("NormalShapeshifter"),

                //マッドメイト
                "マッドメイト" => GetString("Madmate"),
                "マッドガーディアン" => GetString("MadGuardian"),
                "マッドスニッチ" => GetString("MadSnitch"),
                "マッドディクテーター" => GetString("MadDictator"),
                "マッドネイチャコール" => GetString("MadNatureCalls"),
                "マッドブラックアウター" => GetString("MadBrackOuter"),
                "マッドシェリフ" => GetString("MadSheriff"),
                "サイドキックマッドメイト" => GetString("SKMadmate"),
                "うさぎ(M)" or "Mうさぎ" => GetString("IUsagi"),
                "Mシュレ猫" or "Mシュレディンガーの猫" => GetString("MSchrodingerCat"),
                "Mお嬢様" or "マッドお嬢様" => GetString("MOjouSama"),
                "マッドニムロッド" => GetString("MadNimrod"),

                //クルーメイト
                "ベイト" => GetString("Bait"),
                "ディクテーター" => GetString("Dictator"),
                "ドクター" => GetString("Doctor"),
                "ライター" => GetString("Lighter"),
                "メイヤー" => GetString("Mayor"),
                "ナイスウォッチャー" => GetString("NiceWatcher"),
                "サボタージュマスター" => GetString("SabotageMaster"),
                "シーア" => GetString("Seer"),
                "シェリフ" => GetString("Sheriff"),
                "スニッチ" => GetString("Snitch"),
                "スピードブースター" => GetString("SpeedBooster"),
                "トラッパー" => GetString("Trapper"),
                "ハンター" => GetString("Hunter"),
                "タイムマネージャー" => GetString("TimeManager"),
                "パン屋" => GetString("Bakery"),
                "エクスプレス" => GetString("Express"),
                "チェアマン" => GetString("Chairman"),
                "にじいろスター" or "にじいろ" => GetString("Rainbow"),
                "猫又" => GetString("Nekomata"),
                "見送り人" => GetString("SeeingOff"),
                "バカシェリフ" => GetString("SillySheriff"),
                "共鳴者" => GetString("Sympathizer"),
                "ブラインダー" => GetString("Blinder"),
                "メディック" => GetString("Medic"),
                "キャンドルライター" => GetString("CandleLighter"),
                "グラージシェリフ" => GetString("GrudgeSheriff"),
                "占い師" => GetString("FortuneTeller"),
                "霊媒師" => GetString("Psychic"),
                "お嬢様" => GetString("OjouSama"),
                "ネゴシエーター" => GetString("Counselor"),
                "ちいかわ" => GetString("Tiikawa"),
                "ハチワレ" or "はちわれ" => GetString("Hachiware"),
                "うさぎ" => GetString("Usagi"),
                "ナイスゲッサー" => GetString("NiceGuesser"),
                "ニムロッド" => GetString("Nimrod"),
                "エンジニア" => GetString("NormalEngineer"),
                "科学者" => GetString("NormalScientist"),

                //第3陣営
                "アーソニスト" => GetString("Arsonist"),
                "エゴイスト" => GetString("Egoist"),
                "エクスキューショナー" => GetString("Executioner"),
                "ジャッカル" => GetString("Jackal"),
                "ジェスター" => GetString("Jester"),
                "姫" or "純愛者" or "ラバーズ" => GetString("Lovers"),
                "オポチュニスト" or "オポチュニストキラー" => GetString("Opportunist"),
                "テロリスト" => GetString("Terrorist"),
                "シュレディンガーの猫" or "シュレ猫" => GetString("SchrodingerCat"),
                "Eシュレディンガーの猫" or "Eシュレ猫" => GetString("EgoSchrodingerCat"),
                "Oシュレディンガーの猫" or "Oシュレ猫" => GetString("OSchrodingerCat"),
                "Jシュレディンガーの猫" or "Jシュレ猫" => GetString("JSchrodingerCat"),
                "Dシュレディンガーの猫" or "Dシュレ猫" => GetString("DSchrodingerCat"),
                "Gシュレディンガーの猫" or "Gシュレ猫" => GetString("GSchrodingerCat"),
                "Eお嬢様" => GetString("EOjouSama"),
                "Oお嬢様" => GetString("OOjouSama"),
                "Jお嬢様" => GetString("JOjouSama"),
                "Dお嬢様" => GetString("DOjouSama"),
                "アンチコンプリート" => GetString("AntiComplete"),
                "ワーカホリック" => GetString("Workaholic"),
                "ダークハイド" => GetString("DarkHide"),
                "ラブカッター" => GetString("LoveCutter"),
                "弁護士" => GetString("Lawyer"),
                "クライアント" => GetString("JClient"),
                "トトカルチョ" => GetString("Totocalcio"),
                "義賊" => GetString("Gizoku"),
                "ワークホース" => GetString("Workhorse"),
                "決闘者" => GetString("Duelist"),

                //アニマルズ
                "コヨーテ" => GetString("Coyote"),
                "バルチャー" => GetString("Vulture"),
                "アナグマ" => GetString("Badger"),
                "ブラキディオス" => GetString("Braki"),
                "ヒョウ" => GetString("Leopard"),
                _ => text,
            };
        }
        public static bool GetRoleByInputName(string input, out CustomRoles output, bool includeVanilla = false)
        {
            output = new();
            input = Regex.Replace(input, @"[0-9]+", string.Empty);
            input = Regex.Replace(input, @"\s", string.Empty);
            input = Regex.Replace(input, @"[\x01-\x1F,\x7F]", string.Empty);
            input = input.ToLower().Trim().Replace("是", string.Empty);
            if (input == "" || input == string.Empty) return false;
            input = FixRoleNameInput(input).ToLower();
            foreach (CustomRoles role in Enum.GetValues(typeof(CustomRoles)))
            {
                if (!includeVanilla && role.IsVanilla() && role != CustomRoles.GuardianAngel) continue;
                if (input == GuessManager.ChangeNormal2Vanilla(role))
                {
                    output = role;
                    return true;
                }
            }
            return false;
        }
        public static void OnReceiveChat(PlayerControl player, string text, out bool canceled)
        {
            if (player != null)
            {
                var tag = !player.Data.IsDead ? "SendChatAlive" : "SendChatDead";
                VoiceReader.Read(text, Palette.GetColorName(player.Data.DefaultOutfit.ColorId), tag);
            }

            canceled = false;

            if (!AmongUsClient.Instance.AmHost) return;
            string[] args = text.Split(' ');
            string subArgs = "";

            if (GuessManager.GuesserMsg(player, text)) { canceled = true; return; }

            switch (args[0])
            {
                case "/l":
                case "/lastresult":
                    Utils.ShowLastResult(player.PlayerId);
                    break;

                case "/kl":
                case "/killlog":
                    Utils.ShowKillLog(player.PlayerId);
                    break;

                case "/n":
                case "/now":
                    subArgs = args.Length < 2 ? "" : args[1];
                    switch (subArgs)
                    {
                        case "r":
                        case "roles":
                            Utils.ShowActiveRoles(player.PlayerId);
                            break;

                        default:
                            Utils.ShowActiveSettings(player.PlayerId);
                            break;
                    }
                    break;

                case "/h":
                case "/help":
                    subArgs = args.Length < 2 ? "" : args[1];
                    switch (subArgs)
                    {
                        case "n":
                        case "now":
                            Utils.ShowActiveSettingsHelp(player.PlayerId);
                            break;
                    }
                    break;

                case "/m":
                case "/myrole":
                    if (GameStates.IsInGame)
                    {
                        var role = player.GetCustomRole();
                        if (role.GetRoleInfo()?.Description is { } description)
                        {
                            Utils.SendMessage(description.FullFormatHelp, player.PlayerId, removeTags: false);
                        }
                        // roleInfoがない役職
                        else
                        {
                            Utils.SendMessage(GetString(role.ToString()) + player.GetRoleInfo(true), player.PlayerId);
                        }
                    }
                    break;

                case "/t":
                case "/template":
                    if (args.Length > 1) TemplateManager.SendTemplate(args[1], player.PlayerId);
                    else Utils.SendMessage($"{GetString("ForExample")}:\n{args[0]} test", player.PlayerId);
                    break;

                case "/vo":
                case "/voice":
                    var color = Palette.GetColorName(player.Data.DefaultOutfit.ColorId);
                    if (VoiceReader.VoiceReaderMode == null || !VoiceReader.VoiceReaderMode.GetBool())
                        Utils.SendMessage($"現在読上げは停止しています", player.PlayerId);
                    else if (args.Length > 1 && args[1] == "n")
                        Utils.SendMessage($"{color} の現在の読上げは {VoiceReader.GetVoiceName(color)} です", player.PlayerId);
                    else if (args.Length > 1 && int.TryParse(args[1], out int voiceNo))
                    {
                        var name = VoiceReader.SetVoiceNo(color, voiceNo);
                        if (name != null && name != "")
                        {
                            Utils.SendMessage($"{color} の読上げを {name} に変更しました", player.PlayerId);
                            break;
                        }
                        Utils.SendMessage($"{color} の読上げを変更できませんでした", player.PlayerId);
                    }
                    else
                        Utils.SendMessage(VoiceReader.GetVoiceIdxMsg(), player.PlayerId);
                    break;

                default:
                    break;
            }
        }
    }
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
    class ChatUpdatePatch
    {
        public static bool DoBlockChat = false;
        public static void Postfix(ChatController __instance)
        {
            //if (!AmongUsClient.Instance.AmHost || Main.MessagesToSend.Count < 1 || (Main.MessagesToSend[0].Item2 == byte.MaxValue && Main.MessageWait.Value > __instance.TimeSinceLastMessage)) return;
            if (!AmongUsClient.Instance.AmHost) return;
            if (DoBlockChat) return;
            string msg;
            byte sendTo;
            string title;

            var player = Main.AllAlivePlayerControls.OrderBy(x => x.PlayerId).FirstOrDefault();

            if (!(Main.MessagesToSend.Count < 1 || (Main.MessagesToSend[0].Item2 == byte.MaxValue && Main.MessageWait.Value > __instance.timeSinceLastMessage)))
            {
                if (player == null) return;
                (msg, sendTo, title) = Main.MessagesToSend[0];
                Main.MessagesToSend.RemoveAt(0);
            }
            else if (CustomRoles.OjouSama.IsEnable()) //将来的にはここいらんかも
            {
                if (!(Main.SuffixMessagesToSend.Count < 1 || (Main.SuffixMessagesToSend[0].Item2 == byte.MaxValue && Main.MessageWait.Value > __instance.timeSinceLastMessage)))
                {
                    (msg, sendTo, title, player) = Main.SuffixMessagesToSend[0];
                    Main.SuffixMessagesToSend.RemoveAt(0);
                }
                else
                {
                    return;
                }
            }
            else
            {
                return;
            }
            int clientId = sendTo == byte.MaxValue ? -1 : Utils.GetPlayerById(sendTo).GetClientId();
            var name = player.Data.PlayerName;
            if (clientId == -1)
            {
                player.SetName(title);
                DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, msg);
                player.SetName(name);
            }
            var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
            writer.StartMessage(clientId);
            writer.StartRpc(player.NetId, (byte)RpcCalls.SetName)
                .Write(title)
                .EndRpc();
            writer.StartRpc(player.NetId, (byte)RpcCalls.SendChat)
                .Write(msg)
                .EndRpc();
            writer.StartRpc(player.NetId, (byte)RpcCalls.SetName)
                .Write(player.Data.PlayerName)
                .EndRpc();
            writer.EndMessage();
            writer.SendMessage();
            __instance.timeSinceLastMessage = 0f;
        }
    }

    [HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
    class AddChatPatch
    {
        public static void Postfix(string chatText)
        {
            switch (chatText)
            {
                default:
                    break;
            }
            if (!AmongUsClient.Instance.AmHost) return;
        }
    }
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSendChat))]
    class RpcSendChatPatch
    {
        public static bool Prefix(PlayerControl __instance, string chatText, ref bool __result)
        {
            if (string.IsNullOrWhiteSpace(chatText))
            {
                __result = false;
                return false;
            }
            int return_count = PlayerControl.LocalPlayer.name.Count(x => x == '\n');
            chatText = new StringBuilder(chatText).Insert(0, "\n", return_count).ToString();
            if (AmongUsClient.Instance.AmClient && DestroyableSingleton<HudManager>.Instance)
                DestroyableSingleton<HudManager>.Instance.Chat.AddChat(__instance, chatText);
            if (chatText.IndexOf("who", StringComparison.OrdinalIgnoreCase) >= 0)
                DestroyableSingleton<UnityTelemetry>.Instance.SendWho();
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpc(__instance.NetId, (byte)RpcCalls.SendChat, SendOption.None);
            messageWriter.Write(chatText);
            messageWriter.EndMessage();
            __result = true;
            return false;
        }
    }
}