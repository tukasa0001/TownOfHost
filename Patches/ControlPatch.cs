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
using Hazel;

namespace TownOfHost {
    [HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.Update))]
    class DebugManager {
        static System.Random random = new System.Random();
        public static void Postfix(KeyboardJoystick __instance) {
            if(Input.GetKeyDown(KeyCode.L) && Input.GetKey(KeyCode.LeftShift) && AmongUsClient.Instance.AmHost) {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EndGame, Hazel.SendOption.Reliable, -1);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPCProcedure.EndGame();
            }
            if(Input.GetKeyDown(KeyCode.X) && AmongUsClient.Instance.GameMode == GameModes.FreePlay) {
                PlayerControl.LocalPlayer.Data.Object.SetKillTimer(0f);
            }
            if(Input.GetKeyDown(KeyCode.Y) && AmongUsClient.Instance.AmHost) {
                main.SyncCustomSettingsRPC();
            }
            if(Input.GetKeyDown(KeyCode.G) && AmongUsClient.Instance.GameMode == GameModes.FreePlay) {
                HudManager.Instance.StartCoroutine(HudManager.Instance.CoFadeFullScreen(Color.clear, Color.black));
                var list = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
                list.Add(PlayerControl.LocalPlayer);
                HudManager.Instance.StartCoroutine(DestroyableSingleton<HudManager>.Instance.CoShowIntro(list));
            }
            if(Input.GetKeyDown(KeyCode.M) && AmongUsClient.Instance.GameMode == GameModes.FreePlay) {
                foreach(var pc in PlayerControl.AllPlayerControls) {
                    if(pc.PlayerId != 0) pc.nameText.text = "まっちゃ";
                }
            }

            if(main.OptionControllerIsEnable) {
                if(Input.GetKeyDown(KeyCode.UpArrow)) CustomOptionController.Up();
                if(Input.GetKeyDown(KeyCode.DownArrow)) CustomOptionController.Down();
                if(Input.GetKeyDown(KeyCode.RightArrow)) {
                    CustomOptionController.Enter();
                }
                if(Input.GetKeyDown(KeyCode.LeftArrow)) {
                    CustomOptionController.Return();
                }
            }
            if(Input.GetKeyDown(KeyCode.Tab) && AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Joined) {
                main.OptionControllerIsEnable = !main.OptionControllerIsEnable;
                CustomOptionController.currentPage = OptionPages.basepage;
                CustomOptionController.currentCursor = 0;
            }
        }
    }
}