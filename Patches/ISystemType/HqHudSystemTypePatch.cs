using HarmonyLib;
using Hazel;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Patches.ISystemType;

[HarmonyPatch(typeof(HqHudSystemType), nameof(HqHudSystemType.UpdateSystem))]
public static class HqHudSystemTypeUpdateSystemPatch
{
    public static bool Prefix(HqHudSystemType __instance, [HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] MessageReader msgReader)
    {
        byte amount;
        {
            var newReader = MessageReader.Get(msgReader);
            amount = newReader.ReadByte();
            newReader.Recycle();
        }

        if (player.GetRoleClass() is ISystemTypeUpdateHook systemTypeUpdateHook && !systemTypeUpdateHook.UpdateHqHudSystem(__instance, amount))
        {
            return false;
        }
        return true;
    }
    public static void Postfix()
    {
        Camouflage.CheckCamouflage();
        Utils.NotifyRoles();
    }
}
