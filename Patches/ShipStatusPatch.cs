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
            if (main.isFixedCooldown && main.RefixCooldownDelay >= 0)
            {
                main.RefixCooldownDelay -= Time.fixedDeltaTime;
            } else if(!float.IsNaN(main.RefixCooldownDelay)) {
                main.CustomSyncAllSettings();
                main.RefixCooldownDelay = float.NaN;
                Logger.info("Refix Cooldown");
            }
            if(main.IsHideAndSeek) {
                if(main.HideAndSeekKillDelayTimer > 0) {
                    main.HideAndSeekKillDelayTimer -= Time.fixedDeltaTime;
                    Logger.SendToFile("HaSKillDelayTimer: " + main.HideAndSeekKillDelayTimer);
                } else if(!float.IsNaN(main.HideAndSeekKillDelayTimer)) {
                    main.CustomSyncAllSettings();
                    main.HideAndSeekKillDelayTimer = float.NaN;
                    Logger.info("キル能力解禁");
                }
            }
            //BountyHunterのターゲットが無効な場合にリセット
            if(main.BountyHunterCount > 0) {
                bool DoNotifyRoles = false;
                foreach(var pc in PlayerControl.AllPlayerControls) {
                    if(!pc.isBountyHunter()) continue; //BountHutner以外おことわり
                    var target = pc.getBountyTarget();
                    //BountyHunterのターゲット更新
                    if(target.Data.IsDead || target.Data.Disconnected) {
                        pc.ResetBountyTarget();
                        Logger.info($"{pc.name}のターゲットが無効だったため、ターゲットを更新しました");
                        DoNotifyRoles = true;
                    }
                }
                if(DoNotifyRoles) main.NotifyRoles();
            }
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
                        if(amount == 64 || amount == 65)
                        {
                            ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, 67);
                            ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, 66);
                            main.SabotageMasterUsedSkillCount++;
                        }
                        if(amount == 16 || amount == 17) {
                            ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, 19);
                            ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, 18);
                            main.SabotageMasterUsedSkillCount++;
                        }
                        break;
                    case SystemTypes.Laboratory:
                        if(!main.SabotageMasterFixesReactors) break;
                        if(main.SabotageMasterSkillLimit > 0 && main.SabotageMasterUsedSkillCount >= main.SabotageMasterSkillLimit) break;
                        if(amount == 64 || amount == 65)
                        {
                            ShipStatus.Instance.RpcRepairSystem(SystemTypes.Laboratory, 67);
                            ShipStatus.Instance.RpcRepairSystem(SystemTypes.Laboratory, 66);
                            main.SabotageMasterUsedSkillCount++;
                        }
                        break;
                    case SystemTypes.LifeSupp:
                        if(!main.SabotageMasterFixesOxygens) break;
                        if(main.SabotageMasterSkillLimit > 0 && main.SabotageMasterUsedSkillCount >= main.SabotageMasterSkillLimit) break;
                        if(amount == 64 || amount == 65)
                        {
                            ShipStatus.Instance.RpcRepairSystem(SystemTypes.LifeSupp, 67);
                            ShipStatus.Instance.RpcRepairSystem(SystemTypes.LifeSupp, 66);
                            main.SabotageMasterUsedSkillCount++;
                        }
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
            if (!main.MadmateCanFixComms && //Madmateがコミュサボを直せる設定がオフ
                systemType == SystemTypes.Comms && //システムタイプが通信室
                (player.isMadmate() || player.isMadGuardian())) //実行者がMadmateかMadGuardian)
                return false;
            if(player.isSheriff()) {
                if(player.Data.IsDead) return false; //死んだSheriffには何もさせない
                if(systemType == SystemTypes.Sabotage && AmongUsClient.Instance.GameMode != GameModes.FreePlay) return false; //シェリフにサボタージュをさせない ただしフリープレイは例外
            }
            return true;
        }
        public static void Postfix(ShipStatus __instance) {
            main.CustomSyncAllSettings();
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
