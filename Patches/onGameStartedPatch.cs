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
        {//注:この時点では役職は設定されていません。
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

                if(main.SyncButtonMode) main.BeforeFixMeetingCooldown = PlayerControl.GameOptions.EmergencyCooldown;

                if(main.IsHideAndSeek) {
                    main.HideAndSeekKillDelayTimer = main.HideAndSeekKillDelay;
                    main.HideAndSeekImpVisionMin = opt.ImpostorLightMod;
                    opt.ImpostorLightMod = 0f;
                    Logger.SendToFile("HideAndSeekImpVisionMinを" + main.HideAndSeekImpVisionMin + "に変更");
                }

                PlayerControl.LocalPlayer.RpcSyncSettings(opt);
            }
        }
    }
    [HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
    class SelectRolesPatch {
        public static void Postfix(RoleManager __instance) {
            if(!AmongUsClient.Instance.AmHost) return;
            if(main.IsHideAndSeek) {
                var rand = new System.Random();
                SetColorPatch.IsAntiGlitchDisabled = true;
                //Hide And Seek時の処理
                foreach(var pc in PlayerControl.AllPlayerControls) {
                    if(pc.Data.Role.IsImpostor) pc.RpcSetColor(0);//赤色
                    else pc.RpcSetColor(1);//青色
                }
            }
            SetColorPatch.IsAntiGlitchDisabled = false;
        }
    }
}
