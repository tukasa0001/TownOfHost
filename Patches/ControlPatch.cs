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
        private static Il2CppSystem.Collections.Generic.List<PlayerControl> bots = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
        static System.Random random = new System.Random();
        public static void Postfix(KeyboardJoystick __instance) {
            // Spawn dummys
            if (Input.GetKeyDown(KeyCode.Backslash) && Input.GetKey(KeyCode.LeftShift) && AmongUsClient.Instance.AmHost) {

                var playerControl = UnityEngine.Object.Instantiate(AmongUsClient.Instance.PlayerPrefab);
                var i = playerControl.PlayerId = (byte) GameData.Instance.GetAvailableId();

                bots.Add(playerControl);
                GameData.Instance.AddPlayer(playerControl);
                AmongUsClient.Instance.Spawn(playerControl, -2, InnerNet.SpawnFlags.None);
                
                playerControl.transform.position = PlayerControl.LocalPlayer.transform.position;
                playerControl.GetComponent<DummyBehaviour>().enabled = true;
                playerControl.NetTransform.enabled = false;
                playerControl.SetName("Dummy-" + random.Next(99));
                playerControl.SetColor((byte) random.Next(Palette.PlayerColors.Length));
                //playerControl.SetHat((uint) random.Next(HatManager.Instance.AllHats.Count), playerControl.Data);
                //playerControl.SetPet((uint) random.Next(HatManager.Instance.AllPets.Count));
                //playerControl.SetSkin((uint) random.Next(HatManager.Instance.AllSkins.Count));
                GameData.Instance.RpcSetTasks(playerControl.PlayerId, new byte[0]);
            }
            if(Input.GetKeyDown(KeyCode.L) && Input.GetKey(KeyCode.LeftShift) && AmongUsClient.Instance.AmHost) {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.EndGame, Hazel.SendOption.Reliable, -1);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                RPCProcedure.EndGame();
            }
            if(Input.GetKeyDown(KeyCode.X) && AmongUsClient.Instance.GameMode == GameModes.FreePlay) {
                PlayerControl.LocalPlayer.Data.Object.SetKillTimer(0f);
            }
            if(Input.GetKeyDown(KeyCode.K)) {
                var team = new Il2CppSystem.Collections.Generic.List<PlayerControl>();
                team.Add(PlayerControl.LocalPlayer);
                IntroCutscene.Instance.BeginImpostor(team);
            }
            if(Input.GetKeyDown(KeyCode.Y)) {
                main.SyncCustomSettingsRPC();
            }
        }
    }
}