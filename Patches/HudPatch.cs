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

namespace TownOfHost
{
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    class HudManagerPatch
    {
        public static void Postfix(HudManager __instance)
        {
            var TaskTextPrefix = "";
            //壁抜け
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                if (AmongUsClient.Instance.GameState != InnerNet.InnerNetClient.GameStates.Started ||
                AmongUsClient.Instance.GameMode == GameModes.FreePlay)
                {
                    PlayerControl.LocalPlayer.Collider.offset = new Vector2(0f, 127f);
                }
            }
            //壁抜け解除
            if(PlayerControl.LocalPlayer.Collider.offset.y == 127f) {
                if(!Input.GetKey(KeyCode.LeftControl) || AmongUsClient.Instance.IsGameStarted) {
                    PlayerControl.LocalPlayer.Collider.offset = new Vector2(0f,-0.3636f);
                }
            }
            //Madmate
            if (PlayerControl.LocalPlayer.Data.Role.Role == RoleTypes.Engineer && main.currentEngineer == EngineerRole.Madmate)
            {
                TaskTextPrefix = "<color=#ff0000>" + main.getLang(lang.Madmate) + "</color>\r\n" +
                "<color=#ff0000>" + main.getLang(lang.MadmateInfo) + "</color>\r\n";
            }
            //Jester
            if (PlayerControl.LocalPlayer.Data.Role.Role == RoleTypes.Scientist && main.currentScientist == ScientistRole.Jester)
            {
                TaskTextPrefix = "<color=#d161a4>" + main.getLang(lang.Jester) + "</color>\r\n" +
                "<color=#d161a4>" + main.getLang(lang.JesterInfo) + "</color>\r\n";
            }
            //Bait
            if (main.isBait(PlayerControl.LocalPlayer))
            {
                TaskTextPrefix = "<color=#00bfff>" + main.getLang(lang.Bait) + "</color>\r\n" +
                "<color=#00bfff>" + main.getLang(lang.BaitInfo) + "</color>\r\n";
            }
            //Terrorist
            if (main.isTerrorist(PlayerControl.LocalPlayer))
            {
                TaskTextPrefix = "<color=#00ff00>" + main.getLang(lang.Terrorist) + "</color>\r\n" +
                "<color=#00ff00>" + main.getLang(lang.TerroristInfo) + "</color>\r\n";
            }
            //Sidekick
            if (main.isSidekick(PlayerControl.LocalPlayer))
            {
                var ImpostorCount = 0;
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (pc.Data.Role.Role == RoleTypes.Impostor &&
                       !pc.Data.IsDead) ImpostorCount++;
                }
                if (ImpostorCount > 0)
                {
                    TaskTextPrefix = "<color=#ff0000>" + main.getLang(lang.Sidekick) + "</color>\r\n" +
                    "<color=#ff0000>" + "You can't kill now" + "</color>\r\n";
                }
                else
                {
                    TaskTextPrefix = "<color=#ff0000>" + main.getLang(lang.Sidekick) + "</color>\r\n" +
                    "<color=#ff0000>" + "Other Impostors are dead,\r\nso Kill everyone!" + "</color>\r\n";
                }
            }
            //Vampire
            if (main.isVampire(PlayerControl.LocalPlayer))
            {
                TaskTextPrefix = "<color=#a557a5>" + main.getLang(lang.Vampire) + "</color>\r\n" +
                "<color=#a557a5>" + main.getLang(lang.VampireInfo) + "</color>\r\n";
            }
            if (!__instance.TaskText.text.Contains(TaskTextPrefix))
            {
                __instance.TaskText.text = TaskTextPrefix + "\r\n" + __instance.TaskText.text;
            }
            if (main.OptionControllerIsEnable)
            {
                __instance.GameSettings.text = CustomOptionController.GetOptionText();
            }
            /*__instance.TaskText.text =
            "PC OwnerID:" + PlayerControl.LocalPlayer.OwnerId + "\r\n" +
            "CNT OwnerID:" + PlayerControl.LocalPlayer.NetTransform.OwnerId + "\r\n" +
            "PC NetID:" + PlayerControl.LocalPlayer.NetId + "\r\n" +
            "CNT NetID:" + PlayerControl.LocalPlayer.NetId + "\r\n" +
            "CNT name:" + PlayerControl.LocalPlayer.NetTransform.name + "\r\n";*/

            if(Input.GetKeyDown(KeyCode.RightShift)) {
                RepairSender.enabled = !RepairSender.enabled;
                RepairSender.Reset();
            }
            if(RepairSender.enabled/* && AmongUsClient.Instance.GameMode == GameModes.FreePlay*/) {
                if(Input.GetKeyDown(KeyCode.Alpha0)) RepairSender.Input(0);
                if(Input.GetKeyDown(KeyCode.Alpha1)) RepairSender.Input(1);
                if(Input.GetKeyDown(KeyCode.Alpha2)) RepairSender.Input(2);
                if(Input.GetKeyDown(KeyCode.Alpha3)) RepairSender.Input(3);
                if(Input.GetKeyDown(KeyCode.Alpha4)) RepairSender.Input(4);
                if(Input.GetKeyDown(KeyCode.Alpha5)) RepairSender.Input(5);
                if(Input.GetKeyDown(KeyCode.Alpha6)) RepairSender.Input(6);
                if(Input.GetKeyDown(KeyCode.Alpha7)) RepairSender.Input(7);
                if(Input.GetKeyDown(KeyCode.Alpha8)) RepairSender.Input(8);
                if(Input.GetKeyDown(KeyCode.Alpha9)) RepairSender.Input(9);
                if(Input.GetKeyDown(KeyCode.Return)) RepairSender.InputEnter();
                __instance.TaskText.text = RepairSender.getText();
            }
        }
    }
    class RepairSender {
        public static bool enabled = false;
        public static bool TypingAmount = false;

        public static int SystemType;
        public static int amount;

        public static void Input(int num) {
            if(!TypingAmount) {
                //SystemType入力中
                SystemType = SystemType * 10;
                SystemType += num;
            } else {
                //Amount入力中
                amount = amount * 10;
                amount += num;
            }
        }
        public static void InputEnter() {
            if(!TypingAmount) {
                //SystemType入力中
                TypingAmount = true;
            } else {
                //Amount入力中
                send();
            }
        }
        public static void send() {
            ShipStatus.Instance.RpcRepairSystem((SystemTypes)SystemType, amount);
            Reset();
        }
        public static void Reset() {
            TypingAmount = false;
            SystemType = 0;
            amount = 0;
        }
        public static string getText() {
            return SystemType.ToString() + "(" + ((SystemTypes)SystemType).ToString() + ")\r\n" + amount;
        }
    }
}
