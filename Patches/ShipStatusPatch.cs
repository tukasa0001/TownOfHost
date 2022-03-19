using System;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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
            }
            else if (!float.IsNaN(main.RefixCooldownDelay))
            {
                Utils.CustomSyncAllSettings();
                main.RefixCooldownDelay = float.NaN;
                Logger.info("Refix Cooldown");
            }
            if (Options.IsHideAndSeek)
            {
                if (Options.HideAndSeekKillDelayTimer > 0)
                {
                    Options.HideAndSeekKillDelayTimer -= Time.fixedDeltaTime;
                    Logger.SendToFile("HaSKillDelayTimer: " + Options.HideAndSeekKillDelayTimer);
                }
                else if (!float.IsNaN(Options.HideAndSeekKillDelayTimer))
                {
                    Utils.CustomSyncAllSettings();
                    Options.HideAndSeekKillDelayTimer = float.NaN;
                    Logger.info("キル能力解禁");
                }
            }
            //BountyHunterのターゲットが無効な場合にリセット
            if (CustomRoles.BountyHunter.isEnable())
            {
                bool DoNotifyRoles = false;
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (!pc.isBountyHunter()) continue; //BountHutner以外おことわり
                    var target = pc.getBountyTarget();
                    //BountyHunterのターゲット更新
                    if (target.Data.IsDead || target.Data.Disconnected)
                    {
                        pc.ResetBountyTarget();
                        Logger.info($"{pc.name}のターゲットが無効だったため、ターゲットを更新しました");
                        DoNotifyRoles = true;
                    }
                }
                if (DoNotifyRoles) Utils.NotifyRoles();
            }
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.RepairSystem))]
    class RepairSystemPatch
    {
        public static bool Prefix(ShipStatus __instance,
            [HarmonyArgument(0)] SystemTypes systemType,
            [HarmonyArgument(1)] PlayerControl player,
            [HarmonyArgument(2)] byte amount)
        {
            Logger.msg("SystemType: " + systemType.ToString() + ", PlayerName: " + player.name + ", amount: " + amount);
            if (RepairSender.enabled && AmongUsClient.Instance.GameMode != GameModes.OnlineGame)
            {
                Logger.SendInGame("SystemType: " + systemType.ToString() + ", PlayerName: " + player.name + ", amount: " + amount);
            }
            if (!AmongUsClient.Instance.AmHost) return true;
            if (Options.IsHideAndSeek && systemType == SystemTypes.Sabotage) return false;

            //SabotageMaster
            if (player.isSabotageMaster())
            {
                switch (systemType)
                {
                    case SystemTypes.Reactor:
                        if (!Options.SabotageMasterFixesReactors) break;
                        if (Options.SabotageMasterSkillLimit > 0 && Options.SabotageMasterUsedSkillCount >= Options.SabotageMasterSkillLimit) break;
                        if (amount == 64 || amount == 65)
                        {
                            ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, 67);
                            ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, 66);
                            Options.SabotageMasterUsedSkillCount++;
                        }
                        if (amount == 16 || amount == 17)
                        {
                            ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, 19);
                            ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, 18);
                            Options.SabotageMasterUsedSkillCount++;
                        }
                        break;
                    case SystemTypes.Laboratory:
                        if (!Options.SabotageMasterFixesReactors) break;
                        if (Options.SabotageMasterSkillLimit > 0 && Options.SabotageMasterUsedSkillCount >= Options.SabotageMasterSkillLimit) break;
                        if (amount == 64 || amount == 65)
                        {
                            ShipStatus.Instance.RpcRepairSystem(SystemTypes.Laboratory, 67);
                            ShipStatus.Instance.RpcRepairSystem(SystemTypes.Laboratory, 66);
                            Options.SabotageMasterUsedSkillCount++;
                        }
                        break;
                    case SystemTypes.LifeSupp:
                        if (!Options.SabotageMasterFixesOxygens) break;
                        if (Options.SabotageMasterSkillLimit > 0 && Options.SabotageMasterUsedSkillCount >= Options.SabotageMasterSkillLimit) break;
                        if (amount == 64 || amount == 65)
                        {
                            ShipStatus.Instance.RpcRepairSystem(SystemTypes.LifeSupp, 67);
                            ShipStatus.Instance.RpcRepairSystem(SystemTypes.LifeSupp, 66);
                            Options.SabotageMasterUsedSkillCount++;
                        }
                        break;
                    case SystemTypes.Comms:
                        if (!Options.SabotageMasterFixesCommunications) break;
                        if (Options.SabotageMasterSkillLimit > 0 && Options.SabotageMasterUsedSkillCount >= Options.SabotageMasterSkillLimit) break;
                        if (amount == 16 || amount == 17)
                        {
                            ShipStatus.Instance.RpcRepairSystem(SystemTypes.Comms, 19);
                            ShipStatus.Instance.RpcRepairSystem(SystemTypes.Comms, 18);
                        }
                        Options.SabotageMasterUsedSkillCount++;
                        break;
                    case SystemTypes.Doors:
                        if (!Options.SabotageMasterFixesDoors) break;
                        if (DoorsProgressing == true) break;

                        int mapId = PlayerControl.GameOptions.MapId;
                        if (AmongUsClient.Instance.GameMode == GameModes.FreePlay) mapId = AmongUsClient.Instance.TutorialMapId;

                        DoorsProgressing = true;
                        if (mapId == 2)
                        {
                            //Polus
                            CheckAndOpenDoorsRange(__instance, amount, 71, 72);
                            CheckAndOpenDoorsRange(__instance, amount, 67, 68);
                            CheckAndOpenDoorsRange(__instance, amount, 64, 66);
                            CheckAndOpenDoorsRange(__instance, amount, 73, 74);
                        }
                        else if (mapId == 4)
                        {
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

            if (!Options.MadmateCanFixLightsOut && //Madmateが停電を直せる設定がオフ
               systemType == SystemTypes.Electrical && //システムタイプが電気室
               0 <= amount && amount <= 4 && //配電盤操作のamount
               (player.isMadmate() || player.isMadGuardian() || player.isMadSnitch() || player.isSKMadmate())) //実行者がMadmateかMadGuardianかMadSnitchかSKMadmate)
                return false;
            if (!Options.MadmateCanFixComms && //Madmateがコミュサボを直せる設定がオフ
                systemType == SystemTypes.Comms && //システムタイプが通信室
                (player.isMadmate() || player.isMadGuardian())) //実行者がMadmateかMadGuardian)
                return false;
            if (player.isSheriff())
            {
                if (systemType == SystemTypes.Sabotage && AmongUsClient.Instance.GameMode != GameModes.FreePlay) return false; //シェリフにサボタージュをさせない ただしフリープレイは例外
            }
            return true;
        }
        public static void Postfix(ShipStatus __instance)
        {
            Utils.CustomSyncAllSettings();
        }
        private static void CheckAndOpenDoorsRange(ShipStatus __instance, int amount, int min, int max)
        {
            var Ids = new List<int>();
            for (var i = min; i <= max; i++)
            {
                Ids.Add(i);
            }
            CheckAndOpenDoors(__instance, amount, Ids.ToArray());
        }
        private static void CheckAndOpenDoors(ShipStatus __instance, int amount, params int[] DoorIds)
        {
            if (DoorIds.Contains(amount)) foreach (var id in DoorIds)
                {
                    __instance.RpcRepairSystem(SystemTypes.Doors, id);
                }
        }
        private static bool DoorsProgressing = false;
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CloseDoorsOfType))]
    class CloseDoorsPatch
    {
        public static bool Prefix(ShipStatus __instance)
        {
            if (Options.IsHideAndSeek && !Options.AllowCloseDoors) return false;
            return true;
        }
    }
    [HarmonyPatch(typeof(SwitchSystem), nameof(SwitchSystem.RepairDamage))]
    class SwitchSystemRepairPatch
    {
        public static void Postfix(SwitchSystem __instance, [HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] byte amount)
        {
            if (player.isSabotageMaster())
            {
                if (!Options.SabotageMasterFixesElectrical) return;
                if (Options.SabotageMasterSkillLimit > 0 && Options.SabotageMasterUsedSkillCount >= Options.SabotageMasterSkillLimit) return;
                if (0 <= amount && amount <= 4)
                {
                    __instance.ActualSwitches = 0;
                    __instance.ExpectedSwitches = 0;
                    Options.SabotageMasterUsedSkillCount++;
                }
            }
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
    class StartPatch
    {
        public static void Postfix()
        {
            Logger.info("ShipStatus.Start");
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Begin))]
    class BeginPatch
    {
        public static void Postfix()
        {
            Logger.info("ShipStatus.Begin");
            Utils.NotifyRoles();
        }
    }
}
