using System.Linq;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Gamemodes;
using TownOfHost.Managers;
using TownOfHost.Roles;

namespace TownOfHost.Patches.Actions;

public static class EnterVentPatches
{
    [HarmonyPatch(typeof(Vent), nameof(Vent.EnterVent))]
    class EnterVentPatch
    {
        public static void Postfix(Vent __instance, [HarmonyArgument(0)] PlayerControl pc)
        {
            if (Game.CurrentGamemode.IgnoredActions().HasFlag(GameAction.EnterVent) || !pc.GetCustomRole().CanVent())
                pc.MyPhysics.RpcBootFromVent(__instance.Id);
        }
    }
    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.CoEnterVent))]
    class CoEnterVentPatch
    {
        public static bool Prefix(PlayerPhysics __instance, [HarmonyArgument(0)] int id)
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            CustomRole role = __instance.myPlayer.GetCustomRole();
            if (!role.CanVent()) return false;
            ActionHandle handle = ActionHandle.NoInit();
            Vent? vent = ShipStatus.Instance.AllVents.FirstOrDefault(v => v.Id == id);
            if (vent != null)
                role.Trigger(RoleActionType.AnyEnterVent, ref handle, vent, __instance);
            return true;
        }
    }

}