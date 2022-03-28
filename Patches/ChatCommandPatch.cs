using System.IO;
using Hazel;
using HarmonyLib;
using System.Linq;
using System;
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
            var canceled = false;
            var cancelVal = "";
            if (AmongUsClient.Instance.AmHost)
            {
                main.isChatCommand = true;
                Logger.info(text, "SendChat");
                switch (args[0])
                {
                    case "/win":
                    case "/winner":
                        canceled = true;
                        Utils.SendMessage("Winner: " + string.Join(",", main.winnerList.Select(b => main.AllPlayerNames[b])));
                        break;

                    case "/l":
                    case "/lastroles":
                        canceled = true;
                        Utils.ShowLastRoles();
                        break;

                    case "/r":
                    case "/rename":
                        canceled = true;
                        if (args.Length > 1) { main.nickName = args[1]; }
                        break;

                    case "/n":
                    case "/now":
                        canceled = true;
                        Utils.ShowActiveSettings();
                        break;

                    case "/dis":
                        canceled = true;
                        if (args.Length < 2) { __instance.AddChat(PlayerControl.LocalPlayer, "crewmate | impostor"); cancelVal = "/dis"; }
                        switch (args[1])
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

                    case "/dump":
                        canceled = true;
                        string t = DateTime.Now.ToString("yy-dd-yy_HH.mm.ss");
                        string filename = $"{System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)}/{t}.log";
                        FileInfo file = new FileInfo(@$"{System.Environment.CurrentDirectory}/BepInEx/LogOutput.log");
                        file.CopyTo(@filename);
                        System.Diagnostics.Process.Start(@$"{System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)}");
                        Logger.info($"{filename}にログを保存しました。");
                        HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, "デスクトップにログを保存しました。バグ報告チケットを作成してこのファイルを添付してください。");
                        break;

                    case "/h":
                    case "/help":
                        canceled = true;
                        if (args.Length < 2)
                        {
                            Utils.ShowHelp();
                            break;
                        }
                        switch (args[1])
                        {
                            case "r":
                            case "roles":
                                if (args.Length < 3) { getRolesInfo(""); break; }
                                getRolesInfo(args[2]);
                                break;

                            case "att":
                            case "attributes":
                                if (args.Length < 3) { Utils.SendMessage("使用可能な引数(略称): lastimpostor(limp)"); break; }
                                switch (args[2])
                                {
                                    case "lastimpostor":
                                    case "limp":
                                        Utils.SendMessage(getString("LastImpostor") + getString("LastImpostorInfo"));
                                        break;

                                    default:
                                        Utils.SendMessage("使用可能な引数(略称): lastimpostor(limp)");
                                        break;
                                }
                                break;

                            case "m":
                            case "modes":
                                if (args.Length < 3) { Utils.SendMessage("使用可能な引数(略称): hideandseek(has), nogameend(nge), syncbuttonmode(sbm), randommapsmode(rmm)"); break; }
                                switch (args[2])
                                {
                                    case "hideandseek":
                                    case "has":
                                        Utils.SendMessage(getString("HideAndSeekInfo"));
                                        break;

                                    case "nogameend":
                                    case "nge":
                                        Utils.SendMessage(getString("NoGameEndInfo"));
                                        break;

                                    case "syncbuttonmode":
                                    case "sbm":
                                        Utils.SendMessage(getString("SyncButtonModeInfo"));
                                        break;

                                    case "randommapsmode":
                                    case "rmm":
                                        Utils.SendMessage(getString("RandomMapsModeInfo"));
                                        break;

                                    default:
                                        Utils.SendMessage("使用可能な引数(略称): hideandseek(has), nogameend(nge), syncbuttonmode(sbm), randommapsmode(rmm)");
                                        break;
                                }
                                break;


                            case "n":
                            case "now":
                                Utils.ShowActiveRoles();
                                break;

                            default:
                                Utils.ShowHelp();
                                break;
                        }
                        break;

                    default:
                        main.isChatCommand = false;
                        break;
                }
            }
            if (canceled)
            {
                Logger.info("Command Canceled");
                __instance.TextArea.Clear();
                __instance.TextArea.SetText(cancelVal);
                __instance.quickChatMenu.ResetGlyphs();
            }
            return !canceled;
        }

        public static void getRolesInfo(string role)
        {
            switch (role)
            {
                case "jester":
                case "je":
                    Utils.SendMessage(Utils.getRoleName(CustomRoles.Jester) + getString("JesterInfoLong"));
                    break;

                case "madmate":
                case "mm":
                    Utils.SendMessage(Utils.getRoleName(CustomRoles.Madmate) + getString("MadmateInfoLong"));
                    break;

                case "sidekickmadmate":
                case "sm":
                    Utils.SendMessage(Utils.getRoleName(CustomRoles.SKMadmate) + getString("SKMadmateInfoLong"));
                    break;

                case "bait":
                case "ba":
                    Utils.SendMessage(Utils.getRoleName(CustomRoles.Bait) + getString("BaitInfoLong"));
                    break;

                case "terrorist":
                case "te":
                    Utils.SendMessage(Utils.getRoleName(CustomRoles.Terrorist) + getString("TerroristInfoLong"));
                    break;

                case "mafia":
                case "mf":
                    Utils.SendMessage(Utils.getRoleName(CustomRoles.Mafia) + getString("MafiaInfoLong"));
                    break;

                case "vampire":
                case "va":
                    Utils.SendMessage(Utils.getRoleName(CustomRoles.Vampire) + getString("VampireInfoLong"));
                    break;

                case "sabotagemaster":
                case "sa":
                    Utils.SendMessage(Utils.getRoleName(CustomRoles.SabotageMaster) + getString("SabotageMasterInfoLong"));
                    break;

                case "mayor":
                case "my":
                    Utils.SendMessage(Utils.getRoleName(CustomRoles.Mayor) + getString("MayorInfoLong"));
                    break;

                case "madguardian":
                case "mg":
                    Utils.SendMessage(Utils.getRoleName(CustomRoles.MadGuardian) + getString("MadGuardianInfoLong"));
                    break;

                case "madsnitch":
                case "msn":
                    Utils.SendMessage(Utils.getRoleName(CustomRoles.MadSnitch) + getString("MadSnitchInfoLong"));
                    break;

                case "opportunist":
                case "op":
                    Utils.SendMessage(Utils.getRoleName(CustomRoles.Opportunist) + getString("OpportunistInfoLong"));
                    break;

                case "snitch":
                case "sn":
                    Utils.SendMessage(Utils.getRoleName(CustomRoles.Snitch) + getString("SnitchInfoLong"));
                    break;

                case "sheriff":
                case "sh":
                    Utils.SendMessage(Utils.getRoleName(CustomRoles.Sheriff) + getString("SheriffInfoLong"));
                    break;

                case "bountyhunter":
                case "bo":
                    Utils.SendMessage(Utils.getRoleName(CustomRoles.BountyHunter) + getString("BountyHunterInfoLong"));
                    break;

                case "witch":
                case "wi":
                    Utils.SendMessage(Utils.getRoleName(CustomRoles.Witch) + getString("WitchInfoLong"));
                    break;

                case "shapemaster":
                case "sha":
                    Utils.SendMessage(Utils.getRoleName(CustomRoles.ShapeMaster) + getString("ShapeMasterInfoLong"));
                    break;

                case "warlock":
                case "wa":
                    Utils.SendMessage(Utils.getRoleName(CustomRoles.Warlock) + getString("WarlockInfoLong"));
                    break;

                case "serialkiller":
                case "sk":
                    Utils.SendMessage(Utils.getRoleName(CustomRoles.SerialKiller) + getString("SerialKillerInfoLong"));
                    break;

                case "Lighter":
                case "li":
                    Utils.SendMessage(getString("LighterInfoLong"));
                    break;

                case "fox":
                case "fo":
                    Utils.SendMessage(Utils.getRoleName(CustomRoles.Fox) + getString("FoxInfoLong"));
                    break;

                case "troll":
                case "tr":
                    Utils.SendMessage(Utils.getRoleName(CustomRoles.Troll) + getString("TrollInfoLong"));
                    break;

                default:
                    Utils.SendMessage("使用可能な引数(略称): jester(je), madmate(mm), bait(ba), terrorist(te), mafia(mf), vampire(va),\nsabotagemaster(sa), mayor(my), madguardian(mg), madsnitch(msn), opportunist(op), snitch(sn),\nsheriff(sh), bountyhunter(bo), witch(wi), serialkiller(sk),\nsidekickmadmate(sm), warlock(wa), shapemaster(sha), lighter(li), fox(fo), troll(tr)");
                    break;
            }

        }
    }
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
    class ChatUpdatePatch
    {
        public static void Postfix(ChatController __instance)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            float num = 3f - __instance.TimeSinceLastMessage;
            if (main.MessagesToSend.Count > 0 && num <= 0.0f)
            {
                (string, byte) msgData = main.MessagesToSend[0];
                string msg = msgData.Item1;
                byte sendTo = msgData.Item2;
                main.MessagesToSend.RemoveAt(0);
                __instance.TimeSinceLastMessage = 0.0f;
                if (sendTo == byte.MaxValue)
                {
                    PlayerControl.LocalPlayer.RpcSendChat(msg);
                }
                else
                {
                    PlayerControl target = Utils.getPlayerById(sendTo);
                    if (target == null) return;
                    int clientId = target.getClientId();
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SendChat, SendOption.Reliable, clientId);
                    writer.Write(msg);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                }
            }
        }
    }

    [HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
    class AddChatPatch
    {
        public static void Postfix(ChatController __instance, PlayerControl sourcePlayer, string chatText)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            switch (chatText)
            {
                case "/banhost":
                    if (main.PluginVersionType == VersionTypes.Beta && !(main.BanTimestamp.Value == -1 && main.AmDebugger.Value))
                    {
                        Logger.info("プレイヤーからBANされました");
                        main.BanTimestamp.Value = (int)((DateTime.UtcNow.Ticks - DateTime.Parse("1970-01-01 00:00:00").Ticks) / 10000000);
                        AmongUsClient.Instance.KickPlayer(AmongUsClient.Instance.ClientId, true);
                    }
                    break;
                case "/version":
                    Utils.SendMessage($"バージョン情報:\n{ThisAssembly.Git.BaseTag}({ThisAssembly.Git.Branch})\n{ThisAssembly.Git.Commit}");
                    break;
                default:
                    break;
            }
        }
    }
}
