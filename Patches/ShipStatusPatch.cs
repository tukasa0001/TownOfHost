using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

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
            if (Main.IsFixedCooldown && Main.RefixCooldownDelay >= 0)
            {
                Main.RefixCooldownDelay -= Time.fixedDeltaTime;
            }
            else if (!float.IsNaN(Main.RefixCooldownDelay))
            {
                Utils.CustomSyncAllSettings();
                Main.RefixCooldownDelay = float.NaN;
                Logger.Info("Refix Cooldown", "CoolDown");
            }
            if ((Options.CurrentGameMode == CustomGameMode.HideAndSeek || Options.IsStandardHAS) && Main.introDestroyed)
            {
                if (Options.HideAndSeekKillDelayTimer > 0)
                {
                    Options.HideAndSeekKillDelayTimer -= Time.fixedDeltaTime;
                }
                else if (!float.IsNaN(Options.HideAndSeekKillDelayTimer))
                {
                    Utils.CustomSyncAllSettings();
                    Options.HideAndSeekKillDelayTimer = float.NaN;
                    Logger.Info("キル能力解禁", "HideAndSeek");
                }
            }
            //BountyHunterのターゲットが無効な場合にリセット
            if (CustomRoles.BountyHunter.IsEnable())
            {
                bool DoNotifyRoles = false;
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    if (!pc.Is(CustomRoles.BountyHunter)) continue; //BountyHunter以外おことわり
                    var target = pc.GetBountyTarget();
                    //BountyHunterのターゲット更新
                    if (target.Data.IsDead || target.Data.Disconnected)
                    {
                        pc.ResetBountyTarget();
                        Logger.Info($"{pc.GetNameWithRole()}のターゲットが無効だったため、ターゲットを更新しました", "BountyHunter");
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
        public static bool IsComms;
        public static bool Prefix(ShipStatus __instance,
            [HarmonyArgument(0)] SystemTypes systemType,
            [HarmonyArgument(1)] PlayerControl player,
            [HarmonyArgument(2)] byte amount)
        {
            Logger.Msg("SystemType: " + systemType.ToString() + ", PlayerName: " + player.GetNameWithRole() + ", amount: " + amount, "RepairSystem");
            if (RepairSender.enabled && AmongUsClient.Instance.GameMode != GameModes.OnlineGame)
            {
                Logger.SendInGame("SystemType: " + systemType.ToString() + ", PlayerName: " + player.GetNameWithRole() + ", amount: " + amount);
            }
            IsComms = false;
            foreach (PlayerTask task in PlayerControl.LocalPlayer.myTasks)
                if (task.TaskType == TaskTypes.FixComms) IsComms = true;

            if (!AmongUsClient.Instance.AmHost) return true; //以下、ホストのみ実行
            if ((Options.CurrentGameMode == CustomGameMode.HideAndSeek || Options.IsStandardHAS) && systemType == SystemTypes.Sabotage) return false;
            //SabotageMaster
            if (player.Is(CustomRoles.SabotageMaster))
            {
                switch (systemType)
                {
                    case SystemTypes.Reactor:
                        if (!Options.SabotageMasterFixesReactors.GetBool()) break;
                        if (Options.SabotageMasterSkillLimit.GetFloat() > 0 && Options.SabotageMasterUsedSkillCount >= Options.SabotageMasterSkillLimit.GetFloat()) break;
                        if (amount is 64 or 65)
                        {
                            ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, 67);
                            ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, 66);
                            Options.SabotageMasterUsedSkillCount++;
                        }
                        if (amount is 16 or 17)
                        {
                            ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, 19);
                            ShipStatus.Instance.RpcRepairSystem(SystemTypes.Reactor, 18);
                            Options.SabotageMasterUsedSkillCount++;
                        }
                        break;
                    case SystemTypes.Laboratory:
                        if (!Options.SabotageMasterFixesReactors.GetBool()) break;
                        if (Options.SabotageMasterSkillLimit.GetFloat() > 0 && Options.SabotageMasterUsedSkillCount >= Options.SabotageMasterSkillLimit.GetFloat()) break;
                        if (amount is 64 or 65)
                        {
                            ShipStatus.Instance.RpcRepairSystem(SystemTypes.Laboratory, 67);
                            ShipStatus.Instance.RpcRepairSystem(SystemTypes.Laboratory, 66);
                            Options.SabotageMasterUsedSkillCount++;
                        }
                        break;
                    case SystemTypes.LifeSupp:
                        if (!Options.SabotageMasterFixesOxygens.GetBool()) break;
                        if (Options.SabotageMasterSkillLimit.GetFloat() > 0 && Options.SabotageMasterUsedSkillCount >= Options.SabotageMasterSkillLimit.GetFloat()) break;
                        if (amount is 64 or 65)
                        {
                            ShipStatus.Instance.RpcRepairSystem(SystemTypes.LifeSupp, 67);
                            ShipStatus.Instance.RpcRepairSystem(SystemTypes.LifeSupp, 66);
                            Options.SabotageMasterUsedSkillCount++;
                        }
                        break;
                    case SystemTypes.Comms:
                        if (!Options.SabotageMasterFixesComms.GetBool()) break;
                        if (Options.SabotageMasterSkillLimit.GetFloat() > 0 && Options.SabotageMasterUsedSkillCount >= Options.SabotageMasterSkillLimit.GetFloat()) break;
                        if (amount is 16 or 17)
                        {
                            ShipStatus.Instance.RpcRepairSystem(SystemTypes.Comms, 19);
                            ShipStatus.Instance.RpcRepairSystem(SystemTypes.Comms, 18);
                        }
                        Options.SabotageMasterUsedSkillCount++;
                        break;
                    case SystemTypes.Doors:
                        if (!Options.SabotageMasterFixesDoors.GetBool()) break;
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

            if (!Options.MadmateCanFixLightsOut.GetBool() && //Madmateが停電を直せる設定がオフ
               systemType == SystemTypes.Electrical && //システムタイプが電気室
               0 <= amount && amount <= 4 && //配電盤操作のamount
               (player.Is(CustomRoles.Madmate) || player.Is(CustomRoles.MadGuardian) || player.Is(CustomRoles.MadSnitch) || player.Is(CustomRoles.SKMadmate))) //実行者がMadmateかMadGuardianかMadSnitchかSKMadmate)
                return false;
            if (!Options.MadmateCanFixComms.GetBool() && //Madmateがコミュサボを直せる設定がオフ
                systemType == SystemTypes.Comms && //システムタイプが通信室
                (player.Is(CustomRoles.Madmate) || player.Is(CustomRoles.MadGuardian))) //実行者がMadmateかMadGuardian)
                return false;
            if (player.Is(CustomRoles.Sheriff) || player.Is(CustomRoles.Arsonist))
            {
                if (systemType == SystemTypes.Sabotage && AmongUsClient.Instance.GameMode != GameModes.FreePlay) return false; //シェリフにサボタージュをさせない ただしフリープレイは例外
            }
            return true;
        }
        public static void Postfix(ShipStatus __instance)
        {
            Utils.CustomSyncAllSettings();
            new LateTask(
                () =>
                {
                    if (!GameStates.IsMeeting)
                        Utils.NotifyRoles(ForceLoop: true);
                }, 0.1f, "RepairSystem NotifyRoles");
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
            return Options.CurrentGameMode != CustomGameMode.HideAndSeek || Options.AllowCloseDoors.GetBool();
        }
    }
    [HarmonyPatch(typeof(SwitchSystem), nameof(SwitchSystem.RepairDamage))]
    class SwitchSystemRepairPatch
    {
        public static void Postfix(SwitchSystem __instance, [HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] byte amount)
        {
            if (player.Is(CustomRoles.SabotageMaster))
            {
                if (!Options.SabotageMasterFixesElectrical.GetBool()) return;
                if (Options.SabotageMasterSkillLimit.GetFloat() > 0 &&
                    Options.SabotageMasterUsedSkillCount >= Options.SabotageMasterSkillLimit.GetFloat())
                {
                    return;
                }

                if (amount is >= 0 and <= 4)
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
            Logger.CurrentMethod();
            Logger.Info("-----------ゲーム開始-----------", "Phase");

            Utils.CountAliveImpostors();
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Begin))]
    class BeginPatch
    {
        public static void Postfix()
        {
            Logger.CurrentMethod();

            //ホストの役職初期設定はここで行うべき？
        }
    }
}