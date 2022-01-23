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
            if (getCommand("/list", text, out arg))
            {
                canceled = true;
                __instance.AddChat(PlayerControl.LocalPlayer,
$@"{main.getLang(lang.roleListStart)}
{main.getRoleName(RoleNames.Sidekick)}: {getOnOff(main.currentShapeshifter == ShapeshifterRoles.Sidekick)}
{main.getRoleName(RoleNames.Vampire)}: {getOnOff(main.currentImpostor == ImpostorRoles.Vampire)}
{main.getRoleName(RoleNames.Jester)}: {getOnOff(main.currentScientist == ScientistRoles.Jester)}
{main.getRoleName(RoleNames.Madmate)}: {getOnOff(main.currentEngineer == EngineerRoles.Madmate)}
{main.getRoleName(RoleNames.Bait)}: {getOnOff(main.currentScientist == ScientistRoles.Bait)}
{main.getRoleName(RoleNames.Terrorist)}: {getOnOff(main.currentEngineer == EngineerRoles.Terrorist)}"
                );
            }
            if (AmongUsClient.Instance.AmHost)
            {
                if (getCommand("/winner", text, out arg) || getCommand("/win", text, out arg))
                {
                    canceled = true;
                    main.SendToAll(main.winnerList);
                }
                
                if (getCommand("/h now", text, out arg))
                {
                    canceled = true;
                    main.SendToAll("現在有効になっている設定の説明:");
                    if(main.currentImpostor == ImpostorRoles.Vampire){ main.SendToAll(main.getLang(lang.VampireInfoLong)); }
                    if(main.currentShapeshifter == ShapeshifterRoles.Sidekick){ main.SendToAll(main.getLang(lang.SidekickInfoLong)); }
                    if(main.currentEngineer == EngineerRoles.Madmate){ main.SendToAll(main.getLang(lang.MadmateInfoLong)); }
                    if(main.currentEngineer == EngineerRoles.Terrorist){ main.SendToAll(main.getLang(lang.TerroristInfoLong)); }
                    if(main.currentScientist == ScientistRoles.Bait){ main.SendToAll(main.getLang(lang.BaitInfoLong)); }
                    if(main.currentScientist == ScientistRoles.Jester){ main.SendToAll(main.getLang(lang.JesterInfoLong)); }
                    if(main.FoxCount > 0 ){ main.SendToAll(main.getLang(lang.FoxInfoLong)); }
                    if(main.TrollCount > 0 ){ main.SendToAll(main.getLang(lang.TrollInfoLong)); }
                    if(main.IsHideAndSeek){ main.SendToAll(main.getLang(lang.HideAndSeekInfo)); }
                    if(main.NoGameEnd){ main.SendToAll(main.getLang(lang.NoGameEndInfo)); }
                    if(main.SyncButtonMode){ main.SendToAll(main.getLang(lang.SyncButtonModeInfo)); }
                }
                if (getCommand("/h roles", text, out arg))
                {
                    canceled = true;
                    if (arg == "")
                    {
                        __instance.AddChat(PlayerControl.LocalPlayer, "使用可能な引数: jester, madmate, bait, terrorist, sidekick, vampire, fox, troll");
                    }
                    else if (arg == "jester")
                    {
                        main.SendToAll(main.getLang(lang.JesterInfoLong));
                    }
                    else if (arg == "madmate")
                    {
                        main.SendToAll(main.getLang(lang.MadmateInfoLong));
                    }
                    else if (arg == "bait")
                    {
                        main.SendToAll(main.getLang(lang.BaitInfoLong));
                    }
                    else if (arg == "terrorist")
                    {
                        main.SendToAll(main.getLang(lang.TerroristInfoLong));
                    }
                    else if (arg == "sidekick")
                    {
                        main.SendToAll(main.getLang(lang.SidekickInfoLong));
                    }
                    else if (arg == "vampire")
                    {
                        main.SendToAll(main.getLang(lang.VampireInfoLong));
                    }
                    else if (arg == "fox")
                    {
                        main.SendToAll(main.getLang(lang.FoxInfoLong));
                    }
                    else if (arg == "troll")
                    {
                        main.SendToAll(main.getLang(lang.TrollInfoLong));
                    }
                    else
                    {
                        __instance.AddChat(PlayerControl.LocalPlayer, "Error:入力された役職は存在しません。");
                    }
                }
                if (getCommand("/h modes", text, out arg))
                {
                    canceled = true;
                    if (arg == "")
                    {
                        __instance.AddChat(PlayerControl.LocalPlayer, "使用可能な引数: hideandseek, nogameend, syncbuttonmode");
                    }
                    else if (arg == "hideandseek")
                    {
                        main.SendToAll(main.getLang(lang.HideAndSeekInfo));
                    }
                    else if (arg == "nogameend")
                    {
                        main.SendToAll(main.getLang(lang.NoGameEndInfo));
                    }
                    else if (arg == "syncbuttonmode")
                    {
                        main.SendToAll(main.getLang(lang.SyncButtonModeInfo));
                    }
                    else
                    {
                        __instance.AddChat(PlayerControl.LocalPlayer, "Error:入力されたモードは存在しません。");
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
                    {
                        __instance.AddChat(PlayerControl.LocalPlayer, "crewmate | impostor");
                        cancelVal = "/dis";
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
