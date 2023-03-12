using Assets.CoreScripts;
using HarmonyLib;
using Hazel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TOHE.Modules;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

[HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
internal class ChatCommands
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
            string text = GetString("PlayerIdList");
            foreach (var npc in Main.AllAlivePlayerControls)
                text += "\n" + npc.PlayerId.ToString() + " → (" + npc.GetDisplayRoleName() + ") " + npc.GetRealName();
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
        Utils.SendMessage(string.Format(GetString("MafiaKillSucceed"), Name), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.Mafia), " ★ 特供情报 ★ "));

        new LateTask(() =>
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
        for (int i = 0; i < text.Length; i++)
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
        return text.Contains("开") || text.Contains("kai");
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
                msg = string.Format(GetString("Message.KickWhoSayStart"), name);
                msg = $"";
                if (Options.AutoKickStart.GetBool())
                {
                    if (!Main.SayStartTimes.ContainsKey(player.GetClientId())) Main.SayStartTimes.Add(player.GetClientId(), 0);
                    Main.SayStartTimes[player.GetClientId()]++;
                    msg = string.Format(GetString("Message.WarnWhoSayStart"), name, Main.SayStartTimes[player.GetClientId()]);
                    if (Main.SayStartTimes[player.GetClientId()] > Options.AutoKickStartTimes.GetInt())
                    {
                        msg = string.Format(GetString("Message.KickStartAfterWarn"), name, Main.SayStartTimes[player.GetClientId()]);
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

        if (Options.AutoWarnStopWords.GetBool()) msg = string.Format(GetString("Message.WarnWhoSayBanWord"), name);
        if (Options.AutoKickStopWords.GetBool())
        {
            if (!Main.SayBanwordsTimes.ContainsKey(player.GetClientId())) Main.SayBanwordsTimes.Add(player.GetClientId(), 0);
            Main.SayBanwordsTimes[player.GetClientId()]++;
            msg = string.Format(GetString("Message.WarnWhoSayBanWordTimes"), name, Main.SayBanwordsTimes[player.GetClientId()]);
            if (Main.SayBanwordsTimes[player.GetClientId()] > Options.AutoKickStopWordsTimes.GetInt())
            {
                msg = string.Format(GetString("Message.KickWhoSayBanWordAfterWarn"), name, Main.SayBanwordsTimes[player.GetClientId()]);
                kick = true;
            }
        }

        if (msg != "")
        {
            if (kick || !GameStates.IsInGame) Utils.SendMessage(msg);
            else
            {
                foreach (var pc in Main.AllPlayerControls)
                    if (pc.IsAlive() == player.IsAlive())
                        Utils.SendMessage(msg, pc.PlayerId);
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
        if (text.Length >= 4) if (text[..3] == "/up") args[0] = "/up";
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
                    version_text += $"{kvp.Key}:{Utils.GetPlayerById(kvp.Key)?.Data?.PlayerName.RemoveHtmlTags().Replace("\r\n", string.Empty)}:{kvp.Value.forkId}/{kvp.Value.version}({kvp.Value.tag})\n";
                }
                if (version_text != "") HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, (Utils.IsDev(PlayerControl.LocalPlayer) ? "\n" : string.Empty) + version_text);
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
                    Utils.SendMessage(string.Format(GetString("Message.SetLevel"), subArgs), PlayerControl.LocalPlayer.PlayerId);
                    int.TryParse(subArgs, out int input);
                    if (input is < 1 or > 100)
                    {
                        Utils.SendMessage(GetString("Message.AllowLevelRange"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    var number = Convert.ToUInt32(input);
                    PlayerControl.LocalPlayer.RpcSetLevel(number - 1);
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
                    if (subArgs.Trim() is "赌怪" or "賭怪")
                    {
                        Utils.SendMessage(GetString("GuesserInfoLong"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    SendRolesInfo(subArgs, PlayerControl.LocalPlayer, Utils.CanUseDevCommand(PlayerControl.LocalPlayer));
                    break;

                case "/up":
                    canceled = true;
                    subArgs = text.Remove(0, 3);
                    if (!Utils.IsUP(PlayerControl.LocalPlayer)) break;
                    if (!Options.EnableUpMode.GetBool())
                    {
                        Utils.SendMessage($"请在设置启用【{GetString("EnableUpMode")}】");
                        break;
                    }
                    if (!GameStates.IsLobby)
                    {
                        Utils.SendMessage(GetString("Message.OnlyCanUseInLobby"));
                        break;
                    }
                    SendRolesInfo(subArgs, PlayerControl.LocalPlayer, isUp: true);
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
                    {
                        var lp = PlayerControl.LocalPlayer;
                        var sb = new StringBuilder();
                        sb.Append(GetString(role.ToString()) + lp.GetRoleInfo(true));
                        Utils.ShowChildrenSettings(Options.CustomRoleSpawnChances[role], ref sb, command: true);
                        var txt = sb.ToString();
                        sb.Clear().Append(txt.RemoveHtmlTags());
                        foreach (var subRole in Main.PlayerStates[lp.PlayerId].SubRoles)
                            sb.Append($"\n\n" + GetString($"{subRole}") + GetString($"{subRole}InfoLong"));
                        if (CustomRolesHelper.RoleExist(CustomRoles.Ntr) && (role is not CustomRoles.GM and CustomRoles.Ntr))
                            sb.Append($"\n\n" + GetString($"Lovers") + GetString($"LoversInfoLong"));
                        Utils.SendMessage(sb.ToString(), lp.PlayerId);
                    }
                    else
                        HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, (Utils.IsDev(PlayerControl.LocalPlayer) ? "\n" : string.Empty) + GetString("Message.CanNotUseInLobby"));
                    break;

                case "/t":
                case "/template":
                    canceled = true;
                    if (args.Length > 1) TemplateManager.SendTemplate(args[1]);
                    else HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, (Utils.IsDev(PlayerControl.LocalPlayer) ? "\n" : string.Empty) + $"{GetString("ForExample")}:\n{args[0]} test");
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
                        Utils.SendMessage(GetString("Message.CanNotUseInLobby"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    if (args.Length < 2 || !int.TryParse(args[1], out int id)) break;
                    var player = Utils.GetPlayerById(id);
                    if (player != null)
                    {
                        player.Data.IsDead = true;
                        Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.etc;
                        player.RpcExileV2();
                        Main.PlayerStates[player.PlayerId].SetDead();
                    }
                    break;

                case "/kill":
                    canceled = true;
                    if (GameStates.IsLobby)
                    {
                        Utils.SendMessage(GetString("Message.CanNotUseInLobby"), PlayerControl.LocalPlayer.PlayerId);
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
                    if (color == byte.MaxValue)
                    {
                        Utils.SendMessage(GetString("IllegalColor"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    PlayerControl.LocalPlayer.RpcSetColor(color);
                    Utils.SendMessage(string.Format(GetString("Message.SetColor"), subArgs), PlayerControl.LocalPlayer.PlayerId);
                    break;

                case "/quit":
                case "/qt":
                    canceled = true;
                    Utils.SendMessage(GetString("Message.CanNotUseByHost"), PlayerControl.LocalPlayer.PlayerId);
                    break;

                case "/xf":
                    canceled = true;
                    if (!GameStates.IsInGame)
                    {
                        Utils.SendMessage(GetString("Message.CanNotUseInLobby"), PlayerControl.LocalPlayer.PlayerId);
                        break;
                    }
                    foreach (var pc in Main.AllPlayerControls)
                    {
                        pc.RpcSetNameEx(pc.GetRealName(isMeeting: true));
                    }
                    ChatUpdatePatch.DoBlockChat = false;
                    Utils.NotifyRoles(isMeeting: GameStates.IsMeeting, NoCache: true);
                    Utils.SendMessage(GetString("Message.TryFixName"), PlayerControl.LocalPlayer.PlayerId);
                    break;

                case "/id":
                    canceled = true;
                    string msgText = GetString("PlayerIdList");
                    foreach (var pc in Main.AllPlayerControls)
                        msgText += "\n" + pc.PlayerId.ToString() + " → " + pc.GetRealName();
                    Utils.SendMessage(msgText, PlayerControl.LocalPlayer.PlayerId);
                    break;

                case "/qq":
                    canceled = true;
                    if (Main.newLobby) Cloud.SendCodeToQQ(true);
                    else Utils.SendMessage("很抱歉，每个房间车队姬只会发一次", PlayerControl.LocalPlayer.PlayerId);
                    break;

                case "/setrole":
                    if (!DebugModeManager.AmDebugger) break;
                    canceled = true;
                    subArgs = text.Remove(0, 8);
                    var setRole = FixRoleNameInput(subArgs.Trim());
                    foreach (CustomRoles rl in Enum.GetValues(typeof(CustomRoles)))
                    {
                        if (rl.IsVanilla()) continue;
                        var roleName = GetString(rl.ToString()).ToLower().Trim();
                        if (setRole.Contains(roleName))
                        {
                            PlayerControl.LocalPlayer.RpcSetRole(rl.GetRoleTypes());
                            PlayerControl.LocalPlayer.RpcSetCustomRole(rl);
                            Utils.NotifyRoles();
                            Utils.MarkEveryoneDirtySettings();
                        }
                    }
                    break;

                case "/end":
                    canceled = true;
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Draw);
                    GameManager.Instance.LogicFlow.CheckEndCriteria();
                    break;

                case "/mt":
                case "/hy":
                    canceled = true;
                    if (GameStates.IsMeeting) MeetingHud.Instance.RpcClose();
                    else PlayerControl.LocalPlayer.NoCheckStartMeeting(null);
                    break;

                case "/cs":
                    canceled = true;
                    subArgs = text.Remove(0, 3);
                    if (args.Length < 1 || !int.TryParse(args[1], out int sound)) break;
                    CustomSoundsManager.RPCPlay(PlayerControl.LocalPlayer.PlayerId, (CustomSounds)sound);
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

    public static string FixRoleNameInput(string text)
    {
        text = text.Replace("着", "者").Trim().ToLower();
        return text switch
        {
            "管理員" or "管理" => GetString("GM"),
            "賞金獵人" or "赏金" => GetString("BountyHunter"),
            "自爆兵" or "自爆" => GetString("Bomber"),
            "邪惡的追踪者" or "邪恶追踪者" or "追踪" => GetString("EvilTracker"),
            "煙花商人" or "烟花" => GetString("FireWorks"),
            "夢魘" => GetString("Mare"),
            "詭雷" => GetString("BoobyTrap"),
            "黑手黨" or "黑手" => GetString("Mafia"),
            "嗜血殺手" or "嗜血" => GetString("SerialKiller"),
            "千面鬼" or "千面" => GetString("ShapeMaster"),
            "狂妄殺手" or "狂妄" => GetString("Sans"),
            "殺戮機器" or "杀戮" or "机器" => GetString("Minimalism"),
            "蝕時者" or "蚀时" => GetString("TimeThief"),
            "狙擊手" or "狙击" => GetString("Sniper"),
            "傀儡師" or "傀儡" => GetString("Puppeteer"),
            "殭屍" or "丧尸" => GetString("Zombie"),
            "吸血鬼" or "吸血" => GetString("Vampire"),
            "術士" => GetString("Warlock"),
            "駭客" or "黑客" => GetString("Hacker"),
            "刺客" => GetString("Assassin"),
            "礦工" => GetString("Miner"),
            "逃逸者" or "逃逸" => GetString("Escapee"),
            "女巫" => GetString("Witch"),
            "監視者" or "监管" => GetString("AntiAdminer"),
            "清道夫" or "清道" => GetString("Scavenger"),
            "窺視者" or "窥视" => GetString("Watcher"),
            "誘餌" or "大奖" or "头奖" => GetString("Bait"),
            "擺爛人" or "摆烂" => GetString("Needy"),
            "獨裁者" or "独裁" => GetString("Dictator"),
            "醫生" => GetString("Doctor"),
            "偵探" => GetString("Detective"),
            "幸運兒" or "幸运" => GetString("Luckey"),
            "大明星" or "明星" => GetString("SuperStar"),
            "網紅" => GetString("CyberStar"),
            "俠客" => GetString("SwordsMan"),
            "正義賭怪" or "正义的赌怪" or "好赌" => GetString("NiceGuesser"),
            "邪惡賭怪" or "邪恶的赌怪" or "坏赌" or "恶赌" => GetString("EvilGuesser"),
            "市長" => GetString("Mayor"),
            "被害妄想症" or "被害妄想" or "被迫害妄想症" or "被害" or "妄想" or "妄想症" => GetString("Paranoia"),
            "愚者" or "愚" => GetString("Psychic"),
            "修理大师" or "修理" or "维修" => GetString("SabotageMaster"),
            "警長" => GetString("Sheriff"),
            "告密者" or "告密" => GetString("Snitch"),
            "增速者" or "增速" => GetString("SpeedBooster"),
            "時間操控者" or "时间操控人" or "时间操控" => GetString("TimeManager"),
            "陷阱師" or "陷阱" or "小奖" => GetString("Trapper"),
            "傳送師" or "传送" => GetString("Transporter"),
            "縱火犯" or "纵火" => GetString("Arsonist"),
            "處刑人" or "处刑" => GetString("Executioner"),
            "小丑" or "丑皇" => GetString("Jester"),
            "投機者" or "投机" => GetString("Opportunist"),
            "馬里奧" => GetString("Mario"),
            "恐怖分子" or "恐怖" => GetString("Terrorist"),
            "豺狼" => GetString("Jackal"),
            "神" => GetString("God"),
            "情人" or "愛人" or "链子" => GetString("Lovers"),
            "絕境者" or "绝境" => GetString("LastImpostor"),
            "閃電俠" or "闪电" => GetString("Flashman"),
            "靈媒" => GetString("Seer"),
            "破平者" or "破平" => GetString("Brakar"),
            "執燈人" or "执灯" or "灯人" => GetString("Lighter"),
            "膽小" or "胆小" => GetString("Oblivious"),
            "迷惑者" or "迷幻" => GetString("Bewilder"),
            "蠢蛋" or "笨蛋" or "蠢狗" => GetString("Fool"),
            "冤罪師" or "冤罪" => GetString("Innocent"),
            "資本家" or "资本主义" or "资本" => GetString("Capitalism"),
            "老兵" => GetString("Veteran"),
            "加班狂" or "加班" => GetString("Workhorse"),
            "復仇者" or "复仇" => GetString("Avanger"),
            "鵜鶘" => GetString("Pelican"),
            "保鏢" => GetString("Bodyguard"),
            "up" or "up主" => GetString("Youtuber"),
            "利己主義者" or "利己主义" or "利己" => GetString("Egoist"),
            "贗品商" or "赝品" => GetString("Counterfeiter"),
            "吹笛者" or "吹笛" => GetString("Piper"),
            "擲雷兵" or "掷雷" or "闪光弹" => GetString("Grenadier"),
            "竊票者" or "偷票" or "偷票者" or "窃票师" or "窃票" => GetString("TicketsStealer"),
            "教父" => GetString("Gangster"),
            "革命家" or "革命" => GetString("Revolutionist"),
            "fff團" or "fff" or "fff团" => GetString("FFF"),
            "清理工" or "清潔工" or "清洁工" or "清理" or "清洁" => GetString("Cleaner"),
            "法医" => GetString("Medicaler"),
            "占卜師" or "占卜" => GetString("Divinator"),
            "雙重人格" or "双重" or "双人格" or "人格" => GetString("DualPersonality"),
            "玩家" => GetString("Gamer"),
            "情報販子" or "情报" or "贩子" => GetString("Messenger"),
            "球狀閃電" or "球闪" or "球状" => GetString("BallLightning"),
            "潛藏者" or "潜藏" => GetString("DarkHide"),
            "貪婪者" or "贪婪" => GetString("Greedier"),
            "工作狂" or "工作" => GetString("Workaholic"),
            "呪狼" or "咒狼" => GetString("CursedWolf"),
            "寶箱怪" or "宝箱" => GetString("Mimic"),
            _ => text,
        };
    }

    public static bool GetRoleByName(string name, out CustomRoles role)
    {
        role = new();
        if (name == "" || name == string.Empty) return false;

        if ((TranslationController.InstanceExists ? TranslationController.Instance.currentLanguage.languageID : SupportedLangs.SChinese) == SupportedLangs.SChinese)
        {
            Regex r = new("[\u4e00-\u9fa5]+$");
            bool ismatch = r.IsMatch(name);
            MatchCollection mc = r.Matches(name);
            string result = string.Empty;
            for (int i = 0; i < mc.Count; i++)
            {
                if (mc[i].ToString() == "是") continue;
                result += mc[i];//匹配结果是完整的数字，此处可以不做拼接的
            }
            name = FixRoleNameInput(result.Replace("是", string.Empty).Trim());
        }
        else
        {
            name = name.Trim().ToLower();
        }

        foreach (CustomRoles rl in Enum.GetValues(typeof(CustomRoles)))
        {
            if (rl.IsVanilla()) continue;
            var roleName = GetString(rl.ToString()).ToLower().Trim();
            if (name.Contains(roleName))
            {
                role = rl;
                return true;
            }
        }
        return false;
    }
    public static void SendRolesInfo(string role, PlayerControl player, bool isDev = false, bool isUp = false)
    {
        role = role.Trim().ToLower();
        if (role.StartsWith("/r")) role.Replace("/r", string.Empty);
        if (role.StartsWith("/up")) role.Replace("/up", string.Empty);
        if (role.EndsWith("\r\n")) role.Replace("\r\n", string.Empty);
        if (role.EndsWith("\n")) role.Replace("\n", string.Empty);

        if (role == "" || role == string.Empty)
        {
            Utils.ShowActiveRoles(player.PlayerId);
            return;
        }
        role = FixRoleNameInput(role);

        foreach (CustomRoles rl in Enum.GetValues(typeof(CustomRoles)))
        {
            if (rl.IsVanilla()) continue;
            var roleName = GetString(rl.ToString());
            if (role.Contains(roleName.ToLower().Trim()))
            {
                if ((isDev || isUp) && GameStates.IsLobby)
                {
                    string devMark = "▲";
                    if (CustomRolesHelper.IsAdditionRole(rl)) devMark = "";
                    if (rl is CustomRoles.GM || rl.IsDesyncRole()) devMark = "";
                    if (rl.GetCount() < 1 || rl.GetMode() == 0) devMark = "";
                    if (isUp)
                    {
                        if (devMark == "▲") Utils.SendMessage("已提升您成为【" + roleName + "】的概率", player.PlayerId);
                        else Utils.SendMessage("无法提升您成为【" + roleName + "】的概率\n可能是因为您没有启用该职业或该职业不支持被指定", player.PlayerId);
                    }
                    else
                    {
                        Utils.SendMessage(devMark + roleName + GetString($"{rl}InfoLong"), player.PlayerId);
                    }
                    if (devMark == "▲")
                    {
                        if (Main.DevRole.ContainsKey(player.PlayerId)) Main.DevRole.Remove(player.PlayerId);
                        Main.DevRole.Add(player.PlayerId, rl);
                    }
                }
                else
                {
                    Utils.SendMessage(roleName + GetString($"{rl}InfoLong"), player.PlayerId);
                }
                return;
            }
        }
        if (isUp) Utils.SendMessage("请正确拼写您要指定的职业哦~\n查看所有职业请直接输入/r", player.PlayerId);
        else Utils.SendMessage(GetString("Message.CanNotFindRoleThePlayerEnter"), player.PlayerId);
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
                    var sb = new StringBuilder();
                    sb.Append(GetString(role.ToString()) + player.GetRoleInfo(true));
                    Utils.ShowChildrenSettings(Options.CustomRoleSpawnChances[role], ref sb, command: true);
                    var txt = sb.ToString();
                    sb.Clear().Append(txt.RemoveHtmlTags());
                    foreach (var subRole in Main.PlayerStates[player.PlayerId].SubRoles)
                        sb.Append($"\n\n" + GetString($"{subRole}") + GetString($"{subRole}InfoLong"));
                    if (CustomRolesHelper.RoleExist(CustomRoles.Ntr) && (role is not CustomRoles.GM and CustomRoles.Ntr))
                        sb.Append($"\n\n" + GetString($"Lovers") + GetString($"LoversInfoLong"));
                    Utils.SendMessage(sb.ToString(), player.PlayerId);
                }
                else
                    Utils.SendMessage(GetString("Message.CanNotUseInLobby"), player.PlayerId);
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
                    Utils.SendMessage(string.Format(GetString("Message.SetColor"), subArgs), player.PlayerId);
                }
                else
                {
                    Utils.SendMessage(GetString("DisableUseCommand"), player.PlayerId);
                }
                break;

            case "/quit":
            case "/qt":
                subArgs = args.Length < 2 ? "" : args[1];
                if (subArgs.Equals(player.PlayerId.ToString()))
                {
                    string name = player.GetRealName();
                    Utils.SendMessage(string.Format(GetString("Message.PlayerQuitForever"), name));
                    AmongUsClient.Instance.KickPlayer(player.GetClientId(), true);
                }
                else
                {
                    Utils.SendMessage(string.Format(GetString("SureUse.quit"), player.PlayerId.ToString()), player.PlayerId);
                }
                break;

            case "/xf":
                if (!GameStates.IsInGame)
                {
                    Utils.SendMessage(GetString("Message.CanNotUseInLobby"), player.PlayerId);
                    break;
                }
                foreach (var pc in Main.AllPlayerControls)
                {
                    pc.RpcSetNameEx(pc.GetRealName(isMeeting: true));
                }
                ChatUpdatePatch.DoBlockChat = false;
                Utils.NotifyRoles(isMeeting: GameStates.IsMeeting, NoCache: true);
                Utils.SendMessage(GetString("Message.TryFixName"), player.PlayerId);
                break;

            case "/say":
            case "/s":
                if (Utils.CanUseDevCommand(player))
                {
                    if (args.Length > 1)
                        Utils.SendMessage(args.Skip(1).Join(delimiter: " "), title: $"<color={Main.ModColor}>{"【 ★ 开发者消息 ★ 】"}</color>");
                }
                else if (Utils.IsDev(player))
                {
                    if (args.Length > 1)
                        Utils.SendMessage(args.Skip(1).Join(delimiter: " "), title: $"<color=#4bc9b0>{"【 ★ 贡献者消息 ★ 】"}</color>");
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
            HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, (Utils.IsDev(PlayerControl.LocalPlayer) ? "\n" : string.Empty) + $"No {filename} file found.");
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
                HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, (Utils.IsDev(PlayerControl.LocalPlayer) ? "\n" : string.Empty) + string.Format(GetString("Message.TemplateNotFoundHost"), Main.BANNEDWORDS_FILE_PATH, tags.Join(delimiter: ", ")));
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
internal class ChatUpdatePatch
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
internal class AddChatPatch
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
internal class RpcSendChatPatch
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