using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Roles;
using VentLib.Logging;

namespace TownOfHost.Patches.Actions;

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