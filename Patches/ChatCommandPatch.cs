using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using HarmonyLib;
using Hazel;
using static TownOfHost.Translator;

namespace TownOfHost
{
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
    class ChatCommands
    {
        public static bool Prefix(ChatController __instance)
        {
            var text = __instance.TextArea.text;
            string[] args = text.Split(' ');
            string subArgs = "";
            var canceled = false;
            var cancelVal = "";
            Main.isChatCommand = true;
            Logger.Info(text, "SendChat");
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
                        version_text += $"{kvp.Key}:{Utils.GetPlayerById(kvp.Key)?.Data?.PlayerName}:{kvp.Value.version}({kvp.Value.tag})\n";
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

                    case "/r":
                    case "/rename":
                        canceled = true;
                        if (args.Length > 1) { Main.nickName = args[1]; }
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
                        canceled = true;
                        subArgs = args.Length < 2 ? "" : args[1];
                        switch (subArgs)
                        {
                            case "crewmate":
                                ShipStatus.Instance.enabled = false;
                                ShipStatus.RpcEndGame(GameOverReason.HumansDisconnect, false);
                                break;

                            case "impostor":
                                ShipStatus.Instance.enabled = false;
                                ShipStatus.RpcEndGame(GameOverReason.ImpostorDisconnect, false);
                                break;

                            default:
                                __instance.AddChat(PlayerControl.LocalPlayer, "crewmate | impostor");
                                cancelVal = "/dis";
                                break;
                        }
                        ShipStatus.Instance.RpcRepairSystem(SystemTypes.Admin, 0);
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

                            case "att":
                            case "attributes":
                                subArgs = args.Length < 3 ? "" : args[2];
                                switch (subArgs)
                                {
                                    case "lastimpostor":
                                    case "limp":
                                        Utils.SendMessage(GetString("LastImpostor") + GetString("LastImpostorInfo"));
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

                            default:
                                Utils.ShowHelp();
                                break;
                        }
                        break;

                    case "/t":
                    case "/template":
                        canceled = true;
                        if (args.Length > 1) SendTemplate(args[1]);
                        else HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, $"使用例:\n{args[0]} test");
                        break;

                    case "/mw":
                    case "/messagewait":
                        canceled = true;
                        if (args.Length > 1 && int.TryParse(args[1], out int sec))
                        {
                            Main.MessageWait.Value = sec;
                            Utils.SendMessage($"{sec}秒に設定されました", 0);
                        }
                        else Utils.SendMessage($"第一引数を秒数で指定します。\n使用例:\n{args[0]} 3", 0);
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

        public static void GetRolesInfo(string role)
        {
            var roleList = new Dictionary<CustomRoles, string>
            {
                //Impostor役職
                { (CustomRoles)(-1), $"== {GetString("Impostor")} ==" }, //区切り用
                { CustomRoles.BountyHunter, "bo" },
                { CustomRoles.FireWorks, "fw" },
                { CustomRoles.Mare, "ma" },
                { CustomRoles.Mafia, "mf" },
                { CustomRoles.SerialKiller, "sk" },
                { CustomRoles.ShapeMaster, "sha" },
                { CustomRoles.TimeThief, "tt"},
                { CustomRoles.Sniper, "snp" },
                { CustomRoles.Puppeteer, "pup" },
                { CustomRoles.Vampire, "va" },
                { CustomRoles.Warlock, "wa" },
                { CustomRoles.Witch, "wi" },
                //Madmate役職
                { (CustomRoles)(-2), $"== {GetString("Madmate")} ==" }, //区切り用
                { CustomRoles.MadGuardian, "mg" },
                { CustomRoles.Madmate, "mm" },
                { CustomRoles.MadSnitch, "msn" },
                { CustomRoles.SKMadmate, "sm" },
                //両陣営役職
                { (CustomRoles)(-3), $"== {GetString("Impostor")} or {GetString("Crewmate")} ==" }, //区切り用
                { CustomRoles.Watcher, "wat" },
                //Crewmate役職
                { (CustomRoles)(-4), $"== {GetString("Crewmate")} ==" }, //区切り用
                { CustomRoles.Bait, "ba" },
                { CustomRoles.Dictator, "dic" },
                { CustomRoles.Doctor, "doc" },
                { CustomRoles.Lighter, "li" },
                { CustomRoles.Mayor, "my" },
                { CustomRoles.SabotageMaster, "sa" },
                { CustomRoles.Sheriff, "sh" },
                { CustomRoles.Snitch, "sn" },
                { CustomRoles.SpeedBooster, "sb" },
                { CustomRoles.Trapper, "tra" },
                //Neutral役職
                { (CustomRoles)(-5), $"== {GetString("Neutral")} ==" }, //区切り用
                { CustomRoles.Arsonist, "ar" },
                { CustomRoles.Egoist, "eg" },
                { CustomRoles.Executioner, "exe" },
                { CustomRoles.Jester, "je" },
                { CustomRoles.Opportunist, "op" },
                { CustomRoles.SchrodingerCat, "sc" },
                { CustomRoles.Terrorist, "te" },
                //Sub役職
                { (CustomRoles)(-6), $"== {GetString("SubRole")} ==" }, //区切り用
                {CustomRoles.Lovers, "lo" },
                //HAS
                { (CustomRoles)(-7), $"== {GetString("HideAndSeek")} ==" }, //区切り用
                { CustomRoles.HASFox, "hfo" },
                { CustomRoles.HASTroll, "htr" },

            };
            var msg = "";
            var rolemsg = $"{GetString("Command.h_args")}";
            foreach (var r in roleList)
            {
                var roleName = r.Key.ToString();
                var roleShort = r.Value;

                if (String.Compare(role, roleName, true) == 0 || String.Compare(role, roleShort, true) == 0)
                {
                    Utils.SendMessage(GetString(roleName) + GetString($"{roleName}InfoLong"));
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
            msg += rolemsg;
            Utils.SendMessage(msg);
        }
        public static void SendTemplate(string str = "")
        {
            if (!File.Exists("template.txt"))
            {
                HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, "Among Us.exeと同じフォルダにtemplate.txtが見つかりませんでした。\n新規作成します。");
                File.WriteAllText(@"template.txt", "test:This is template text.\\nLine breaks are also possible.\ntest:これは定型文です。\\n改行も可能です。");
                return;
            }
            using StreamReader sr = new(@"template.txt", Encoding.GetEncoding("UTF-8"));
            string text;
            string[] tmp = { };
            List<string> sendList = new();
            while ((text = sr.ReadLine()) != null)
            {
                tmp = text.Split(":");
                if (tmp[0] == str && tmp.Length > 1 && tmp[1] != "") sendList.Add(tmp.Skip(1).Join(delimiter: "").Replace("\\n", "\n"));
            }
            if (sendList.Count == 0)
                HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, $"「{str}」に該当するメッセージが見つかりませんでした。\n{str}:内容\nのようにtemplate.txtに追記してください。");
            else for (int i = 0; i < sendList.Count; i++) Utils.SendMessage(sendList[i]);
        }
        public static void OnReceiveChat(PlayerControl player, string text)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            string[] args = text.Split(' ');
            string subArgs = "";
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

                default:
                    break;
            }
        }
    }
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
    class ChatUpdatePatch
    {
        public static void Postfix(ChatController __instance)
        {
            if (!AmongUsClient.Instance.AmHost || Main.MessagesToSend.Count < 1 || Main.MessageWait.Value > __instance.TimeSinceLastMessage) return;
            (string msg, byte sendTo) = Main.MessagesToSend[0];
            Main.MessagesToSend.RemoveAt(0);
            int clientId = sendTo == byte.MaxValue ? -1 : Utils.GetPlayerById(sendTo).GetClientId();
            if (clientId == -1) DestroyableSingleton<HudManager>.Instance.Chat.AddChat(PlayerControl.LocalPlayer, msg);
            MessageWriter writer = clientId == -1 ? AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SendChat, SendOption.None, clientId)
                                                : AmongUsClient.Instance.StartRpcImmediately(Utils.GetPlayerById(sendTo).NetId, (byte)RpcCalls.SendChat, SendOption.None, clientId);
            writer.Write(msg);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
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
}