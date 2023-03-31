using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Crewmate;
using TOHE.Roles.Neutral;
using UnityEngine;

namespace TOHE;

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
            Utils.MarkEveryoneDirtySettings();
            Main.RefixCooldownDelay = float.NaN;
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

        if (Options.DisableSabotage.GetBool()) return false;

        // 蠢蛋无法修复破坏
        if (player.Is(CustomRoles.Fool) && systemType is SystemTypes.Sabotage or SystemTypes.Comms or SystemTypes.Electrical or SystemTypes.Reactor) return false;

        Logger.Msg("SystemType: " + systemType.ToString() + ", PlayerName: " + player.GetNameWithRole() + ", amount: " + amount, "RepairSystem");
        if (RepairSender.enabled && AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
        {
            Logger.SendInGame("SystemType: " + systemType.ToString() + ", PlayerName: " + player.GetNameWithRole() + ", amount: " + amount);
        }
        IsComms = false;
        foreach (PlayerTask task in PlayerControl.LocalPlayer.myTasks)
            if (task.TaskType == TaskTypes.FixComms) IsComms = true;

        if (!AmongUsClient.Instance.AmHost) return true; //以下、ホストのみ実行
        if ((Options.CurrentGameMode == CustomGameMode.SoloKombat) && systemType == SystemTypes.Sabotage) return false;
        //SabotageMaster
        if (player.Is(CustomRoles.SabotageMaster))
            SabotageMaster.RepairSystem(__instance, systemType, amount);

        if (systemType == SystemTypes.Electrical && 0 <= amount && amount <= 4)
        {
            switch (Main.NormalOptions.MapId)
            {
                case 4:
                    if (Options.DisableAirshipViewingDeckLightsPanel.GetBool() && Vector2.Distance(player.transform.position, new(-12.93f, -11.28f)) <= 2f) return false;
                    if (Options.DisableAirshipGapRoomLightsPanel.GetBool() && Vector2.Distance(player.transform.position, new(13.92f, 6.43f)) <= 2f) return false;
                    if (Options.DisableAirshipCargoLightsPanel.GetBool() && Vector2.Distance(player.transform.position, new(30.56f, 2.12f)) <= 2f) return false;
                    break;
            }
        }

        if (!player.Is(CustomRoleTypes.Impostor) && !(player.Is(CustomRoles.Jackal) && !player.Is(CustomRoles.Minimalism)) && Jackal.CanUseSabotage.GetBool())
        {
            if (systemType == SystemTypes.Sabotage && AmongUsClient.Instance.NetworkMode != NetworkModes.FreePlay) return false; //シェリフにサボタージュをさせない ただしフリープレイは例外
        }
        return true;
    }
    public static void Postfix(ShipStatus __instance)
    {
        Camouflage.CheckCamouflage();
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
        return !(Options.DisableSabotage.GetBool() || Options.CurrentGameMode == CustomGameMode.SoloKombat);
    }
}
[HarmonyPatch(typeof(SwitchSystem), nameof(SwitchSystem.RepairDamage))]
class SwitchSystemRepairPatch
{
    public static void Postfix(SwitchSystem __instance, [HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] byte amount)
    {
        if (player.Is(CustomRoles.SabotageMaster))
            SabotageMaster.SwitchSystemRepair(__instance, amount);
    }
}
[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
class StartPatch
{
    public static void Postfix()
    {
        Logger.CurrentMethod();
        Logger.Info("-----------游戏开始-----------", "Phase");

        Utils.CountAlivePlayers(true);

        if (Options.AllowConsole.GetBool() || PlayerControl.LocalPlayer.FriendCode.GetDevUser().IsDev)
        {
            if (!BepInEx.ConsoleManager.ConsoleActive && BepInEx.ConsoleManager.ConsoleEnabled)
                BepInEx.ConsoleManager.CreateConsole();
        }
        else
        {
            if (BepInEx.ConsoleManager.ConsoleActive && !DebugModeManager.AmDebugger)
            {
                BepInEx.ConsoleManager.DetachConsole();
                Logger.SendInGame("很抱歉，本房间禁止使用控制台，因此已将您的控制台关闭");
            }
        }
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
        if (Options.DisableTaskWin.GetBool() || Options.NoGameEnd.GetBool() || TaskState.InitialTotalTasks == 0 || Options.CurrentGameMode == CustomGameMode.SoloKombat)
        {
            __result = false;
            return false;
        }
        return true;
    }
}