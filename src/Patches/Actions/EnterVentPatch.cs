using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Gamemodes;
using TownOfHost.Managers;
using TownOfHost.Roles;
using VentLib.Logging;

namespace TownOfHost.Patches.Actions;

[HarmonyPatch(typeof(Vent), nameof(Vent.EnterVent))]
class EnterVentPatch
{
    public static void Postfix(Vent __instance, [HarmonyArgument(0)] PlayerControl pc)
    {
        VentLogger.Trace($"{pc.GetNameWithRole()} Entered Vent!!!! ({__instance.Id})", "CoEnterVent");
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

    }
}