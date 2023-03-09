using HarmonyLib;
using TOHTOR.Extensions;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using VentLib.Logging;

namespace TOHTOR.Patches.Actions;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CompleteTask))]
class TaskCompletePatch
{
    public static void Postfix(PlayerControl __instance)
    {
        VentLogger.Info($"TaskComplete:{__instance.GetNameWithRole()}", "CompleteTask");
        ActionHandle handle = ActionHandle.NoInit();
        __instance.GetCustomRole().Trigger(RoleActionType.TaskComplete, ref handle, __instance);
    }
}