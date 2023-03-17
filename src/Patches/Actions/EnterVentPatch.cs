using System.Collections.Generic;
using HarmonyLib;
using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.Gamemodes;
using TOHTOR.Roles;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using UnityEngine;
using VentLib.Logging;
using VentLib.Utilities;

namespace TOHTOR.Patches.Actions;

[HarmonyPatch(typeof(Vent), nameof(Vent.EnterVent))]
class EnterVentPatch
{
    internal static Dictionary<byte, Vector2?> lastVentLocation = new();

    public static void Postfix(Vent __instance, [HarmonyArgument(0)] PlayerControl pc)
    {
        VentLogger.Trace($"{pc.GetNameWithRole()} Entered Vent (ID: {__instance.Id})", "CoEnterVent");
        CustomRole role = pc.GetCustomRole();
        if (Game.CurrentGamemode.IgnoredActions().HasFlag(GameAction.EnterVent)) pc.MyPhysics.RpcBootFromVent(__instance.Id);
        ActionHandle vented = ActionHandle.NoInit();
        role.Trigger(RoleActionType.MyEnterVent, ref vented, __instance);

        if (!role.CanVent() || vented.IsCanceled) {
            Async.Schedule(() => pc.MyPhysics.RpcBootFromVent(__instance.Id), 0.4f);
            return;
        }

        vented = ActionHandle.NoInit();
        Game.TriggerForAll(RoleActionType.AnyEnterVent, ref vented, __instance, pc);
        if (vented.IsCanceled)
            Async.Schedule(() => pc.MyPhysics.RpcBootFromVent(__instance.Id), 0.4f);
        else lastVentLocation[pc.PlayerId] = new Vector2(__instance.Offset.x, __instance.Offset.y);
    }

    public static void CheckVentSwap(PlayerControl player)
    {
        Vector2? lastLocation = lastVentLocation.GetValueOrDefault(player.PlayerId);
        if (lastLocation == null) return;
        float distance = Vector2.Distance(lastLocation.Value, player.GetTruePosition());
        if (distance < 1) return;
        VentLogger.Fatal($"Player {player.GetNameWithRole()} Swapped Vents!");
        lastVentLocation[player.PlayerId] = player.GetTruePosition();

        // Run Code here
    }
}

[HarmonyPatch(typeof(Vent), nameof(Vent.ExitVent))]
class ExitVentPatch
{
    public static void Postfix(Vent __instance, [HarmonyArgument(0)] PlayerControl pc)
    {

        ActionHandle exitVent = ActionHandle.NoInit();
        pc.GetCustomRole().Trigger(RoleActionType.VentExit, ref exitVent, __instance);
        if (exitVent.IsCanceled) Async.Schedule(() => pc.MyPhysics.RpcEnterVent(__instance.Id), 0.0f);
        else EnterVentPatch.lastVentLocation.Remove(pc.PlayerId);
    }
}