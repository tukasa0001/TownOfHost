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

namespace TownOfHost
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.StartGame))]
    class changeRoleSettings
    {
        public static void Postfix(AmongUsClient __instance)
        {
            main.currentWinner = CustomWinner.Default;
            main.CustomWinTrigger = false;
            main.OptionControllerIsEnable = false;
            main.BitPlayers = new Dictionary<byte, (byte, float)>();
            main.UsedButtonCount = 0;
            if (__instance.AmHost)
            {
                main.SyncCustomSettingsRPC();
                var opt = PlayerControl.GameOptions;
                if (main.currentScientist != ScientistRole.Default)
                {
                    opt.RoleOptions.ScientistBatteryCharge = 0f;
                    opt.RoleOptions.ScientistCooldown = 99f;
                }
                if (main.currentEngineer != EngineerRole.Default)
                {
                    opt.RoleOptions.EngineerCooldown = 0.2f;
                    opt.RoleOptions.EngineerInVentMaxTime = float.PositiveInfinity;
                }
                if (main.isFixedCooldown)
                {
                    main.BeforeFixCooldown = opt.KillCooldown;
                    opt.KillCooldown = main.BeforeFixCooldown * 2;
                }
                if (main.SyncButtonMode) main.BeforeFixMeetingCooldown = PlayerControl.GameOptions.EmergencyCooldown;
                PlayerControl.LocalPlayer.RpcSyncSettings(opt);
            }
        }
    }
}
