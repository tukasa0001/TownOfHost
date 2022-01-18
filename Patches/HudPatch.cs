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
            if (PlayerControl.LocalPlayer.Collider.offset.y == 127f)
            {
                if (!Input.GetKey(KeyCode.LeftControl))
                {
                    PlayerControl.LocalPlayer.Collider.offset = new Vector2(0f, -0.3636f);
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
        }
    }
}
