using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Assets.CoreScripts;
using HarmonyLib;
using Hazel;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
    class ChatCommands
    {
        public static List<string> ChatHistory = new();

        public static bool MafiaMsgCheck(PlayerControl pc, string msg)
        {
            if (!AmongUsClient.Instance.AmHost) return false;
            msg = msg.Trim();
            if (msg.Length < 3 || msg[..3] != "/rv") return false;
            if (!pc.Is(CustomRoles.Mafia)) return false;
            if (Options.MafiaCanKillNum.GetInt() < 1)
            {
                Utils.SendMessage(GetString("MafiaKillDisable"), pc.PlayerId);
                return true;
            }
            if (msg == "/rv")
            {
                string text = "玩家编号：";
                foreach (var npc in PlayerControl.AllPlayerControls)
                {
                    if (npc.Data.IsDead) continue;
                    text += "\n" + npc.PlayerId.ToString() + " → (" + npc.GetDisplayRoleName() + ") " + npc.GetRealName();
                }
                Utils.SendMessage(text, pc.PlayerId);
                return true;
            }
            if (Main.MafiaRevenged.ContainsKey(pc.PlayerId))
            {

                if (Main.MafiaRevenged[pc.PlayerId] >= Options.MafiaCanKillNum.GetInt())
                {
                    Utils.SendMessage(GetString("MafiaKillMax"), pc.PlayerId);
                    return true;
                }
            }
            else
            {
                Main.MafiaRevenged.Add(pc.PlayerId, 0);
            }

            if (!pc.Data.IsDead)
            {
                Utils.SendMessage(GetString("MafiaAliveKill"), pc.PlayerId);
                return true;
            }

            int targetId;
            PlayerControl target;
            try
            {
                targetId = int.Parse(msg.Replace("/rv", String.Empty));
                target = Utils.GetPlayerById(targetId);
            }
            catch
            {
                Utils.SendMessage(GetString("MafiaKillDead"), pc.PlayerId);
                return true;
            }

            if (target == null || target.Data.IsDead)
            {
                Utils.SendMessage(GetString("MafiaKillDead"), pc.PlayerId);
                return true;
            }

            target.SetRealKiller(pc);
            target.RpcMurderPlayer(target);
            Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Revenge;
            Main.PlayerStates[target.PlayerId].SetDead();
            Main.MafiaRevenged[pc.PlayerId]++;
            string Name = target.GetRealName();
            foreach (var cpc in Main.AllPlayerControls)
            {
                cpc.RpcSetNameEx(cpc.GetRealName(isMeeting: true));
            }
            ChatUpdatePatch.DoBlockChat = false;
            Utils.NotifyRoles(isMeeting: true, NoCache: true);
            Utils.SendMessage(Name + " " + GetString("MafiaKillSucceed"), pc.PlayerId);
            return true;
        }

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
                if (Options.AutoWarnStopWords.GetBool()) Utils.SendMessage($"{name} ，请友善讨论哦~");
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
            if (MafiaMsgCheck(PlayerControl.LocalPlayer, text)) return false;
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
                        var number = Convert.ToUInt32(subArgs);
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
                        SendRolesInfo(subArgs, PlayerControl.LocalPlayer);
                        break;
                    case "/roles":
                        canceled = true;
                        subArgs = text.Remove(0, 6);
                        SendRolesInfo(subArgs, PlayerControl.LocalPlayer);
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
                        canceled = true;
                        Utils.SendMessage("很抱歉，房主无法使用该指令", PlayerControl.LocalPlayer.PlayerId);
                        break;

                    case "/xf":
                        canceled = true;
                        foreach (var pc in Main.AllPlayerControls)
                        {
                            pc.RpcSetNameEx(pc.GetRealName(isMeeting: true));
                        }
                        ChatUpdatePatch.DoBlockChat = false;
                        Utils.NotifyRoles(isMeeting: true, NoCache: true);
                        Utils.SendMessage("已尝试修复名字遮挡", PlayerControl.LocalPlayer.PlayerId);
                        break;

                    case "/id":
                        canceled = true;
                        string msgText = "玩家编号列表：";
                        foreach (var pc in PlayerControl.AllPlayerControls)
                        {
                            msgText += "\n" + pc.PlayerId.ToString() + " → " + pc.GetRealName();
                        }
                        Utils.SendMessage(msgText, PlayerControl.LocalPlayer.PlayerId);
                        break;
                    case "/guesslist":
                        canceled = true;
                        if (!PlayerControl.LocalPlayer.IsAlive())
                        {
                            Utils.SendMessage("死亡后不能赌注", PlayerControl.LocalPlayer.PlayerId);
                            break;
                        }
                        if (GameStates.IsLobby)
                        {
                            Utils.SendMessage("准备阶段无法使用", PlayerControl.LocalPlayer.PlayerId);
                            break;
                        }
                        if (!GuessManager.isGuesser(PlayerControl.LocalPlayer.PlayerId)) break;
                        Utils.SendMessage(GuessManager.getFormatString(), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    case "/guess":
                        canceled = true;
                        if (!PlayerControl.LocalPlayer.IsAlive())
                        {
                            Utils.SendMessage("死亡后不能赌注", PlayerControl.LocalPlayer.PlayerId);
                            break;
                        }
                        if (GameStates.IsLobby)
                        {
                            Utils.SendMessage("准备阶段无法使用", PlayerControl.LocalPlayer.PlayerId);
                            break;
                        }
                        if (!GuessManager.isGuesser(PlayerControl.LocalPlayer.PlayerId)) break;
                        if (args.Length < 3) break;
                        var i = int.Parse(args[1]);
                        CustomRoles customRole = getRoleByName(args[2]);

                        PlayerControl typePlayer = GuessManager.GetPlayerByNum(i);

                        if (!typePlayer.IsAlive())
                        {
                            Utils.SendMessage("猜测对象已经死亡", PlayerControl.LocalPlayer.PlayerId);
                            break;
                        }

                        PlayerControl diedPlayer;

                        if (customRole.IsCrewmate() && GuessManager.isGood(PlayerControl.LocalPlayer.PlayerId))
                        {
                            Utils.SendMessage("你只能猜测非船员阵营", PlayerControl.LocalPlayer.PlayerId);
                            break;
                        }

                        if (GuessManager.isRealRole(typePlayer.PlayerId, customRole))
                        {
                            // typePlayer.RpcExileV2();
                            typePlayer?.RpcMurderPlayer(typePlayer);
                            diedPlayer = typePlayer;
                        }
                        else
                        {
                            // PlayerControl.LocalPlayer.RpcExileV2();
                            PlayerControl.LocalPlayer?.RpcMurderPlayer(PlayerControl.LocalPlayer);
                            diedPlayer = PlayerControl.LocalPlayer;
                        }

                        Utils.SendMessage(diedPlayer.GetRealName() + " 在赌局中失利");
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
            return text switch
            {
                "管理員" or "管理" => "管理员",
                "賞金獵人" or "赏金" => "赏金猎人",
                "邪惡的追踪者" or "邪恶追踪者" => "邪恶的追踪者",
                "煙花商人" or "烟花" => "烟花商人",
                "夢魘" => "梦魇",
                "黑手黨" or "黑手" => "黑手党",
                "嗜血殺手" or "嗜血" => "嗜血杀手",
                "蝕時者" or "蚀时" => "蚀时者",
                "狙擊手" or "狙击" => "狙击手",
                "傀儡師" or "傀儡" => "傀儡师",
                "吸血鬼" or "吸血" => "吸血鬼",
                "術士" => "术士",
                "駭客" or "黑客" => "骇客",
                "刺客" => "刺客",
                "礦工" => "矿工",
                "逃逸者" or "逃逸" => "逃逸者",
                "女巫" => "女巫",
                "背叛的守衛" or "背叛守卫" => "背叛的守卫",
                "叛徒" => "叛徒",
                "背叛的告密者" or "背叛告密" => "背叛的告密者",
                "叛徒跟班" => "叛徒跟班",
                "窺視者" or "窥视" => "窥视者",
                "誘餌" or "大奖" or "头奖" => "诱饵",
                "擺爛人" or "摆烂" => "摆烂人",
                "獨裁者" or "独裁" => "独裁者",
                "醫生" => "医生",
                "執燈人" or "执灯" or "灯人" => "执灯人",
                "幸運兒" or "幸运" => "幸运儿",
                "大明星" or "明星" => "大明星",
                "網紅" => "网红",
                "俠客" => "侠客",
                "正義賭怪" => "正义赌怪",
                "邪惡賭怪" => "邪恶赌怪",
                "工程師" or "工程" => "工程师",
                "市長" => "市长",
                "被害妄想症" or "被害妄想" or "被迫害妄想症" or "被害" or "妄想" or "妄想症" => "被害妄想症",
                "愚者" or "愚" => "愚者",
                "修理大師" or "修理大师" or "维修大师" => "修理大师",
                "靈媒" => "灵媒",
                "警長" => "警长",
                "告密者" or "告密" => "告密者",
                "增速者" or "增速" => "增速者",
                "陷阱師" or "陷阱" or "小奖" => "陷阱师",
                "縱火犯" or "纵火" => "纵火犯",
                "野心家" or "野心" => "野心家",
                "處刑人" or "处刑" => "处刑人",
                "小丑" => "小丑",
                "投機者" or "投机" => "投机者",
                "薛定諤的貓" or "薛定谔猫" or "猫" => "薛定谔的猫",
                "恐怖分子" or "恐怖" => "恐怖分子",
                "豺狼" => "豺狼",
                "情人" or "愛人" or "链子" => "恋人",
                "狐狸" => "狐狸",
                "巨魔" => "巨魔",
                _ => text,
            };
        }

        private static readonly Dictionary<CustomRoles, string> roleList = new()
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
                { CustomRoles.Assassin, "刺客" },
                { CustomRoles.Hacker, "骇客" },
                { CustomRoles.Miner, "矿工" },
                { CustomRoles.Escapee, "逃逸者" },
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
                { CustomRoles.Luckey, "幸运儿" },
                { CustomRoles.Needy, "摆烂人" },
                { CustomRoles.Dictator, "独裁者" },
                { CustomRoles.Doctor, "医生" },
                { CustomRoles.Lighter, "执灯人" },
                { CustomRoles.SuperStar, "大明星" },
                { CustomRoles.CyberStar, "网红" },
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
                { CustomRoles.ChivalrousExpert, "侠客" },
                { CustomRoles.NiceGuesser, "正义赌怪" },
                { CustomRoles.EvilGuesser, "邪恶赌怪" },
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

        private static CustomRoles getRoleByName(string name) {
            name = name.Trim();
            if (name == "" || name == String.Empty)
            {
                return CustomRoles.Crewmate;
            }

            name = ToSimplified(name);

            foreach (var r in roleList) {
                var roleName = r.Key.ToString();
                var roleShort = r.Value;

                if (String.Compare(name, roleName, true) == 0 || String.Compare(name, roleShort, true) == 0)
                {
                    return r.Key;
                }
            }

            return CustomRoles.Crewmate;
        }

    public static void SendRolesInfo(string role, PlayerControl player)
        {
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
            if (MafiaMsgCheck(player, text)) return;
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
                    SendRolesInfo(subArgs, player);
                    break;
                case "/roles":
                    subArgs = text.Remove(0, 6);
                    SendRolesInfo(subArgs, player);
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
                        player.RpcSetColor(color);
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

                case "/xf":
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        pc.RpcSetNameEx(pc.GetRealName(isMeeting: true));
                    }
                    ChatUpdatePatch.DoBlockChat = false;
                    Utils.NotifyRoles(isMeeting: true, NoCache: true);
                    Utils.SendMessage("已尝试修复名字遮挡", PlayerControl.LocalPlayer.PlayerId);
                    break;
                case "/guesslist":
                    if (GameStates.IsLobby)
                    {
                        Utils.SendMessage("准备阶段无法使用", player.PlayerId);
                        break;
                    }
                    if (!player.IsAlive())
                    {
                        Utils.SendMessage("死亡后不能赌注", player.PlayerId);
                        break;
                    }
                    if (!GuessManager.isGuesser(player.PlayerId)) break;
                    Utils.SendMessage(GuessManager.getFormatString(), player.PlayerId);
                    break;
                case "/guess":
                    if (GameStates.IsLobby)
                    {
                        Utils.SendMessage("准备阶段无法使用", player.PlayerId);
                        break;
                    }
                    if (!player.IsAlive()) {
                        Utils.SendMessage("死亡后不能赌注", player.PlayerId);
                        break;
                    }
                    if (!GuessManager.isGuesser(player.PlayerId)) break;
                    if (args.Length < 3) break;
                    var i = int.Parse(args[1]);
                    CustomRoles customRole = getRoleByName(args[2]);

                    PlayerControl typePlayer = GuessManager.GetPlayerByNum(i);

                    PlayerControl diedPlayer;

                    if (customRole.IsCrewmate() && GuessManager.isGood(player.PlayerId))
                    {
                        Utils.SendMessage("你只能猜测非船员阵营", player.PlayerId);
                        break;
                    }
                    else if (customRole.IsImpostor() && !GuessManager.isGood(player.PlayerId)) {
                        Utils.SendMessage("你只能猜测非伪装者阵营", player.PlayerId);
                        break;
                    }

                    if (!typePlayer.IsAlive()) {
                        Utils.SendMessage("猜测对象已经死亡", player.PlayerId);
                        break;
                    }

                    if (GuessManager.isRealRole(typePlayer.PlayerId, customRole))
                    {
                        typePlayer?.RpcMurderPlayer(typePlayer);
                        diedPlayer = typePlayer;
                    }
                    else {
                        player?.RpcMurderPlayer(player);
                        diedPlayer = player;
                    }

                    Utils.SendMessage(diedPlayer.GetRealName() + " 在赌局中失利");
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