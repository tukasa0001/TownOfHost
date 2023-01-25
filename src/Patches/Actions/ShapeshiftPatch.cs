using System.Diagnostics;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Roles;
using TownOfHost.Roles.Internals;
using TownOfHost.Roles.Internals.Attributes;
using TownOfHost.RPC;
using VentLib.Logging;
using VentLib.Utilities;

namespace TownOfHost.Patches.Actions;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Shapeshift))]
public class ShapeshiftPatch
{
    public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        string invokerName = new StackTrace(5)?.GetFrame(0)?.GetMethod()?.Name;
        VentLogger.Old($"Shapeshift Cause (Invoker): {invokerName}", "ShapeshiftEvent");
        if (invokerName is "RpcShapeshiftV2" or "RpcRevertShapeshiftV2" or "<Shapeshift>b__0" or "<RevertShapeshift>b__0") return true;
        VentLogger.Old($"{__instance?.GetNameWithRole()} => {target?.GetNameWithRole()}", "Shapeshift");
        if (!AmongUsClient.Instance.AmHost) return true;

        var shapeshifter = __instance;
        var shapeshifting = shapeshifter.PlayerId != target.PlayerId;



        ActionHandle handle = ActionHandle.NoInit();
        __instance.Trigger(shapeshifting ? RoleActionType.Shapeshift : RoleActionType.Unshapeshift, ref handle, target);
        if (!handle.IsCanceled) return true;
        Async.Schedule(() => __instance.CRpcRevertShapeshift(false), 0.3f);
        return false;
    }
}