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
{main.getLang(lang.Jester)}: {getOnOff(main.currentScientist == ScientistRole.Jester)}
{main.getLang(lang.Madmate)}: {getOnOff(main.currentEngineer == EngineerRole.Madmate)}
{main.getLang(lang.Bait)}: {getOnOff(main.currentScientist == ScientistRole.Bait)}
{main.getLang(lang.Terrorist)}: {getOnOff(main.currentEngineer == EngineerRole.Terrorist)}"
                );
            }
            if (AmongUsClient.Instance.AmHost)
            {
                if (getCommand("/winner", text, out arg))
                {
                    canceled = true;
                    main.SendToAll(main.winnerList);
                }
                if (getCommand("/help roles", text, out arg))
                {
                    canceled = true;
                    if (arg == "jester")
                    {
                        main.SendToAll("Jester(Scientist):投票で追放されたときに単独勝利となる第三陣営の役職。追放されずにゲームが終了するか、キルされると敗北となる。");
                    }
                    else if (arg == "madmate")
                    {
                        main.SendToAll("Jester(Scientist):投票で追放されたときに単独勝利となる第三陣営の役職。追放されずにゲームが終了するか、キルされると敗北となる。");
                    }
                }
                if (getCommand("/help modes", text, out arg))
                {
                    canceled = true;
                    if (arg == "hide and seak")
                    {
                        main.SendToAll("HideAndSeek:クルー陣営は全てのタスクを完了させると勝利する。インポスター陣営は全てのクルーをキルすると勝利する。サボタージュやアドミン、カメラ、幽霊が生存者に位置情報を伝える行為、待ち伏せは禁止事項である。死体の通報や緊急会議ボタン、サボタージュ勝利をすることはできない。");
                    }
                }
                if (getCommand("/jester", text, out arg))
                {
                    canceled = true;
                    if (arg == "on")
                    {
                        main.currentScientist = ScientistRole.Jester;
                        __instance.AddChat(PlayerControl.LocalPlayer, CommandReturn(lang.roleEnabled, lang.Jester));
                        main.SyncCustomSettingsRPC();
                    }
                    else if (arg == "off")
                    {
                        main.currentScientist = ScientistRole.Default;
                        __instance.AddChat(PlayerControl.LocalPlayer, CommandReturn(lang.roleDisabled, lang.Jester));
                        main.SyncCustomSettingsRPC();
                    }
                    else
                    {
                        __instance.AddChat(PlayerControl.LocalPlayer, CommandReturn(lang.commandError, lang.InvalidArgs));
                        cancelVal = "/jester";
                    }
                }
                if (getCommand("/madmate", text, out arg))
                {
                    canceled = true;
                    if (arg == "on")
                    {
                        main.currentEngineer = EngineerRole.Madmate;
                        __instance.AddChat(PlayerControl.LocalPlayer, CommandReturn(lang.roleEnabled, lang.Madmate));
                        main.SyncCustomSettingsRPC();
                    }
                    else if (arg == "off")
                    {
                        main.currentEngineer = EngineerRole.Default;
                        __instance.AddChat(PlayerControl.LocalPlayer, CommandReturn(lang.roleDisabled, lang.Madmate));
                        main.SyncCustomSettingsRPC();
                    }
                    else
                    {
                        __instance.AddChat(PlayerControl.LocalPlayer, CommandReturn(lang.commandError, lang.InvalidArgs));
                        cancelVal = "/madmate";
                    }
                }
                if (getCommand("/bait", text, out arg))
                {
                    canceled = true;
                    if (arg == "on")
                    {
                        main.currentScientist = ScientistRole.Bait;
                        __instance.AddChat(PlayerControl.LocalPlayer, CommandReturn(lang.roleEnabled, lang.Bait));
                        main.SyncCustomSettingsRPC();
                    }
                    else if (arg == "off")
                    {
                        main.currentScientist = ScientistRole.Default;
                        __instance.AddChat(PlayerControl.LocalPlayer, CommandReturn(lang.roleDisabled, lang.Bait));
                        main.SyncCustomSettingsRPC();
                    }
                    else
                    {
                        __instance.AddChat(PlayerControl.LocalPlayer, CommandReturn(lang.commandError, lang.InvalidArgs));
                        cancelVal = "/bait";
                    }
                }
                if (getCommand("/terrorist", text, out arg))
                {
                    canceled = true;
                    if (arg == "on")
                    {
                        main.currentEngineer = EngineerRole.Terrorist;
                        __instance.AddChat(PlayerControl.LocalPlayer, CommandReturn(lang.roleEnabled, lang.Terrorist));
                        main.SyncCustomSettingsRPC();
                    }
                    else if (arg == "off")
                    {
                        main.currentEngineer = EngineerRole.Default;
                        __instance.AddChat(PlayerControl.LocalPlayer, CommandReturn(lang.roleDisabled, lang.Terrorist));
                        main.SyncCustomSettingsRPC();
                    }
                    else
                    {
                        __instance.AddChat(PlayerControl.LocalPlayer, CommandReturn(lang.commandError, lang.InvalidArgs));
                        cancelVal = "/terrorist";
                    }
                }
                if (getCommand("/sidekick", text, out arg))
                {
                    canceled = true;
                    if (arg == "on")
                    {
                        main.currentShapeshifter = ShapeshifterRoles.Sidekick;
                        __instance.AddChat(PlayerControl.LocalPlayer, CommandReturn(lang.roleEnabled, lang.Sidekick));
                        main.SyncCustomSettingsRPC();
                    }
                    else if (arg == "off")
                    {
                        main.currentShapeshifter = ShapeshifterRoles.Sidekick;
                        __instance.AddChat(PlayerControl.LocalPlayer, CommandReturn(lang.roleDisabled, lang.Sidekick));
                        main.SyncCustomSettingsRPC();
                    }
                    else
                    {
                        __instance.AddChat(PlayerControl.LocalPlayer, CommandReturn(lang.commandError, lang.InvalidArgs));
                        cancelVal = "/sidekick";
                    }
                }
                if (getCommand("/hideandseek", text, out arg))
                {
                    canceled = true;
                    if (arg == "on")
                    {
                        main.IsHideAndSeek = true;
                        __instance.AddChat(PlayerControl.LocalPlayer, "HideAndSeekが有効化されました");
                    }
                    else if (arg == "off")
                    {
                        main.IsHideAndSeek = false;
                        __instance.AddChat(PlayerControl.LocalPlayer, "HideAndSeekが無効化されました");
                    }
                    else
                    {
                        __instance.AddChat(PlayerControl.LocalPlayer, CommandReturn(lang.commandError, lang.InvalidArgs));
                        cancelVal = "/hideandseek";
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
            if (chatText == "/winner" && AmongUsClient.Instance.AmHost && main.IgnoreWinnerCommand.Value == false)
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
