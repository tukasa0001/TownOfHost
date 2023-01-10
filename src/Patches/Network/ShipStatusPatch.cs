using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Managers;
using UnityEngine;
using TownOfHost.Roles;
using TownOfHost.ReduxOptions;

namespace TownOfHost
{
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.FixedUpdate))]
    class ShipFixedUpdatePatch
    {
        public static void Postfix(ShipStatus __instance)
        {
            //ここより上、全員が実行する
            if (!AmongUsClient.Instance.AmHost) return;

            Game.CurrentGamemode.FixedUpdate();

            //ここより下、ホストのみが実行する
            if (TOHPlugin.IsFixedCooldown && TOHPlugin.RefixCooldownDelay >= 0)
                TOHPlugin.RefixCooldownDelay -= Time.fixedDeltaTime;

            else if (!float.IsNaN(TOHPlugin.RefixCooldownDelay))
            {
                TOHPlugin.RefixCooldownDelay = float.NaN;
                Logger.Info("Refix Cooldown", "CoolDown");
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
            if (RepairSender.enabled && AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
            {
                Logger.SendInGame("SystemType: " + systemType.ToString() + ", PlayerName: " + player.GetNameWithRole() + ", amount: " + amount);
            }
            IsComms = false;
            foreach (PlayerTask task in PlayerControl.LocalPlayer.myTasks)
                if (task.TaskType == TaskTypes.FixComms) IsComms = true;

            if (!AmongUsClient.Instance.AmHost) return true; //以下、ホストのみ実行
            //SabotageMaster
            /*if (player.Is(CustomRoles.SabotageMaster))
                SabotageMasterOLD.RepairSystem(__instance, systemType, amount);*/

            if (systemType == SystemTypes.Electrical && 0 <= amount && amount <= 4)
            {
                if (!StaticOptions.MadmateCanFixLightsOut && player.GetCustomRole().IsMadmate()) return false; //Madmateが停電を直せる設定がオフ
                switch (TOHPlugin.NormalOptions.MapId)
                {
                    case 4:
                        if (StaticOptions.DisableAirshipViewingDeckLightsPanel && Vector2.Distance(player.transform.position, new(-12.93f, -11.28f)) <= 2f) return false;
                        if (StaticOptions.DisableAirshipGapRoomLightsPanel && Vector2.Distance(player.transform.position, new(13.92f, 6.43f)) <= 2f) return false;
                        if (StaticOptions.DisableAirshipCargoLightsPanel && Vector2.Distance(player.transform.position, new(30.56f, 2.12f)) <= 2f) return false;
                        break;
                }
            }

            if (!StaticOptions.MadmateCanFixComms && player.GetCustomRole().IsMadmate() //Madmateがコミュサボを直せる設定がオフ
                && systemType == SystemTypes.Comms //システムタイプが通信室
                && amount is 0 or 16 or 17)
                return false;
            return true;
        }
        public static void CheckAndOpenDoorsRange(ShipStatus __instance, int amount, int min, int max)
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
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CloseDoorsOfType))]
    class CloseDoorsPatch
    {
        public static bool Prefix(ShipStatus __instance)
        {
            return !(OldOptions.CurrentGameMode == CustomGameMode.HideAndSeek || StaticOptions.IsStandardHAS) || StaticOptions.AllowCloseDoors;
        }
    }
    [HarmonyPatch(typeof(SwitchSystem), nameof(SwitchSystem.RepairDamage))]
    class SwitchSystemRepairPatch
    {
        public static void Postfix(SwitchSystem __instance, [HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] byte amount)
        {
            /*if (player.Is(CustomRoles.SabotageMaster))
                SabotageMasterOLD.SwitchSystemRepair(__instance, amount);*/
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
    class StartPatch
    {
        public static void Postfix()
        {
            Logger.CurrentMethod();
            Logger.Info("-----------ゲーム開始-----------", "Phase");
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.StartMeeting))]
    class StartMeetingPatch
    {
        public static void Prefix(ShipStatus __instance, PlayerControl reporter, GameData.PlayerInfo target)
        {
            MeetingStates.ReportTarget = target;
            MeetingStates.DeadBodies = UnityEngine.Object.FindObjectsOfType<DeadBody>();
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
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.CheckTaskCompletion))]
    class CheckTaskCompletionPatch
    {
        public static bool Prefix(ref bool __result)
        {
            if (StaticOptions.DisableTaskWin)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}