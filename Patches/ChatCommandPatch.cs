using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
            if (!GameStates.IsInGame || pc == null) return false;
            if (!pc.Is(CustomRoles.Mafia)) return false;
            msg = msg.Trim().ToLower();
            if (msg.Length < 3 || msg[..3] != "/rv") return false;
            if (Options.MafiaCanKillNum.GetInt() < 1)
            {
                Utils.SendMessage(GetString("MafiaKillDisable"), pc.PlayerId);
                return true;
            }

            if (!pc.Data.IsDead)
            {
                Utils.SendMessage(GetString("MafiaAliveKill"), pc.PlayerId);
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

            string Name = target.GetRealName();
            Utils.SendMessage(Name + " " + GetString("MafiaKillSucceed"), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mafia), " ★ 特供情报 ★ "));

            new LateTask (() =>
            {
            target.SetRealKiller(pc);
            target.RpcMurderPlayer(target);
            Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Revenge;
            Main.PlayerStates[target.PlayerId].SetDead();
            Main.MafiaRevenged[pc.PlayerId]++;
            foreach (var cpc in Main.AllPlayerControls)
            {
                RPC.PlaySoundRPC(cpc.PlayerId, Sounds.KillSound);
                cpc.RpcSetNameEx(cpc.GetRealName(isMeeting: true));
            }
            ChatUpdatePatch.DoBlockChat = false;
            Utils.NotifyRoles(isMeeting: true, NoCache: true);
            }, 0.9f, "Mafia Kill");
            return true;
        }

        public static bool ContainsStart(string text)
        {
            text = text.Trim().ToLower();

            int stNum = 0;
            for (int i = 0 ; i < text.Length; i++)
            {
                if (text[i..].Equals("k")) stNum++;
                if (text[i..].Equals("开")) stNum++;
            }
            if (stNum >= 3) return true;

            if (text == "Start") return true;
            if (text == "start") return true;
            if (text == "开") return true;
            if (text == "快开") return true;
            if (text == "开始") return true;
            if (text == "开啊") return true;
            if (text == "开阿") return true;
            if (text == "kai") return true;
            if (text == "kaishi") return true;
            if (text.Contains("started")) return false;
            if (text.Contains("starter")) return false;
            if (text.Contains("Starting")) return false;
            if (text.Contains("starting")) return false;
            if (text.Contains("beginner")) return false;
            if (text.Contains("beginned")) return false;
            if (text.Contains("了")) return false;
            if (text.Contains("没")) return false;
            if (text.Contains("吗")) return false;
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
            if (text.Contains("摆")) return false;
            if (text.Contains("啦")) return false;
            if (text.Contains("咯")) return false;
            if (text.Contains("嘞")) return false;
            if (text.Contains("勒")) return false;
            if (text.Contains("心")) return false;
            if (text.Contains("呢")) return false;
            if (text.Contains("门")) return false;
            if (text.Contains("总")) return false;
            if (text.Contains("哥")) return false;
            if (text.Contains("姐")) return false;
            if (text.Contains("《")) return false;
            if (text.Contains("?")) return false;
            if (text.Contains("？")) return false;
            if (text.Length >= 3) return false;
            if (text.Contains("start")) return true;
            if (text.Contains("s t a r t")) return true;
            if (text.Contains("begin")) return true;
            if (text.Contains("开")) return true;
            if (text.Contains("kai")) return true;
            return false;
        }

        public static bool ProhibitedCheck(PlayerControl player, string text)
        {
            if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId) return false;
            string name = player.GetRealName();
            bool kick = false;
            string msg = "";

            if (Options.AutoKickStart.GetBool())
            {
                if (ContainsStart(text) && GameStates.IsLobby)
                {
                    msg = $"【{name}】被系统请离\n请不要催开始，可能会被判定为违规信息";
                    if (Options.AutoKickStart.GetBool())
                    {
                        if (!Main.SayStartTimes.ContainsKey(player.GetClientId())) Main.SayStartTimes.Add(player.GetClientId(), 0);
                        Main.SayStartTimes[player.GetClientId()]++;
                        msg = $"【{name}】被警告：{Main.SayStartTimes[player.GetClientId()]}次\n请不要催开始，可能会被判定为违规信息";
                        if (Main.SayStartTimes[player.GetClientId()] > Options.AutoKickStartTimes.GetInt())
                        {
                            msg = $"【{name}】达到 {Main.SayStartTimes[player.GetClientId()]} 次警告被请离房间\n请不要催开始，可能会被判定为违规信息";
                            kick = true;
                        }
                    }
                    if (msg != "") Utils.SendMessage(msg);
                    if (kick) AmongUsClient.Instance.KickPlayer(player.GetClientId(), Options.AutoKickStartAsBan.GetBool());
                    return true;
                }
            }

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
            if (!banned) return false;

            if (Options.AutoWarnStopWords.GetBool()) msg = $"【{name}】，请友善讨论哦~";
            if (Options.AutoKickStopWords.GetBool())
            {
                if (!Main.SayBanwordsTimes.ContainsKey(player.GetClientId())) Main.SayBanwordsTimes.Add(player.GetClientId(), 0);
                Main.SayBanwordsTimes[player.GetClientId()]++;
                msg = $"【{name}】被警告：{Main.SayBanwordsTimes[player.GetClientId()]}次\n请友善讨论哦~";
                if (Main.SayBanwordsTimes[player.GetClientId()] > Options.AutoKickStopWordsTimes.GetInt())
                {
                    msg = $"【{name}】达到 {Main.SayBanwordsTimes[player.GetClientId()]} 次警告被请离房间\n请友善讨论哦~";
                    kick = true;
                }
            }

            if (msg != "")
            {
                if (kick || !GameStates.IsInGame) Utils.SendMessage(msg);
                else
                {
                    foreach (var pc in PlayerControl.AllPlayerControls)
                    {
                        if (pc != null && pc.IsAlive() == player.IsAlive()) Utils.SendMessage(msg, pc.PlayerId);
                    }
                }
            }
            if (kick) AmongUsClient.Instance.KickPlayer(player.GetClientId(), Options.AutoKickStopWordsAsBan.GetBool());
            return true;
        }

        public static bool Prefix(ChatController __instance)
        {
            if (__instance.TextArea.text == "") return false;
            __instance.TimeSinceLastMessage = 3f;
            var text = __instance.TextArea.text;
            if (ChatHistory.Count == 0 || ChatHistory[^1] != text) ChatHistory.Add(text);
            ChatControllerUpdatePatch.CurrentHistorySelection = ChatHistory.Count;
            string[] args = text.Split(' ');
            string subArgs = "";
            var canceled = false;
            var cancelVal = "";
            Main.isChatCommand = true;
            Logger.Info(text, "SendChat");
            if (text.Length >= 3) if (text[..2] == "/r" && text[..3] != "/rn") args[0] = "/r";
            if (GuessManager.GuesserMsg(PlayerControl.LocalPlayer, text)) goto Canceled;
            if (MafiaMsgCheck(PlayerControl.LocalPlayer, text)) goto Canceled;
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
                        int.TryParse(subArgs, out int input);
                        if (input is < 1 or > 100)
                        {
                            Utils.SendMessage("等级可设置范围：0 - 100", PlayerControl.LocalPlayer.PlayerId);
                            break;
                        }
                        var number = Convert.ToUInt32(input);
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
                                    Utils.ShowActiveRoles();
                                    break;

                                default:
                                    Utils.ShowActiveSettings();
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
                        if(subArgs.Trim() is "赌怪" or "賭怪")
                        {
                            Utils.SendMessage(GetString("GuesserInfoLong"), PlayerControl.LocalPlayer.PlayerId);
                            break;
                        }
                        SendRolesInfo(subArgs, PlayerControl.LocalPlayer, Utils.CanUseDevCommand(PlayerControl.LocalPlayer));
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
                            if (GameStates.IsInGame)
                            {
                                string mtext = GetString(role.ToString()) + PlayerControl.LocalPlayer.GetRoleInfo(true);
                                foreach (var subRole in Main.PlayerStates[PlayerControl.LocalPlayer.PlayerId].SubRoles)
                                    mtext += $"\n\n"+ GetString($"{subRole}") + GetString($"{subRole}InfoLong");
                                if (CustomRolesHelper.RoleExist(CustomRoles.Ntr) && (role is not CustomRoles.GM and CustomRoles.Ntr))
                                    mtext += $"\n\n" + GetString($"Lovers") + GetString($"LoversInfoLong");
                                HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, mtext);
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
                        var color = Utils.MsgToColor(subArgs, true);
                        if (color == Byte.MaxValue)
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
                        if (!GameStates.IsInGame)
                        {
                            Utils.SendMessage("很抱歉，您只能在游戏中使用该指令", PlayerControl.LocalPlayer.PlayerId);
                            break;
                        }
                        foreach (var pc in Main.AllPlayerControls)
                        {
                            pc.RpcSetNameEx(pc.GetRealName(isMeeting: true));
                        }
                        ChatUpdatePatch.DoBlockChat = false;
                        Utils.NotifyRoles(isMeeting: GameStates.IsMeeting, NoCache: true);
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

                    case "/qq":
                        canceled = true;
                        if (Main.newLobby) Cloud.SendCodeToQQ(true);
                        else Utils.SendMessage("很抱歉，每个房间车队姬只会发一次", PlayerControl.LocalPlayer.PlayerId);
                        break;
                        
                    default:
                        Main.isChatCommand = false;
                        break;
                }
            }
            goto Skip;
            Canceled:
            Main.isChatCommand = false;
            canceled = true;
            Skip:
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
            text = text.Replace("着", "者").Trim();
            return text switch
            {
                "管理員" or "管理" => "管理员",
                "賞金獵人" or "赏金" => "赏金猎人",
                "自爆兵" or "自爆" => "自爆兵",
                "邪惡的追踪者" or "邪恶追踪者" => "追踪者",
                "煙花商人" or "烟花" => "烟花商人",
                "夢魘" => "梦魇",
                "詭雷" => "诡雷",
                "黑手黨" or "黑手" => "黑手党",
                "嗜血殺手" or "嗜血" => "嗜血杀手",
                "狂妄殺手" or "狂妄" => "狂妄杀手",
                "殺戮機器" or "杀戮" or "机器" => "杀戮机器",
                "蝕時者" or "蚀时" => "蚀时者",
                "狙擊手" or "狙击" => "狙击手",
                "傀儡師" or "傀儡" => "傀儡师",
                "殭屍" or "丧尸" => "僵尸",
                "吸血鬼" or "吸血" => "吸血鬼",
                "術士" => "术士",
                "駭客" or "黑客" => "骇客",
                "刺客" => "刺客",
                "礦工" => "矿工",
                "逃逸者" or "逃逸" => "逃逸者",
                "女巫" => "女巫",
                "監視者" or "监管" => "监管者",
                "窺視者" or "窥视" => "窥视者",
                "誘餌" or "大奖" or "头奖" => "诱饵",
                "擺爛人" or "摆烂" => "摆烂人",
                "獨裁者" or "独裁" => "独裁者",
                "醫生" => "医生",
                "偵探" => "侦探",
                "幸運兒" or "幸运" => "幸运儿",
                "大明星" or "明星" => "大明星",
                "網紅" => "网红",
                "俠客" => "侠客",
                "正義賭怪" or "正义的赌怪" or "好赌" => "正义赌怪",
                "邪惡賭怪" or "邪恶的赌怪" or "坏赌" or "恶赌" => "邪恶赌怪",
                "工程師" or "工程" => "工程师",
                "市長" => "市长",
                "被害妄想症" or "被害妄想" or "被迫害妄想症" or "被害" or "妄想" or "妄想症" => "被害妄想症",
                "愚者" or "愚" => "愚者",
                "修理大师" or "修理" or "维修" => "修理工",
                "警長" => "警长",
                "告密者" or "告密" => "告密者",
                "增速者" or "增速" => "增速者",
                "陷阱師" or "陷阱" or "小奖" => "陷阱师",
                "傳送師" or "传送" => "传送师",
                "縱火犯" or "纵火" => "纵火犯",
                "野心家" or "野心" => "野心家",
                "處刑人" or "处刑" => "处刑人",
                "小丑" or "丑皇" => "小丑",
                "投機者殺手" or "投机杀手" or "带刀投机" or "杀手投机" => "投机者杀手",
                "投機者" or "投机" => "投机者",
                "馬里奧" => "马里奥",
                "薛定諤的貓" or "薛定谔猫" or "猫" => "薛定谔的猫",
                "恐怖分子" or "恐怖" => "恐怖分子",
                "豺狼" => "豺狼",
                "神" => "神",
                "情人" or "愛人" or "链子" => "恋人",
                "絕境者" or "绝境" => "绝境者",
                "閃電俠" or"闪电" => "闪电侠",
                "靈媒" => "灵媒",
                "破平者" or "破平" => "破平者",
                "執燈人" or "执灯" or "灯人" => "执灯人",
                "膽小" or "胆小" => "胆小鬼",
                "迷惑者" or "迷幻" => "迷幻者",
                _ => text,
            };
        }

        private static readonly Dictionary<CustomRoles, string> roleList = new()
        {
                //GM
                { CustomRoles.GM, GetString("GM") },
                //Impostor役職
                { (CustomRoles)(-1), $"== {GetString("Impostor")} ==" }, //区切り用
                { CustomRoles.AntiAdminer, GetString("AntiAdminer") },
                { CustomRoles.Bomber, GetString("Bomber") },
                { CustomRoles.BountyHunter, GetString("BountyHunter") },
                { CustomRoles.EvilTracker,GetString("EvilTracker") },
                { CustomRoles.FireWorks, GetString("FireWorks") },
                { CustomRoles.Mare, GetString("Mare") },
                { CustomRoles.Mafia, GetString("Mafia") },
                { CustomRoles.Minimalism, GetString("Minimalism") },
                { CustomRoles.SerialKiller, GetString("SerialKiller") },
                { CustomRoles.TimeThief, GetString("TimeThief")},
                { CustomRoles.Sniper, GetString("Sniper") },
                { CustomRoles.Zombie, GetString("Zombie") },
                { CustomRoles.Puppeteer, GetString("Puppeteer") },
                { CustomRoles.Vampire, GetString("Vampire") },
                { CustomRoles.Warlock, GetString("Warlock") },
                { CustomRoles.Assassin, GetString("Assassin") },
                { CustomRoles.Hacker, GetString("Hacker") },
                { CustomRoles.Miner, GetString("Miner") },
                { CustomRoles.Escapee, GetString("Escapee") },
                { CustomRoles.Witch, GetString("Witch") },
                { CustomRoles.Sans, GetString("Sans") },
                { CustomRoles.BoobyTrap, GetString("BoobyTrap") },
                { CustomRoles.EvilGuesser, GetString("EvilGuesser") },
                { CustomRoles.Scavenger, GetString("Scavenger") },
                //Crewmate役職
                { (CustomRoles)(-4), $"== {GetString("Crewmate")} ==" }, //区切り用
                { CustomRoles.Bait, GetString("Bait") },
                { CustomRoles.Luckey, GetString("Luckey") },
                { CustomRoles.Needy, GetString("Needy") },
                { CustomRoles.Dictator, GetString("Dictator") },
                { CustomRoles.Doctor, GetString("Doctor") },
                { CustomRoles.SuperStar, GetString("SuperStar") },
                { CustomRoles.CyberStar, GetString("CyberStar") },
                { CustomRoles.Mayor, GetString("Mayor") },
                { CustomRoles.Paranoia, GetString("Paranoia") },
                { CustomRoles.Psychic, GetString("Psychic") },
                { CustomRoles.SabotageMaster, GetString("SabotageMaster") },
                { CustomRoles.Detective,GetString("Detective") },
                { CustomRoles.Sheriff, GetString("Sheriff") },
                { CustomRoles.Snitch, GetString("Snitch") },
                { CustomRoles.SpeedBooster, GetString("SpeedBooster") },
                { CustomRoles.Trapper, GetString("Trapper") },
                { CustomRoles.ChivalrousExpert, GetString("ChivalrousExpert") },
                { CustomRoles.NiceGuesser, GetString("NiceGuesser") },
                { CustomRoles.Transporter, GetString("Transporter") },
                //Neutral役職
                { (CustomRoles)(-5), $"== {GetString("Neutral")} ==" }, //区切り用
                { CustomRoles.Arsonist, GetString("Arsonist") },
                { CustomRoles.Executioner, GetString("Executioner")},
                { CustomRoles.Jester, GetString("Jester") },
                { CustomRoles.God, GetString("God") },
                { CustomRoles.OpportunistKiller, GetString("OpportunistKiller") },
                { CustomRoles.Opportunist, GetString("Opportunist") },
                { CustomRoles.Mario, GetString("Mario") },
                { CustomRoles.Terrorist, GetString("Terrorist") },
                { CustomRoles.Jackal, GetString("Jackal") },
                //属性
                { (CustomRoles)(-6), $"== {GetString("Addons")} ==" }, //区切り用
                {CustomRoles.Lovers, GetString("Lovers") },
                {CustomRoles.Ntr, GetString("Ntr") },
                {CustomRoles.LastImpostor, GetString("LastImpostor") },
                {CustomRoles.Madmate, GetString("Madmate") },
                {CustomRoles.Watcher, GetString("Watcher") },
                {CustomRoles.Flashman, GetString("Flashman") },
                { CustomRoles.Lighter, GetString("Lighter") },
                { CustomRoles.Seer,GetString("Seer") },
                { CustomRoles.Brakar,GetString("Brakar") },
                { CustomRoles.Oblivious,GetString("Oblivious") },
                { CustomRoles.Bewilder,GetString("Bewilder") },
            };

        public static bool GetRoleByName(string name, out CustomRoles role)
        {
            role = new();
            if (name == "" || name == String.Empty) return false;
            Regex r = new("[\u4e00-\u9fa5]+$");
            bool ismatch = r.IsMatch(name);
            MatchCollection mc = r.Matches(name);
            string result = string.Empty;
            for (int i = 0; i < mc.Count; i++)
            {
                if (mc[i].ToString() == "是") continue;
                result += mc[i];//匹配结果是完整的数字，此处可以不做拼接的
            }
            name = ToSimplified(result.Replace("是", string.Empty).Trim());
            foreach (var rl in roleList)
            {
                var roleShort = rl.Key.ToString().ToLower();
                var roleName = rl.Value;

                if (name.Contains(roleShort) || name.Contains(roleName))
                {
                    role = rl.Key;
                    return true;
                }
            }
            return false;
        }

        public static void SendRolesInfo(string role, PlayerControl player, bool isDev = false)
        {
            role = role.Trim();
            if (role == "" || role == string.Empty)
            {
                Utils.ShowActiveRoles();
                return;
            }
            role = ToSimplified(role);

            var msg = "";
            var rolemsg = $"{GetString("Command.h_args")}";
            foreach (var r in roleList)
            {
                var roleName = r.Key.ToString();
                var roleShort = r.Value;

                if (string.Compare(role, roleName, true) == 0 || string.Compare(role, roleShort, true) == 0)
                {

                    if (isDev && GameStates.IsLobby)
                    {
                        string devMark = "▲";
                        if (CustomRolesHelper.IsAdditionRole(r.Key)) devMark = "";
                        if (r.Key is CustomRoles.GM) devMark = "";
                        if (r.Key.GetCount () < 1 || r.Key.GetMode() == 0) devMark = "";
                        Utils.SendMessage(devMark + GetString(roleName) + GetString($"{roleName}InfoLong"), player.PlayerId);
                        if (devMark == "▲")
                        {
                            if (Main.DevRole.ContainsKey(player.PlayerId)) Main.DevRole.Remove(player.PlayerId);
                            Main.DevRole.Add(player.PlayerId, r.Key);
                        }
                    }
                    else
                    {
                        Utils.SendMessage(GetString(roleName) + GetString($"{roleName}InfoLong"), player.PlayerId);
                    }
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

            Utils.SendMessage("请正确拼写您要查询的职业哦~\n查看所有职业请直接输入/r", player.PlayerId);
            return;
        }
        public static void OnReceiveChat(PlayerControl player, string text)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (text.StartsWith("\n")) text = text[1..];
            string[] args = text.Split(' ');
            string subArgs = "";
            if (text.Length >= 3) if (text[..2] == "/r" && text[..3] != "/rn") args[0] = "/r";
            if (GuessManager.GuesserMsg(player, text)) return;
            if (MafiaMsgCheck(player, text)) return;
            if (ProhibitedCheck(player, text)) return;
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
                    if (subArgs.Trim() is "赌怪" or "賭怪")
                    {
                        Utils.SendMessage(GetString("GuesserInfoLong"), player.PlayerId);
                        break;
                    }
                    SendRolesInfo(subArgs, player, Utils.CanUseDevCommand(player));
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
                    {
                        string mtext = GetString(role.ToString()) + player.GetRoleInfo(true);
                        foreach (var subRole in Main.PlayerStates[player.PlayerId].SubRoles)
                            mtext += $"\n\n" + GetString($"{subRole}") + GetString($"{subRole}InfoLong");
                        if (CustomRolesHelper.RoleExist(CustomRoles.Ntr) && (role is not CustomRoles.GM and CustomRoles.Ntr))
                            mtext += $"\n\n" + GetString($"Lovers") + GetString($"LoversInfoLong");
                        Utils.SendMessage(mtext, player.PlayerId);
                    }
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
                        if (color == Byte.MaxValue)
                        {
                            Utils.SendMessage(GetString("IllegalColor"), player.PlayerId);
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
                        Utils.SendMessage($"【{name}】选择自愿离开\n很抱歉给大家带来了糟糕的游戏体验\n我们真的很努力地在进步了");
                        AmongUsClient.Instance.KickPlayer(player.GetClientId(), true);
                    }
                    else
                    {
                        Utils.SendMessage(GetString("SureUse.quit"), player.PlayerId);
                    }
                    break;

                case "/xf":
                    if (!GameStates.IsInGame)
                    {
                        Utils.SendMessage("很抱歉，您只能在游戏中使用该指令", player.PlayerId);
                        break;
                    }
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        pc.RpcSetNameEx(pc.GetRealName(isMeeting: true));
                    }
                    ChatUpdatePatch.DoBlockChat = false;
                    Utils.NotifyRoles(isMeeting: GameStates.IsMeeting, NoCache: true);
                    Utils.SendMessage("已尝试修复名字遮挡", player.PlayerId);
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