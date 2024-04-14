using HarmonyLib;
using Hazel;
using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;

namespace TownOfHostForE.Patches.ISystemType;

[HarmonyPatch(typeof(LifeSuppSystemType), nameof(LifeSuppSystemType.UpdateSystem))]
public static class LifeSuppSystemUpdateSystemPatch
{
    public static bool Prefix(LifeSuppSystemType __instance, [HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] MessageReader msgReader)
    {
        byte amount;
        {
            var newReader = MessageReader.Get(msgReader);
            amount = newReader.ReadByte();
            newReader.Recycle();
        }

        if (player.GetRoleClass() is ISystemTypeUpdateHook systemTypeUpdateHook && !systemTypeUpdateHook.UpdateLifeSuppSystem(__instance, amount))
        {
            return false;
        }
        return true;
    }
}
