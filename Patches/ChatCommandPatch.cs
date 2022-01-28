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

namespace TownOfHost
{
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
    class ChatCommands
    {
        public static bool Prefix(ChatController __instance)
        {
            var text = __instance.TextArea.text;
            string arg;
            var canceled = false;
            var cancelVal = "";
            if (AmongUsClient.Instance.AmHost)
            {
                if (getCommand("/winner", text, out arg) || getCommand("/win", text, out arg))
                {
                    canceled = true;
                    main.SendToAll(main.winnerList);
                }

                if (getCommand("/h n", text, out arg) || getCommand("/h now", text, out arg))
                {
                    canceled = true;
                    main.SendToAll("現在有効になっている設定の説明:");
                    if(main.IsHideAndSeek)
                    {
                        main.SendToAll(main.getLang(lang.HideAndSeekInfo));
                        if(main.FoxCount > 0 ){ main.SendToAll(main.getLang(lang.FoxInfoLong)); }
                        if(main.TrollCount > 0 ){ main.SendToAll(main.getLang(lang.TrollInfoLong)); }
                    }else{
                        if(main.SyncButtonMode){ main.SendToAll(main.getLang(lang.SyncButtonModeInfo)); }
                        if(main.currentImpostor == ImpostorRoles.Vampire){ main.SendToAll(main.getLang(lang.VampireInfoLong)); }
                        if(main.currentShapeshifter == ShapeshifterRoles.Sidekick){ main.SendToAll(main.getLang(lang.SidekickInfoLong)); }
                        if(main.currentEngineer == EngineerRoles.Madmate){ main.SendToAll(main.getLang(lang.MadmateInfoLong)); }
                        if(main.currentEngineer == EngineerRoles.Terrorist){ main.SendToAll(main.getLang(lang.TerroristInfoLong)); }
                        if(main.currentScientist == ScientistRoles.Bait){ main.SendToAll(main.getLang(lang.BaitInfoLong)); }
                        if(main.currentScientist == ScientistRoles.Jester){ main.SendToAll(main.getLang(lang.JesterInfoLong)); }
                        if(main.currentScientist == ScientistRoles.SabotageMaster){ main.SendToAll(main.getLang(lang.SabotageMasterInfoLong)); }
                        if(main.currentScientist == ScientistRoles.Mayor) { main.SendToAll(main.getLang(lang.MayorInfoLong)); }
                    }
                    if(main.NoGameEnd){ main.SendToAll(main.getLang(lang.NoGameEndInfo)); }
                }
                if (getCommand("/h r", text, out arg) || getCommand("/h roles", text, out arg))
                {
                    canceled = true;
                    if (arg == "")
                    {
                        __instance.AddChat(PlayerControl.LocalPlayer, "使用可能な引数(略称): jester(je), madmate(ma), bait(ba), terrorist(te), sidekick(si), vampire(va), sabotagemaster(sa), mayor(may), opportunist(op), fox(fo), troll(tr)");
                    }
                    else if (arg == "jester" || arg == "je")
                    {
                        main.SendToAll(main.getLang(lang.JesterInfoLong));
                    }
                    else if (arg == "madmate" || arg == "ma")
                    {
                        main.SendToAll(main.getLang(lang.MadmateInfoLong));
                    }
                    else if (arg == "bait" || arg == "ba")
                    {
                        main.SendToAll(main.getLang(lang.BaitInfoLong));
                    }
                    else if (arg == "terrorist" || arg == "te")
                    {
                        main.SendToAll(main.getLang(lang.TerroristInfoLong));
                    }
                    else if (arg == "sidekick" || arg == "si")
                    {
                        main.SendToAll(main.getLang(lang.SidekickInfoLong));
                    }
                    else if (arg == "vampire" || arg == "va")
                    {
                        main.SendToAll(main.getLang(lang.VampireInfoLong));
                    }
                    else if (arg == "sabotagemaster" || arg == "sa")
                    {
                        main.SendToAll(main.getLang(lang.SabotageMasterInfoLong));
                    }
                    else if (arg == "mayor" || arg == "may")
                    {
                        main.SendToAll(main.getLang(lang.MayorInfoLong));
                    }
                    else if (arg == "opportunist" || arg == "op")
                    {
                        main.SendToAll(main.getLang(lang.OpportunistInfoLong));
                    }
                    else if (arg == "fox" || arg == "fo")
                    {
                        main.SendToAll(main.getLang(lang.FoxInfoLong));
                    }
                    else if (arg == "troll" || arg == "tr")
                    {
                        main.SendToAll(main.getLang(lang.TrollInfoLong));
                    }
                    else
                    {
                        __instance.AddChat(PlayerControl.LocalPlayer, CommandReturn(lang.commandError, lang.InvalidArgs));
                    }
                }
                if (getCommand("/h m", text, out arg) || getCommand("/h modes", text, out arg))
                {
                    canceled = true;
                    if (arg == "")
                    {
                        __instance.AddChat(PlayerControl.LocalPlayer, "使用可能な引数(略称): hideandseek(has), nogameend(nge), syncbuttonmode(sbm)");
                    }
                    else if (arg == "hideandseek" || arg == "has")
                    {
                        main.SendToAll(main.getLang(lang.HideAndSeekInfo));
                    }
                    else if (arg == "nogameend" || arg == "nge")
                    {
                        main.SendToAll(main.getLang(lang.NoGameEndInfo));
                    }
                    else if (arg == "syncbuttonmode" || arg == "sbm")
                    {
                        main.SendToAll(main.getLang(lang.SyncButtonModeInfo));
                    }
                    else
                    {
                        __instance.AddChat(PlayerControl.LocalPlayer, CommandReturn(lang.commandError, lang.InvalidArgs));
                    }
                }
                if (getCommand("/endgame", text, out arg))
                {
                    canceled = true;
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EndGame, Hazel.SendOption.Reliable, -1);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    RPCProcedure.EndGame();
                }
                if (getCommand("/dis", text, out arg))
                {
                    canceled = true;
                    if (arg == "crewmate")
                    {
                        ShipStatus.Instance.enabled = false;
                        ShipStatus.RpcEndGame(GameOverReason.HumansDisconnect, false);
                    }
                    else
                    if (arg == "impostor")
                    {
                        ShipStatus.Instance.enabled = false;
                        ShipStatus.RpcEndGame(GameOverReason.ImpostorDisconnect, false);
                    }
                    else
                    if (arg == "")
                    {
                        __instance.AddChat(PlayerControl.LocalPlayer, "crewmate | impostor");
                        cancelVal = "/dis";
                    }
                    else
                    {
                        __instance.AddChat(PlayerControl.LocalPlayer, CommandReturn(lang.commandError, lang.InvalidArgs));
                    }
                    ShipStatus.Instance.RpcRepairSystem(SystemTypes.Admin, 0);
                }
            }
            if (canceled)
            {
                __instance.TextArea.Clear();
                __instance.TextArea.SetText(cancelVal);
                __instance.quickChatMenu.ResetGlyphs();
            }
            return !canceled;
        }
        public static bool getCommand(string command, string text, out string arg)
        {
            arg = "";
            var isValid = text.StartsWith(command + " ");
            if (isValid)
                arg = text.Substring(command.Length + 1);
            if (text == command) isValid = true;
            return isValid;
        }
        public static string CommandReturn(lang prefixID, lang textID)
        {
            var text = "";
            text = main.getLang(prefixID);
            return text.Replace("%1$", main.getLang(textID));
        }
        public static string getOnOff(bool value)
        {
            if (value) return main.getLang(lang.ON);
            else return main.getLang(lang.OFF);
        }
    }
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
    class AddChatPatch
    {
        public static void Postfix(ChatController __instance, [HarmonyArgument(1)] string chatText)
        {
            Logger.SendToFile(__instance.name + ":" + chatText, LogLevel.Message);
            if ((chatText == "/winner" || chatText == "/win") && AmongUsClient.Instance.AmHost && main.IgnoreWinnerCommand.Value == false)
            {
                main.SendToAll(main.winnerList);
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
                string msg = main.MessagesToSend[0];
                main.MessagesToSend.RemoveAt(0);
                __instance.TimeSinceLastMessage = 0.0f;
                PlayerControl.LocalPlayer.RpcSendChat(msg);
            }
        }
    }
}
