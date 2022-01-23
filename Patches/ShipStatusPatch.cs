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
            
            //SabotargeMaster
            if(main.isSabotargeMaster(player)) {
                switch(systemType){
                    case SystemTypes.Reactor:
                        if(amount == 64) ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, 67);
                        if(amount == 65) ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, 66);
                        if(amount == 16 || amount == 17) {
                            ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, 19);
                            ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, 18);
                        }
                        break;
                    case SystemTypes.Laboratory:
                        if(amount == 64) ShipStatus.Instance.RpcRepairSystem(SystemTypes.Laboratory, 67);
                        if(amount == 65) ShipStatus.Instance.RpcRepairSystem(SystemTypes.Laboratory, 66);
                        break;
                    case SystemTypes.LifeSupp:
                        if(amount == 64) ShipStatus.Instance.RpcRepairSystem(SystemTypes.LifeSupp, 67);
                        if(amount == 65) ShipStatus.Instance.RpcRepairSystem(SystemTypes.LifeSupp, 66);
                        break;
                    case SystemTypes.Comms:
                        if(amount == 16 || amount == 17) {
                            ShipStatus.Instance.RpcRepairSystem(SystemTypes.Comms, 19);
                            ShipStatus.Instance.RpcRepairSystem(SystemTypes.Comms, 18);
                        }
                        break;
                }
            }

            return true;
        }
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
            if(main.isSabotargeMaster(player)) {
                if(0 <= amount && amount <= 4) {
                    __instance.ActualSwitches = 0;
                    __instance.ExpectedSwitches = 0;
                }
            }
        }
    }
}
