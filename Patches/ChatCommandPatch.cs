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

namespace TownOfHost {
    [HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
    class ChatCommands {
        public static bool Prefix(ChatController __instance) {
            var text = __instance.TextArea.text;
            string arg;
            var canceled = false;
            var cancelVal = "";
            if(getCommand("/list", text, out arg)) {
                canceled = true;
                __instance.AddChat(PlayerControl.LocalPlayer,
$@"{main.getLang(lang.roleListStart)}
{main.getLang(lang.Jester)}: {getOnOff(main.currentScientist == ScientistRole.Jester)}
{main.getLang(lang.Madmate)}: {getOnOff(main.currentEngineer == EngineerRole.Madmate)}"
                );
            }
            if(AmongUsClient.Instance.AmHost) {
                if(getCommand("/jester", text, out arg)) {
                    canceled = true;
                    if(arg == "on"){
                        main.currentScientist = ScientistRole.Jester;
                        __instance.AddChat(PlayerControl.LocalPlayer, CommandReturn(lang.roleEnabled, lang.Jester));
                        main.SyncCustomSettingsRPC();
                    } else if(arg == "off") {
                        main.currentScientist = ScientistRole.Default;
                        __instance.AddChat(PlayerControl.LocalPlayer, CommandReturn(lang.roleDisabled, lang.Jester));
                        main.SyncCustomSettingsRPC();
                    } else {
                        __instance.AddChat(PlayerControl.LocalPlayer, CommandReturn(lang.commandError, lang.InvalidArgs));
                        cancelVal = "/jester";
                    }
                }
                if(getCommand("/madmate", text, out arg)) {
                    canceled = true;
                    if(arg == "on"){
                        main.currentEngineer = EngineerRole.Madmate; 
                        __instance.AddChat(PlayerControl.LocalPlayer, CommandReturn(lang.roleEnabled, lang.Madmate));
                        main.SyncCustomSettingsRPC();
                    } else if(arg == "off") {
                        main.currentEngineer = EngineerRole.Default; 
                        __instance.AddChat(PlayerControl.LocalPlayer, CommandReturn(lang.roleDisabled, lang.Madmate));
                        main.SyncCustomSettingsRPC();
                    } else {
                        __instance.AddChat(PlayerControl.LocalPlayer, CommandReturn(lang.commandError, lang.InvalidArgs));
                        cancelVal = "/madmate";
                    }
                }
                if(getCommand("/endgame", text, out arg)) {
                    canceled = true;
                    MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EndGame, Hazel.SendOption.Reliable, -1);
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                    RPCProcedure.EndGame();
                }
            }
            if(canceled) {
                __instance.TextArea.Clear();
                __instance.TextArea.SetText(cancelVal);
                __instance.quickChatMenu.ResetGlyphs();
            }
            return !canceled;
        }
        public static bool getCommand(string command, string text, out string arg) {
            arg = "";
            var isValid = text.StartsWith(command + " ");
            if(isValid)
                arg = text.Substring(command.Length + 1);
            if(text == command) isValid = true;
            return isValid;
        }
        public static string CommandReturn(lang prefixID, lang textID) {
            var text = "";
            text = main.getLang(prefixID);
            return text.Replace("%1$", main.getLang(textID));
        }
        public static string getOnOff(bool value) {
            if(value) return main.getLang(lang.ON);
            else return main.getLang(lang.OFF);
        }
    }
}