using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Assets.CoreScripts;
using BepInEx.IL2CPP.UnityEngine;
using HarmonyLib;
using Hazel;
using Il2CppSystem.Runtime.Remoting.Messaging;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
    class ChatCommands
    {

        public static List<string> ChatHistory = new();

        public static bool ContainsStart(string text)
        {
            text = text.Trim();
            if (text == "Start") return true;
            if (text == "start") return true;
            if (text == "开") return true;
            if (text == "开始") return true;
            if (text == "开啊") return true;
            if (text == "开阿") return true;
            if (text == "kai") return true;
            if (text == "KAI") return true;
            if (text.Contains("started")) return false;
            if (text.Contains("starter")) return false;
            if (text.Contains("Starting")) return false;
            if (text.Contains("starting")) return false;
            if (text.Contains("beginner")) return false;
            if (text.Contains("beginned")) return false;
            if (text.Contains("了")) return false;
            if (text.Contains("吗")) return false;
            if (text.Contains("吧")) return false;
            if (text.Contains("哈")) return false;
            if (text.Contains("还")) return false;
            if (text.Contains("现")) return false;
            if (text.Contains("不")) return false;
            if (text.Contains("可")) return false;
            if (text.Contains("刚")) return false;
            if (text.Contains("的")) return false;
            if (text.Contains("打")) return false;
            if (text.Contains("门")) return false;
            if (text.Contains("关")) return false;
            if (text.Contains("怎")) return false;
            if (text.Contains("要")) return false;
            if (text.Contains("《")) return false;
            if (text.Contains("?")) return false;
            if (text.Contains("？")) return false;
            if (text.Length >= 3) return false;
            if (text.Contains("start")) return true;
            if (text.Contains("s t a r t")) return true;
            if (text.Contains("begin")) return true;
            if (text.Contains("开")) return true;
            if (text.Contains("kai")) return true;
            if (text.Contains("KAI")) return true;
            return false;
        }

        public static bool ProhibitedCheck(PlayerControl player, string text)
        {

            string name = player.GetRealName();

            if (Options.AutoKickStart.GetBool())
            {
                if (ContainsStart(text) && GameStates.IsLobby && player.PlayerId != PlayerControl.LocalPlayer.PlayerId)
                {
                    Utils.SendMessage($"{name} 被系统请离\n请不要催开始，可能会被判定为违规信息");
                    AmongUsClient.Instance.KickPlayer(player.GetClientId(), false);
                    Logger.Msg($"{name} 因催促开始被请离", "Blocked Word");
                    return true;
                }
            }

            if (!Options.AutoKickStopWords.GetBool() && !Options.AutoWarnStopWords.GetBool()) return false;

            var list = ReturnAllNewLinesInFile(Main.BANNEDWORDS_FILE_PATH, noErr: true);
            bool banned = false;
            var banedWord = "";

            foreach (var word in list)
            {
                if (word != null && text.Contains(word))
                {
                    banedWord = word;
                    banned = true;
                    break;
                }
            }

            if (banned && player.PlayerId != PlayerControl.LocalPlayer.PlayerId)
            {
                if (Options.AutoWarnStopWords.GetBool()) Utils.SendMessage($"{name} ，请友善讨论，杜绝脏话哦~");
                Logger.Msg($"{name} 触发违禁词： {banedWord}.", "Blocked Word");

                if (!Options.AutoKickStopWords.GetBool()) return true;
                Utils.SendMessage($"{name} 被踢出因其触发了违禁词");
                AmongUsClient.Instance.KickPlayer(player.GetClientId(), false);
                Logger.Msg($"{name} said a word blocked by this host. The blocked word was {banedWord}.", "Blocked Word");
                return true;
            }
            return false;
        }

        public static bool Prefix(ChatController __instance)
        {
            if (__instance.TextArea.text == "") return false;
            __instance.TimeSinceLastMessage = 3f;
            var text = __instance.TextArea.text;
            if (ProhibitedCheck(PlayerControl.LocalPlayer, text)) return false;
            if (ChatHistory.Count == 0 || ChatHistory[^1] != text) ChatHistory.Add(text);
            ChatControllerUpdatePatch.CurrentHistorySelection = ChatHistory.Count;
            string[] args = text.Split(' ');
            string subArgs = "";
            var canceled = false;
            var cancelVal = "";
            Main.isChatCommand = true;
            Logger.Info(text, "SendChat");
            if (text.Length >= 3) if (text[..2] == "/r" && text[..3] != "/rn") args[0] = "/r";
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

                    case "/rn":
                    case "/rename":
                        canceled = true;
                        Main.nickName = args.Length > 1 ? Main.nickName = args[1] : "";
                        break;

                    case "/hn":
                    case "/hidename":
                        canceled = true;
                        Main.HideName.Value = args.Length > 1 ? args.Skip(1).Join(delimiter: " ") : Main.HideName.DefaultValue.ToString();
                        GameStartManagerPatch.GameStartManagerStartPatch.HideName.text =
                            ColorUtility.TryParseHtmlString(Main.HideColor.Value, out _)
                                ? $"<color={Main.HideColor.Value}>{Main.HideName.Value}</color>"
                                : $"<color={Main.ModColor}>{Main.HideName.Value}</color>";
                        break;

                    case "/level":
                        canceled = true;
                        subArgs = args.Length < 2 ? "" : args[1];
                        Utils.SendMessage("您的等级设置为：" + subArgs, PlayerControl.LocalPlayer.PlayerId);
                        //nt32.Parse("-105");
                        var number = System.Convert.ToUInt32(subArgs);
                        PlayerControl.LocalPlayer.RpcSetLevel(number - 1);
                        break;

                    case "/n":
                    case "/now":
                        canceled = true;
                        if (Options.DIYGameSettings.GetBool())
                        {
                            Utils.SendMessage(GetString("Message.NowOverrideText"), PlayerControl.LocalPlayer.PlayerId);
                            break;
                        }
                        else
                        {
                            subArgs = args.Length < 2 ? "" : args[1];
                            switch (subArgs)
                            {
                                case "r":
                                case "roles":
                                    Utils.ShowActiveRoles(PlayerControl.LocalPlayer.PlayerId);
                                    break;

                                default:
                                    Utils.ShowActiveSettings(PlayerControl.LocalPlayer.PlayerId);
                                    break;
                            }
                        }
                        break;

                    case "/dis":
                    case "/disconnect":
                        canceled = true;
                        subArgs = args.Length < 2 ? "" : args[1];
                        switch (subArgs)
                        {
                            case "crew":
                                GameManager.Instance.enabled = false;
                                GameManager.Instance.RpcEndGame(GameOverReason.HumansDisconnect, false);
                                break;

                            case "imp":
                                GameManager.Instance.enabled = false;
                                GameManager.Instance.RpcEndGame(GameOverReason.ImpostorDisconnect, false);
                                break;

                            default:
                                __instance.AddChat(PlayerControl.LocalPlayer, "crew | imp");
                                cancelVal = "/dis";
                                break;
                        }
                        ShipStatus.Instance.RpcRepairSystem(SystemTypes.Admin, 0);
                        break;

                    case "/r":
                        canceled = true;
                        subArgs = text.Remove(0, 2);
                        GetRolesInfo(subArgs, PlayerControl.LocalPlayer);
                        break;
                    case "/roles":
                        canceled = true;
                        subArgs = text.Remove(0, 6);
                        GetRolesInfo(subArgs, PlayerControl.LocalPlayer);
                        break;

                    case "/h":
                    case "/help":
                        canceled = true;
                        Utils.ShowHelp(PlayerControl.LocalPlayer.PlayerId);
                        break;

                    case "/m":
                    case "/myrole":
                        canceled = true;
                        var role = PlayerControl.LocalPlayer.GetCustomRole();
                        if (GameStates.IsInGame)
                            HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, GetString(role.ToString()) + PlayerControl.LocalPlayer.GetRoleInfo(true));
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
                        if (args.Length > 1 && int.TryParse(args[1], out int sec))
                        {
                            Main.MessageWait.Value = sec;
                            Utils.SendMessage(string.Format(GetString("Message.SetToSeconds"), sec), 0);
                        }
                        else Utils.SendMessage($"{GetString("Message.MessageWaitHelp")}\n{GetString("ForExample")}:\n{args[0]} 3", 0);
                        break;

                    case "/say":
                    case "/s":
                        canceled = true;
                        if (args.Length > 1)
                            Utils.SendMessage(args.Skip(1).Join(delimiter: " "), title: $"<color=#ff0000>{GetString("MessageFromTheHost")}</color>");
                        break;

                    case "/exe":
                        canceled = true;
                        if (GameStates.IsLobby)
                        {
                            Utils.SendMessage("准备阶段无法使用执行指令", PlayerControl.LocalPlayer.PlayerId);
                            break;
                        }
                        if (args.Length < 2 || !int.TryParse(args[1], out int id)) break;
                        Utils.GetPlayerById(id)?.RpcExileV2();
                        break;

                    case "/kill":
                        canceled = true;
                        if (GameStates.IsLobby)
                        {
                            Utils.SendMessage("准备阶段无法使用击杀指令", PlayerControl.LocalPlayer.PlayerId);
                            break;
                        }
                        if (args.Length < 2 || !int.TryParse(args[1], out int id2)) break;
                        Utils.GetPlayerById(id2)?.RpcMurderPlayer(Utils.GetPlayerById(id2));
                        break;

                    case "/colour":
                    case "/color":
                        canceled = true;
                        subArgs = args.Length < 2 ? "" : args[1];
                        var color = Utils.MsgToColor(subArgs);
                        if (color == Convert.ToByte(99))
                        {
                            Utils.SendMessage(GetString("IllegalColor"), PlayerControl.LocalPlayer.PlayerId);
                            break;
                        }
                        PlayerControl.LocalPlayer.RpcSetColor(color);
                        Utils.SendMessage("颜色设置为：" + subArgs, PlayerControl.LocalPlayer.PlayerId);
                        break;

                    case "/quit":
                    case "/qt":
                        Utils.SendMessage("很抱歉，房主无法使用该指令", PlayerControl.LocalPlayer.PlayerId);
                        break;

                    default:
                        Main.isChatCommand = false;
                        break;
                }
            }
            if (canceled)
            {
                Logger.Info("Command Canceled", "ChatCommand");
                __instance.TextArea.Clear();
                __instance.TextArea.SetText(cancelVal);
                __instance.quickChatMenu.ResetGlyphs();
            }
            return !canceled;
        }

        public static string ToSimplified(string text)
        {
            switch (text)
            {
                case "管理員": case "管理": return "管理员";
                case "賞金獵人": case "赏金": return "赏金猎人";
                case "邪惡的追踪者": case "邪恶追踪者": return "邪恶的追踪者";
                case "煙花商人": case "烟花": return "烟花商人";
                case "夢魘": return "梦魇";
                case "黑手黨": return "黑手党";
                case "嗜血殺手": case "嗜血": return "嗜血杀手";
                case "蝕時者": case "蚀时": return "蚀时者";
                case "狙擊手": case "狙击": return "狙击手";
                case "傀儡師": case "傀儡": return "傀儡师";
                case "吸血鬼": case "吸血": return "吸血鬼";
                case "術士": return "术士";
                case "駭客": case "黑客": return "骇客";
                case "忍者": return "忍者";
                case "礦工": return "矿工";
                case "女巫": return "女巫";
                case "背叛的守衛": case "背叛守卫": return "背叛的守卫";
                case "叛徒": return "叛徒";
                case "背叛的告密者": case "背叛告密": return "背叛的告密者";
                case "叛徒跟班": return "叛徒跟班";
                case "窺視者": case "窥视": return "窥视者";
                case "誘餌": case "大奖": case "头奖": return "诱饵";
                case "擺爛人": case "摆烂": return "摆烂人";
                case "獨裁者": case "独裁": return "独裁者";
                case "醫生": return "医生";
                case "執燈人": case "执灯": case "灯人": return "执灯人";
                case "大明星": case "明星": return "大明星";
                case "工程師": case "工程": return "工程师";
                case "市長": return "市长";
                case "被害妄想症": case "被害妄想": case "被迫害妄想症": case "被害": case "妄想": case "妄想症": return "被害妄想症";
                case "愚者": case "愚": return "愚者";
                case "修理大師": case "修理大师": case "维修大师": return "修理大师";
                case "靈媒": return "灵媒";
                case "警長": return "警长";
                case "告密者": case "告密": return "告密者";
                case "增速者": case "增速": return "增速者";
                case "陷阱師": case "陷阱": case "小奖": return "陷阱师";
                case "縱火犯": case "纵火": return "纵火犯";
                case "野心家": case "野心": return "野心家";
                case "處刑人": case "处刑": return "处刑人";
                case "小丑": return "小丑";
                case "投機者": case "投机": return "投机者";
                case "薛定諤的貓": case "薛定谔猫": case "猫": return "薛定谔的猫";
                case "恐怖分子": case "恐怖": return "恐怖分子";
                case "豺狼": return "豺狼";
                case "情人": case "愛人": case "链子": return "恋人";
                case "狐狸": return "狐狸";
                case "巨魔": return "巨魔";
                default: return text;
            }

        }
        public static void GetRolesInfo(string role, PlayerControl player)
        {
            var roleList = new Dictionary<CustomRoles, string>
            {
                //GM
                { CustomRoles.GM, "管理员" },
                //Impostor役職
                { (CustomRoles)(-1), $"== {GetString("Impostor")} ==" }, //区切り用
                { CustomRoles.BountyHunter, "赏金猎人" },
                { CustomRoles.EvilTracker,"邪恶的追踪者" },
                { CustomRoles.FireWorks, "烟花商人" },
                { CustomRoles.Mare, "梦魇" },
                { CustomRoles.Mafia, "黑手党" },
                { CustomRoles.SerialKiller, "嗜血杀手" },
                //{ CustomRoles.ShapeMaster, "sha" },
                { CustomRoles.TimeThief, "蚀时者"},
                { CustomRoles.Sniper, "狙击手" },
                { CustomRoles.Puppeteer, "傀儡师" },
                { CustomRoles.Vampire, "吸血鬼" },
                { CustomRoles.Warlock, "术士" },
                { CustomRoles.Assassin, "忍者" },
                { CustomRoles.Hacker, "骇客" },
                { CustomRoles.Miner, "矿工" },
                { CustomRoles.Witch, "女巫" },
                //Madmate役職
                { (CustomRoles)(-2), $"== {GetString("Madmate")} ==" }, //区切り用
                { CustomRoles.MadGuardian, "背叛的守卫" },
                { CustomRoles.Madmate, "叛徒" },
                { CustomRoles.MadSnitch, "背叛的告密者" },
                { CustomRoles.SKMadmate, "叛徒跟班" },
                //両陣営役職
                { (CustomRoles)(-3), $"== {GetString("Impostor")} or {GetString("Crewmate")} ==" }, //区切り用
                { CustomRoles.Watcher, "窥视者" },
                //Crewmate役職
                { (CustomRoles)(-4), $"== {GetString("Crewmate")} ==" }, //区切り用
                { CustomRoles.Bait, "诱饵" },
                { CustomRoles.Needy, "摆烂人" },
                { CustomRoles.Dictator, "独裁者" },
                { CustomRoles.Doctor, "医生" },
                { CustomRoles.Lighter, "执灯人" },
                { CustomRoles.SuperStar, "大明星" },
                { CustomRoles.Plumber, "工程师" },
                { CustomRoles.Mayor, "市长" },
                { CustomRoles.Paranoia, "被害妄想症" },
                { CustomRoles.Psychic, "愚者" },
                { CustomRoles.SabotageMaster, "修理大师" },
                { CustomRoles.Seer,"灵媒" },
                { CustomRoles.Sheriff, "警长" },
                { CustomRoles.Snitch, "告密者" },
                { CustomRoles.SpeedBooster, "增速者" },
                { CustomRoles.Trapper, "陷阱师" },
                //Neutral役職
                { (CustomRoles)(-5), $"== {GetString("Neutral")} ==" }, //区切り用
                { CustomRoles.Arsonist, "纵火犯" },
                { CustomRoles.Egoist, "野心家" },
                { CustomRoles.Executioner, "处刑人" },
                { CustomRoles.Jester, "小丑" },
                { CustomRoles.Opportunist, "投机者" },
                { CustomRoles.SchrodingerCat, "薛定谔的猫" },
                { CustomRoles.Terrorist, "恐怖分子" },
                { CustomRoles.Jackal, "豺狼" },
                //属性
                { (CustomRoles)(-6), $"== {GetString("Addons")} ==" }, //区切り用
                {CustomRoles.Lovers, "恋人" },
                //HAS
                { (CustomRoles)(-7), $"== {GetString("HideAndSeek")} ==" }, //区切り用
                { CustomRoles.HASFox, "狐狸" },
                { CustomRoles.HASTroll, "巨魔" },

            };

            role = role.Trim();
            if (role == "" || role == String.Empty)
            {
                Utils.SendMessage("指令格式：/r [职业]\n例如：/r 灵媒", player.PlayerId);
                return;
            }
            role = ToSimplified(role);

            var msg = "";
            var rolemsg = $"{GetString("Command.h_args")}";
            foreach (var r in roleList)
            {
                var roleName = r.Key.ToString();
                var roleShort = r.Value;

                if (String.Compare(role, roleName, true) == 0 || String.Compare(role, roleShort, true) == 0)
                {
                    Utils.SendMessage(GetString(roleName) + GetString($"{roleName}InfoLong"), player.PlayerId);
                    return;
                }

                var roleText = $"{roleName.ToLower()}({roleShort.ToLower()}), ";
                if ((int)r.Key < 0)
                {
                    msg += rolemsg + "\n" + roleShort + "\n";
                    rolemsg = "";
                }
                else if ((rolemsg.Length + roleText.Length) > 40)
                {
                    msg += rolemsg + "\n";
                    rolemsg = roleText;
                }
                else
                {
                    rolemsg += roleText;
                }
            }

            Utils.SendMessage("请正确拼写您要查询的职业哦~\n查看所有职业请输入/n", player.PlayerId);
            return;
        }
        public static void OnReceiveChat(PlayerControl player, string text)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (ProhibitedCheck(player, text)) return;
            string[] args = text.Split(' ');
            string subArgs = "";
            if (text.Length >= 3) if (text[..2] == "/r" && text[..3] != "/rn") args[0] = "/r";
            switch (args[0])
            {
                case "/l":
                case "/lastresult":
                    Utils.ShowLastResult(player.PlayerId);
                    break;

                case "/n":
                case "/now":
                    if (Options.DIYGameSettings.GetBool())
                    {
                        Utils.SendMessage(GetString("Message.NowOverrideText"), player.PlayerId);
                        break;
                    }
                    else
                    {
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
                    }
                    break;

                case "/r":
                    subArgs = text.Remove(0, 2);
                    GetRolesInfo(subArgs, player);
                    break;
                case "/roles":
                    subArgs = text.Remove(0, 6);
                    GetRolesInfo(subArgs, player);
                    break;

                case "/h":
                case "/help":
                    subArgs = args.Length < 2 ? "" : args[1];
                    Utils.ShowHelpToClient(player.PlayerId);
                    break;

                case "/m":
                case "/myrole":
                    var role = player.GetCustomRole();
                    if (GameStates.IsInGame)
                        Utils.SendMessage(GetString(role.ToString()) + player.GetRoleInfo(true), player.PlayerId);
                    break;

                case "/t":
                case "/template":
                    if (args.Length > 1) TemplateManager.SendTemplate(args[1], player.PlayerId);
                    else Utils.SendMessage($"{GetString("ForExample")}:\n{args[0]} test", player.PlayerId);
                    break;

                case "/colour":
                case "/color":
                    if (Options.PlayerCanSerColor.GetBool())
                    {
                        subArgs = args.Length < 2 ? "" : args[1];
                        var color = Utils.MsgToColor(subArgs);
                        if (color == Convert.ToByte(99))
                        {
                            Utils.SendMessage(GetString("IllegalColor"), PlayerControl.LocalPlayer.PlayerId);
                            break;
                        }
                        PlayerControl.LocalPlayer.RpcSetColor(color);
                        Utils.SendMessage("颜色设置为：" + subArgs, player.PlayerId);
                    }
                    else
                    {
                        Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId);
                    }
                    break;

                case "/quit":
                case "/qt":
                    subArgs = args.Length < 2 ? "" : args[1];
                    if (subArgs.Equals("sure"))
                    {
                        string name = player.GetRealName();
                        Utils.SendMessage($"{name} 选择自愿离开\n很抱歉给大家带来了糟糕的游戏体验\n我们真的很努力地在进步了");
                        AmongUsClient.Instance.KickPlayer(player.GetClientId(), true);
                    }
                    else
                    {
                        Utils.SendMessage(GetString("SureUse.quit"), player.PlayerId);
                    }
                    break;

                default:
                    break;
            }
        }

        public static List<string> ReturnAllNewLinesInFile(string filename, byte playerId = 0xff, bool noErr = false)
        {
            // Logger.Info($"Checking lines in directory {filename}.", "ReturnAllNewLinesInFile (ChatCommands)");
            if (!File.Exists(filename))
            {
                HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, $"No {filename} file found.");
                File.WriteAllText(filename, "Enter the desired stuff here.");
                return new List<string>();
            }
            using StreamReader sr = new(filename, Encoding.GetEncoding("UTF-8"));
            string text;
            string[] tmp = { };
            List<string> sendList = new();
            HashSet<string> tags = new();
            while ((text = sr.ReadLine()) != null)
            {
                if (text.Length > 1 && text != "")
                {
                    tags.Add(text.ToLower());
                    sendList.Add(text.Join(delimiter: "").Replace("\\n", "\n").ToLower());
                }
            }
            if (sendList.Count == 0 && !noErr)
            {
                if (playerId == 0xff)
                    HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, string.Format(GetString("Message.TemplateNotFoundHost"), Main.BANNEDWORDS_FILE_PATH, tags.Join(delimiter: ", ")));
                else Utils.SendMessage(string.Format(GetString("Message.TemplateNotFoundClient"), Main.BANNEDWORDS_FILE_PATH), playerId);
                return new List<string>();
            }
            else
            {
                return sendList;
            }
        }
    }
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
    class ChatUpdatePatch
    {
        public static bool DoBlockChat = false;
        public static void Postfix(ChatController __instance)
        {
            if (!AmongUsClient.Instance.AmHost || Main.MessagesToSend.Count < 1 || (Main.MessagesToSend[0].Item2 == byte.MaxValue && Main.MessageWait.Value > __instance.TimeSinceLastMessage)) return;
            if (DoBlockChat) return;
            var player = Main.AllAlivePlayerControls.OrderBy(x => x.PlayerId).FirstOrDefault();
            if (player == null) return;
            (string msg, byte sendTo, string title) = Main.MessagesToSend[0];
            Main.MessagesToSend.RemoveAt(0);
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
            __instance.TimeSinceLastMessage = 0f;
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
                DestroyableSingleton<Telemetry>.Instance.SendWho();
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpc(__instance.NetId, (byte)RpcCalls.SendChat, SendOption.None);
            messageWriter.Write(chatText);
            messageWriter.EndMessage();
            __result = true;
            return false;
        }
    }
}