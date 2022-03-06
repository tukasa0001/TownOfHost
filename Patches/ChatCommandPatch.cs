using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using Hazel;
using System;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnhollowerBaseLib;
using TownOfHost;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

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

                case "fox":
                case "fo":
                    main.SendToAll(main.getLang(lang.FoxInfoLong));
                    break;

                case "troll":
                case "tr":
                    main.SendToAll(main.getLang(lang.TrollInfoLong));
                    break;

                default:
                    main.SendToAll("使用可能な引数(略称): jester(je), madmate(mm), bait(ba), terrorist(te), mafia(mf), vampire(va),\nsabotagemaster(sa), mayor(my), madguardian(mg), opportunist(op), snitch(sn), sheriff(sh),\nbountyhunter(bo), witch(wi), fox(fo), troll(tr)");
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
}
