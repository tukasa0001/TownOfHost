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

                main.VisibleTasksCount = true;
                if(main.IsHideAndSeek) {
                    main.currentEngineer = EngineerRoles.Default;
                    main.currentScientist = ScientistRoles.Default;
                    main.currentImpostor = ImpostorRoles.Default;
                    main.currentShapeshifter = ShapeshifterRoles.Default;
                }
                main.SyncCustomSettingsRPC();
                var opt = PlayerControl.GameOptions;
                if (main.currentScientist != ScientistRoles.Default)
                {//バイタル無効
                    opt.RoleOptions.ScientistBatteryCharge = 0f;
                    opt.RoleOptions.ScientistCooldown = 99f;
                }
                if (main.currentEngineer != EngineerRoles.Default)
                {//無限ベント
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
            main.ApplySuffix();
            
            if(main.IsHideAndSeek) {
                var rand = new System.Random();
                SetColorPatch.IsAntiGlitchDisabled = true;
                main.HideAndSeekRoleList = new Dictionary<byte, HideAndSeekRoles>();
                //Hide And Seek時の処理
                List<PlayerControl> Impostors = new List<PlayerControl>();
                List<PlayerControl> Crewmates = new List<PlayerControl>();
                //リスト作成兼色設定処理
                foreach(var pc in PlayerControl.AllPlayerControls) {
                    main.HideAndSeekRoleList.Add(pc.PlayerId,HideAndSeekRoles.Default);
                    if(pc.Data.Role.IsImpostor) {
                        Impostors.Add(pc);
                        pc.RpcSetColor(0);
                    } else {
                        Crewmates.Add(pc);
                        pc.RpcSetColor(1);
                    }
                    if(main.IgnoreCosmetics) {
                        pc.RpcSetHat("");
                        pc.RpcSetSkin("");
                    }
                }
                //FoxCountとTrollCountを適切に修正する
                int FixedFoxCount = Math.Clamp(main.FoxCount,0,Crewmates.Count);
                int FixedTrollCount = Math.Clamp(main.TrollCount,0,Crewmates.Count - FixedFoxCount);
                List<PlayerControl> FoxList = new List<PlayerControl>();
                List<PlayerControl> TrollList = new List<PlayerControl>();
                //役職設定処理
                for(var i = 0; i < FixedFoxCount; i++) {
                    var id = rand.Next(Crewmates.Count);
                    FoxList.Add(Crewmates[id]);
                    main.HideAndSeekRoleList[Crewmates[id].PlayerId] = HideAndSeekRoles.Fox;
                    Crewmates[id].RpcSetColor(3);
                    Crewmates[id].RpcSetHideAndSeekRole(HideAndSeekRoles.Fox);
                    Crewmates.RemoveAt(id);
                }
                for(var i = 0; i < FixedTrollCount; i++) {
                    var id = rand.Next(Crewmates.Count);
                    TrollList.Add(Crewmates[id]);
                    main.HideAndSeekRoleList[Crewmates[id].PlayerId] = HideAndSeekRoles.Troll;
                    Crewmates[id].RpcSetColor(2);
                    Crewmates[id].RpcSetHideAndSeekRole(HideAndSeekRoles.Troll);
                    Crewmates.RemoveAt(id);
                }
                //通常クルー・インポスター用RPC
                foreach(var pc in Crewmates) pc.RpcSetHideAndSeekRole(HideAndSeekRoles.Default);
                foreach(var pc in Impostors) pc.RpcSetHideAndSeekRole(HideAndSeekRoles.Default);
            }
            SetColorPatch.IsAntiGlitchDisabled = false;
        }
    }
}
