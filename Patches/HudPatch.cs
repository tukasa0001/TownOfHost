using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using System;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnhollowerBaseLib;
using TownOfHost;

namespace TownOfHost {
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    class HudManagerPatch {
        public static void Postfix(HudManager __instance) {
            var TaskTextPrefix = "";
            //壁抜け
            if(Input.GetKeyDown(KeyCode.LeftControl)) {
                if(AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started ||
                AmongUsClient.Instance.GameMode == GameModes.FreePlay) {
                    PlayerControl.LocalPlayer.Collider.offset = new Vector2(0f,127f);
                }
            }
            //壁抜け解除
            if(PlayerControl.LocalPlayer.Collider.offset.y == 127f) {
                if(!Input.GetKey(KeyCode.LeftControl)) {
                    PlayerControl.LocalPlayer.Collider.offset = new Vector2(0f,-0.3636f);
                }
            }
            //Madmateのベントボタンの画像変更
            if(PlayerControl.LocalPlayer.Data.Role.Role == RoleTypes.Engineer && main.MadmateEnabled) {
                TaskTextPrefix = "<color=#ff0000>" + main.getLang(lang.Madmate) + "</color>\r\n" +
                "<color=#ff0000>" + main.getLang(lang.MadmateInfo) + "</color>\r\n";
            }
            //Jesterのバイタルボタンのテキストを変更
            if(PlayerControl.LocalPlayer.Data.Role.Role == RoleTypes.Scientist && main.JesterEnabled) {
                TaskTextPrefix = "<color=#d161a4>" + main.getLang(lang.Jester) + "</color>\r\n" +
                "<color=#d161a4>" + main.getLang(lang.JesterInfo) + "</color>\r\n";
            }
            if(!__instance.TaskText.text.Contains(TaskTextPrefix)) {
                __instance.TaskText.text = TaskTextPrefix + "\r\n" + __instance.TaskText.text;
            }
        }
    }
}