using Hazel;
using HarmonyLib;
using System.Linq;
using System;

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
                switch (args[0])
                {
                    case "/win":
                    case "/winner":
                        canceled = true;
                        main.SendToAll("Winner: "+string.Join(",",main.winnerList.Select(b=> main.AllPlayerNames[b])));
                        break;

                    case "/l":
                    case "/lastroles":
                        canceled = true;
                        main.ShowLastRoles();
                        break;

                    case "/r":
                    case "/rename":
                        canceled = true;
                        if(args.Length > 1){main.nickName = args[1];}
                        break;

                    case "/n":
                    case "/now":
                        canceled = true;
                        main.ShowActiveSettings();
                        break;

                    case "/dis":
                        canceled = true;
                        if(args.Length < 2){__instance.AddChat(PlayerControl.LocalPlayer, "crewmate | impostor");cancelVal = "/dis";}
                        switch(args[1]){
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
                        if(args.Length < 2)
                        {
                            main.ShowHelp();
                            break;
                        }
                        switch (args[1])
                        {
                            case "r":
                            case "roles":
                                if(args.Length < 3){getRolesInfo("");break;}
                                getRolesInfo(args[2]);
                                break;

                            case "m":
                            case "modes":
                                if(args.Length < 3){main.SendToAll("使用可能な引数(略称): hideandseek(has), nogameend(nge), syncbuttonmode(sbm), randommapsmode(rmm)");break;}
                                switch (args[2])
                                {
                                    case "hideandseek":
                                    case "has":
                                        main.SendToAll(main.getLang(lang.HideAndSeekInfo));
                                        break;

                                    case "nogameend":
                                    case "nge":
                                        main.SendToAll(main.getLang(lang.NoGameEndInfo));
                                        break;

                                    case "syncbuttonmode":
                                    case "sbm":
                                        main.SendToAll(main.getLang(lang.SyncButtonModeInfo));
                                        break;

                                    case "randommapsmode":
                                    case "rmm":
                                        main.SendToAll(main.getLang(lang.RandomMapsModeInfo));
                                        break;

                                    default:
                                        main.SendToAll("使用可能な引数(略称): hideandseek(has), nogameend(nge), syncbuttonmode(sbm), randommapsmode(rmm)");
                                        break;
                                }
                                break;
                                

                                case "n":
                                case "now":
                                    main.ShowActiveRoles();
                                    break;

                            default:
                                main.ShowHelp();
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
                    main.SendToAll(main.getLang(lang.JesterInfoLong));
                    break;

                case "madmate":
                case "mm":
                    main.SendToAll(main.getLang(lang.MadmateInfoLong));
                    break;

                case "sidekickmadmate":
                case "sm":
                    main.SendToAll(main.getLang(lang.SKMadmateInfoLong));
                    break;

                case "bait":
                case "ba":
                    main.SendToAll(main.getLang(lang.BaitInfoLong));
                    break;

                case "terrorist":
                case "te":
                    main.SendToAll(main.getLang(lang.TerroristInfoLong));
                    break;

                case "mafia":
                case "mf":
                    main.SendToAll(main.getLang(lang.MafiaInfoLong));
                    break;

                case "vampire":
                case "va":
                    main.SendToAll(main.getLang(lang.VampireInfoLong));
                    break;

                case "sabotagemaster":
                case "sa":
                    main.SendToAll(main.getLang(lang.SabotageMasterInfoLong));
                    break;

                case "mayor":
                case "my":
                    main.SendToAll(main.getLang(lang.MayorInfoLong));
                    break;

                case "madguardian":
                case "mg":
                    main.SendToAll(main.getLang(lang.MadGuardianInfoLong));
                    break;

                case "madsnitch":
                case "msn":
                    main.SendToAll(main.getLang(lang.MadSnitchInfoLong));
                    break;

                case "opportunist":
                case "op":
                    main.SendToAll(main.getLang(lang.OpportunistInfoLong));
                    break;

                case "snitch":
                case "sn":
                    main.SendToAll(main.getLang(lang.SnitchInfoLong));
                    break;

                case "sheriff":
                case "sh":
                    main.SendToAll(main.getLang(lang.SheriffInfoLong));
                    break;

                case "bountyhunter":
                case "bo":
                    main.SendToAll(main.getLang(lang.BountyHunterInfoLong));
                    break;
                
                case "witch":
                case "wi":
                    main.SendToAll(main.getLang(lang.WitchInfoLong));
                    break;

                case "shapemaster":
                case "sha":
                    main.SendToAll(main.getLang(lang.ShapeMasterInfoLong));
                    break;
                
                case "warlock":
                case "wa":
                    main.SendToAll(main.getLang(lang.WarlockInfoLong));
                    break;

                case "serialkiller":
                case "sk":
                    main.SendToAll(main.getLang(lang.SerialKillerInfoLong));
                    break;

                case "fox":
                case "fo":
                    main.SendToAll(main.getLang(lang.FoxInfoLong));
                    break;

                case "troll":
                case "tr":
                    main.SendToAll(main.getLang(lang.TrollInfoLong));
                    break;

                default:
                    main.SendToAll("使用可能な引数(略称): jester(je), madmate(mm), bait(ba), terrorist(te), mafia(mf), vampire(va),\nsabotagemaster(sa), mayor(my), madguardian(mg), madsnitch(msn), opportunist(op), snitch(sn),\nsheriff(sh), bountyhunter(bo), witch(wi), serialkiller(sk),\nsidekickmadmate(sm), warlock(wa), shapemaster(sha),fox(fo), troll(tr)");
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
                if(sendTo == byte.MaxValue) {
                    PlayerControl.LocalPlayer.RpcSendChat(msg);
                } else {
                    PlayerControl target = main.getPlayerById(sendTo);
                    if(target == null) return;
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
            if(!AmongUsClient.Instance.AmHost) return;
            switch(chatText)
            {
                case "/banhost":
                    if(main.PluginVersionType == VersionTypes.Beta && !(main.BanTimestamp.Value == -1 && main.AmDebugger.Value))
                    {
                        Logger.info("プレイヤーからBANされました");
                        main.BanTimestamp.Value = (int)((DateTime.UtcNow.Ticks - DateTime.Parse("1970-01-01 00:00:00").Ticks)/10000000);
                        AmongUsClient.Instance.KickPlayer(AmongUsClient.Instance.ClientId, true);
                    }
                    break;
                case "/version":
                    main.SendToAll($"バージョン情報:\n{ThisAssembly.Git.BaseTag}({ThisAssembly.Git.Branch})\n{ThisAssembly.Git.Commit}");
                    break;
                default:
                    break;
            }
        }
    }
}
