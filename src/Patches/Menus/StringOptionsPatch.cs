using System;
using System.Linq;
using HarmonyLib;
using TownOfHost.Extensions;

namespace TownOfHost.Patches.Menus;

[HarmonyPatch(typeof(StringOption), nameof(StringOption.OnEnable))]
public class StringOptionEnablePatch
{
    public static bool Prefix(StringOption __instance)
    {
        var option = Main.OptionManager.AllHolders.FirstOrDefault(opt => opt.Behaviour == __instance);
        if (option == null) return true;
        __instance.OnValueChanged = new Action<OptionBehaviour>((o) => { });
        __instance.TitleText.text = option.ColorName;
        __instance.ValueText.text = option.GetAsString();


        return false;
    }
}

[HarmonyPatch(typeof(StringOption), nameof(StringOption.Increase))]
public class StringOptionIncreasePatch
{
    public static bool Prefix(StringOption __instance)
    {
        var option = Main.OptionManager.AllHolders.FirstOrDefault(opt => opt.Behaviour == __instance);
        if (option == null) return true;

        option?.Increment();
        __instance.ValueText.text = option?.GetAsString() ?? "N/A";


        return false;
    }
}

[HarmonyPatch(typeof(StringOption), nameof(StringOption.Decrease))]
public class StringOptionDecreasePatch
{
    public static bool Prefix(StringOption __instance)
    {
        var option = Main.OptionManager.AllHolders.FirstOrDefault(opt => opt.Behaviour == __instance);
        if (option == null) return true;

        option?.Decrement();
        __instance.ValueText.text = option?.GetAsString() ?? "N/A";

        return false;
    }
}