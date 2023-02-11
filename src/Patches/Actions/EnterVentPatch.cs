using System.Collections.Generic;
using HarmonyLib;
using TownOfHost.API;
using TownOfHost.Extensions;
using TownOfHost.Gamemodes;
using TownOfHost.Roles;
using TownOfHost.Roles.Internals;
using TownOfHost.Roles.Internals.Attributes;
using UnityEngine;
using VentLib.Logging;

namespace TownOfHost.Patches.Actions;

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

        if (!role.CanVent()) {
            pc.MyPhysics.RpcBootFromVent(__instance.Id);
            return;
        }

        vented = ActionHandle.NoInit();
        Game.TriggerForAll(RoleActionType.AnyEnterVent, ref vented, __instance, pc);
        if (vented.IsCanceled)
            pc.MyPhysics.RpcBootFromVent(__instance.Id);
        else lastVentLocation.Add(pc.PlayerId, new Vector2(__instance.Offset.x, __instance.Offset.y));
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
        EnterVentPatch.lastVentLocation.Remove(pc.PlayerId);
    }
}