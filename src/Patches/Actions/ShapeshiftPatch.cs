using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Roles;
using TownOfHost.RPC;
using UnityEngine;

namespace TownOfHost.Patches.Actions;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Shapeshift))]
public class ShapeshiftPatch
{
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        string invokerName = new StackTrace(5)?.GetFrame(0)?.GetMethod()?.Name;
        Logger.Info($"Shapeshift Cause (Invoker): {invokerName}", "ShapeshiftEvent");
        if (invokerName is "RpcShapeshiftV2" or "RpcRevertShapeshiftV2") return true;
        Logger.Info($"{__instance?.GetNameWithRole()} => {target?.GetNameWithRole()}", "Shapeshift");
        if (!AmongUsClient.Instance.AmHost) return true;

        var shapeshifter = __instance;
        var shapeshifting = shapeshifter.PlayerId != target.PlayerId;



        ActionHandle handle = ActionHandle.NoInit();
        __instance.Trigger(shapeshifting ? RoleActionType.Shapeshift : RoleActionType.Unshapeshift, ref handle, target);
        if (!handle.IsCanceled) return true;
        DTask.Schedule(() => __instance.CRpcRevertShapeshift(false), 0.3f);
        return false;
    }
}