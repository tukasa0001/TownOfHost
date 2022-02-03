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
using System.Linq;
using System.Threading.Tasks;

namespace TownOfHost
{
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.FixedUpdate))]
    class ShipFixedUpdatePatch
    {
        public static void Postfix(ShipStatus __instance)
        {
            //ここより上、全員が実行する
            if (!AmongUsClient.Instance.AmHost) return;
            //ここより下、ホストのみが実行する
            if (main.isFixedCooldown && PlayerControl.GameOptions.KillCooldown == main.BeforeFixCooldown)
            {
                if (main.RefixCooldownDelay <= 0)
                {
                    PlayerControl.GameOptions.KillCooldown = main.BeforeFixCooldown * 2;
                    PlayerControl.LocalPlayer.RpcSyncSettings(PlayerControl.GameOptions); ;
                }
                else
                {
                    main.RefixCooldownDelay -= Time.fixedDeltaTime;
                }
            }
            if(main.IsHideAndSeek) {
                if(main.HideAndSeekKillDelayTimer > 0) {
                    main.HideAndSeekKillDelayTimer -= Time.fixedDeltaTime;
                    Logger.SendToFile("HaSKillDelayTimer: " + main.HideAndSeekKillDelayTimer);
                    //インポスター行動解禁までの処理
                    foreach(var pc in PlayerControl.AllPlayerControls) {
                        if(pc.Data.Role.IsImpostor) {
                        }
                    }
                } else if(!float.IsNaN(main.HideAndSeekKillDelayTimer)) {
                    Logger.info("キル能力解禁");
                    main.HideAndSeekKillDelayTimer = float.NaN;
                    PlayerControl.GameOptions.ImpostorLightMod = main.HideAndSeekImpVisionMin;
                    PlayerControl.LocalPlayer.RpcSyncSettings(PlayerControl.GameOptions);
                }
            }
            main.NotifyRoles();
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.RepairSystem))]
    class RepairSystemPatch {
        public static bool Prefix(ShipStatus __instance,
            [HarmonyArgument(0)] SystemTypes systemType,
            [HarmonyArgument(1)] PlayerControl player,
            [HarmonyArgument(2)] byte amount) {
            Logger.msg("SystemType: " + systemType.ToString() + ", PlayerName: " + player.name + ", amount: " + amount);
            if(RepairSender.enabled && AmongUsClient.Instance.GameMode != GameModes.OnlineGame)
            Logger.SendInGame("SystemType: " + systemType.ToString() + ", PlayerName: " + player.name + ", amount: " + amount);

            if(!AmongUsClient.Instance.AmHost) return true;
            if(main.IsHideAndSeek && systemType == SystemTypes.Sabotage) return false;

            //SabotageMaster
            if(player.isSabotageMaster()) {
                switch(systemType){
                    case SystemTypes.Reactor:
                        if(!main.SabotageMasterFixesReactors) break;
                        if(main.SabotageMasterSkillLimit > 0 && main.SabotageMasterUsedSkillCount >= main.SabotageMasterSkillLimit) break;
                        if(amount == 64) ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, 67);
                        if(amount == 65) ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, 66);
                        if(amount == 16 || amount == 17) {
                            ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, 19);
                            ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, 18);
                        }
                        main.SabotageMasterUsedSkillCount++;
                        break;
                    case SystemTypes.Laboratory:
                        if(!main.SabotageMasterFixesReactors) break;
                        if(main.SabotageMasterSkillLimit > 0 && main.SabotageMasterUsedSkillCount >= main.SabotageMasterSkillLimit) break;
                        if(amount == 64) ShipStatus.Instance.RpcRepairSystem(SystemTypes.Laboratory, 67);
                        if(amount == 65) ShipStatus.Instance.RpcRepairSystem(SystemTypes.Laboratory, 66);
                        main.SabotageMasterUsedSkillCount++;
                        break;
                    case SystemTypes.LifeSupp:
                        if(!main.SabotageMasterFixesOxygens) break;
                        if(main.SabotageMasterSkillLimit > 0 && main.SabotageMasterUsedSkillCount >= main.SabotageMasterSkillLimit) break;
                        if(amount == 64) ShipStatus.Instance.RpcRepairSystem(SystemTypes.LifeSupp, 67);
                        if(amount == 65) ShipStatus.Instance.RpcRepairSystem(SystemTypes.LifeSupp, 66);
                        main.SabotageMasterUsedSkillCount++;
                        break;
                    case SystemTypes.Comms:
                        if(!main.SabotageMasterFixesCommunications) break;
                        if(main.SabotageMasterSkillLimit > 0 && main.SabotageMasterUsedSkillCount >= main.SabotageMasterSkillLimit) break;
                        if(amount == 16 || amount == 17) {
                            ShipStatus.Instance.RpcRepairSystem(SystemTypes.Comms, 19);
                            ShipStatus.Instance.RpcRepairSystem(SystemTypes.Comms, 18);
                        }
                        main.SabotageMasterUsedSkillCount++;
                        break;
                    case SystemTypes.Doors:
                        if(!main.SabotageMasterFixesDoors) break;
                        if(DoorsProgressing == true) break;

                        int mapId = PlayerControl.GameOptions.MapId;
                        if(AmongUsClient.Instance.GameMode == GameModes.FreePlay) mapId = AmongUsClient.Instance.TutorialMapId;

                        DoorsProgressing = true;
                        if(mapId == 2) {
                            //Polus
                            CheckAndOpenDoorsRange(__instance, amount, 71, 72);
                            CheckAndOpenDoorsRange(__instance, amount, 67, 68);
                            CheckAndOpenDoorsRange(__instance, amount, 64, 66);
                            CheckAndOpenDoorsRange(__instance, amount, 73, 74);
                        }
                        else if(mapId == 4) {
                            //Airship
                            CheckAndOpenDoorsRange(__instance, amount, 64, 67);
                            CheckAndOpenDoorsRange(__instance, amount, 71, 73);
                            CheckAndOpenDoorsRange(__instance, amount, 74, 75);
                            CheckAndOpenDoorsRange(__instance, amount, 76, 78);
                            CheckAndOpenDoorsRange(__instance, amount, 68, 70);
                            CheckAndOpenDoorsRange(__instance, amount, 83, 84);
                        }
                        DoorsProgressing = false;
                        break;
                }
            }

            if(!main.MadmateCanFixLightsOut && //Madmateが停電を直せる設定がオフ
               systemType == SystemTypes.Electrical && //システムタイプが電気室
               0 <= amount && amount <= 4 && //配電盤操作のamount
               (player.isMadmate() || player.isMadGuardian())) //実行者がMadmateかMadGuardian)
                return false;
            return true;
        }
        private static void CheckAndOpenDoorsRange(ShipStatus __instance, int amount, int min, int max) {
            var Ids = new List<int>();
            for(var i = min; i <= max; i++) {
                Ids.Add(i);
            }
            CheckAndOpenDoors(__instance, amount, Ids.ToArray());
        }
        private static void CheckAndOpenDoors(ShipStatus __instance, int amount, params int[] DoorIds) {
            if(DoorIds.Contains(amount)) foreach(var id in DoorIds) {
                __instance.RpcRepairSystem(SystemTypes.Doors, id);
            }
        }
        private static bool DoorsProgressing = false;
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CloseDoorsOfType))]
    class CloseDoorsPatch {
        public static bool Prefix(ShipStatus __instance) {
            if(main.IsHideAndSeek && !main.AllowCloseDoors) return false;
            return true;
        }
    }
    [HarmonyPatch(typeof(SwitchSystem), nameof(SwitchSystem.RepairDamage))]
    class SwitchSystemRepairPatch {
        public static void Postfix(SwitchSystem __instance, [HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] byte amount) {
            if(player.isSabotageMaster()) {
                if(!main.SabotageMasterFixesElectrical) return;
                if(main.SabotageMasterSkillLimit > 0 && main.SabotageMasterUsedSkillCount >= main.SabotageMasterSkillLimit) return;
                if(0 <= amount && amount <= 4) {
                    __instance.ActualSwitches = 0;
                    __instance.ExpectedSwitches = 0;
                    main.SabotageMasterUsedSkillCount++;
                }
            }
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
    class StartPatch {
        public static void Postfix() {
            Logger.info("ShipStatus.Start");
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Begin))]
    class BeginPatch {
        public static void Postfix() {
            Logger.info("ShipStatus.Begin");
            main.NotifyRoles();
        }
    }
}
