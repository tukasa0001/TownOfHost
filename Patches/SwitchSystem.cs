using System;
using HarmonyLib;

namespace TownOfHost;

[HarmonyPatch(typeof(SwitchSystem), nameof(SwitchSystem.RepairDamage))]
class SwitchSystemRepairPatch
{
    public static void Postfix(SwitchSystem __instance, [HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] byte amount)
    {
        if (amount is >= 0 and <= 4 &&
            Options.DisableInterferenceFixLights.GetBool())
        {
            byte change = (byte)(1U << (int)amount);
            if ((__instance.ActualSwitches & change) != (__instance.ExpectedSwitches & change))
            {
                __instance.ActualSwitches ^= change;
                Logger.Info($"SwitchChange ActualSwitches: {Convert.ToString(__instance.ActualSwitches, toBase: 2)} => {Convert.ToString(__instance.ActualSwitches ^ change, toBase: 2)}", "SwitchSystem.RepairDamage");
            }
        }

    }
}