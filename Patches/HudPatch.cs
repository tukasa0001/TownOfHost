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
            //デバッグ用テキスト
            if(Input.GetKeyDown(KeyCode.U) && Input.GetKey(KeyCode.LeftShift)) {
                __instance.Dialogue.enabled = true;
                
                __instance.Dialogue.Show("何も設定されていません");
            }
        }
    }
}