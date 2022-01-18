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

namespace TownOfHost
{
    [HarmonyPatch(typeof(KeyboardJoystick), nameof(KeyboardJoystick.Update))]
    class DebugManager
    {
        static System.Random random = new System.Random();
        public static void Postfix(KeyboardJoystick __instance)
        {
            if (Input.GetKeyDown(KeyCode.L) && Input.GetKey(KeyCode.LeftShift) && AmongUsClient.Instance.AmHost)
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EndGame, Hazel.SendOption.Reliable, -1);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPCProcedure.EndGame();
            }
            if (Input.GetKeyDown(KeyCode.X) && AmongUsClient.Instance.GameMode == GameModes.FreePlay)
            {
                PlayerControl.LocalPlayer.Data.Object.SetKillTimer(0f);
            }
            if (Input.GetKeyDown(KeyCode.Y) && AmongUsClient.Instance.AmHost)
            {
                main.SyncCustomSettingsRPC();
            }
            if (Input.GetKeyDown(KeyCode.M))
            {
                if (AmongUsClient.Instance.GameMode == GameModes.FreePlay)
                {
                    MeetingHud.Instance.RpcClose();
                }
            }
            if (Input.GetKeyDown(KeyCode.O))
            {
                if (AmongUsClient.Instance.GameMode == GameModes.FreePlay)
                {
                    foreach (var pc in PlayerControl.AllPlayerControls)
                    {
                        foreach (var task in pc.myTasks)
                        {
                            pc.RpcCompleteTask(task.Id);
                        }
                    }
                }
            }
            if (Input.GetKey(KeyCode.B) && AmongUsClient.Instance.GameMode == GameModes.FreePlay)
            {
                Logger.SendToFile("none");
            }
            if (Input.GetKeyDown(KeyCode.G) && AmongUsClient.Instance.GameMode == GameModes.FreePlay)
            {
                HudManager.Instance.StartCoroutine(HudManager.Instance.CoFadeFullScreen(Color.clear, Color.black));
                var list = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
                list.Add(PlayerControl.LocalPlayer);
                HudManager.Instance.StartCoroutine(DestroyableSingleton<HudManager>.Instance.CoShowIntro(list));
            }
            if (Input.GetKeyDown(KeyCode.Equals) && AmongUsClient.Instance.GameMode == GameModes.FreePlay)
            {
                main.VisibleTasksCount = !main.VisibleTasksCount;
                DestroyableSingleton<HudManager>.Instance.Notifier.AddItem("VisibleTaskCountが" + main.VisibleTasksCount.ToString() + "に変更されました。");
            }

            if (Input.GetKeyDown(KeyCode.Tab) && AmongUsClient.Instance.GameState == InnerNet.InnerNetClient.GameStates.Joined)
            {
                //Logger.SendInGame("tabキーが押されました");
                main.OptionControllerIsEnable = !main.OptionControllerIsEnable;
                CustomOptionController.currentPage = OptionPages.basepage;
                CustomOptionController.currentCursor = 0;
            }
            if (main.OptionControllerIsEnable)
            {
                main.TextCursorTimer += Time.deltaTime;
                if (main.TextCursorTimer > 0.5f)
                {
                    main.TextCursorTimer = 0f;
                    main.TextCursorVisible = !main.TextCursorVisible;
                }
                if (Input.GetKeyDown(KeyCode.UpArrow)) CustomOptionController.Up();
                if (Input.GetKeyDown(KeyCode.DownArrow)) CustomOptionController.Down();
                if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    CustomOptionController.Enter();
                }
                if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    CustomOptionController.Return();
                }
                if (Input.GetKeyDown(KeyCode.Alpha0)) CustomOptionController.Input(0);
                if (Input.GetKeyDown(KeyCode.Alpha1)) CustomOptionController.Input(1);
                if (Input.GetKeyDown(KeyCode.Alpha2)) CustomOptionController.Input(2);
                if (Input.GetKeyDown(KeyCode.Alpha3)) CustomOptionController.Input(3);
                if (Input.GetKeyDown(KeyCode.Alpha4)) CustomOptionController.Input(4);
                if (Input.GetKeyDown(KeyCode.Alpha5)) CustomOptionController.Input(5);
                if (Input.GetKeyDown(KeyCode.Alpha6)) CustomOptionController.Input(6);
                if (Input.GetKeyDown(KeyCode.Alpha7)) CustomOptionController.Input(7);
                if (Input.GetKeyDown(KeyCode.Alpha8)) CustomOptionController.Input(8);
                if (Input.GetKeyDown(KeyCode.Alpha9)) CustomOptionController.Input(9);
            }
        }
    }
}
