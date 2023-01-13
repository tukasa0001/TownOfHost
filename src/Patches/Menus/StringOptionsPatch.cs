using System;
using System.Linq;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.RPC;

namespace TownOfHost.Patches.Menus;

[HarmonyPatch(typeof(StringOption), nameof(StringOption.OnEnable))]
public class StringOptionEnablePatch
{
    public static bool Prefix(StringOption __instance)
    {
        var option = TOHPlugin.OptionManager.AllHolders.FirstOrDefault(opt => opt.Behaviour == __instance);
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
        var option = TOHPlugin.OptionManager.AllHolders.FirstOrDefault(opt => opt.Behaviour == __instance);
        if (option == null) return true;

        option?.Increment();
        __instance.ValueText.text = option?.GetAsString() ?? "N/A";
        SendOptionsDelayed.SendOptions();

        return false;
    }
}

[HarmonyPatch(typeof(StringOption), nameof(StringOption.Decrease))]
public class StringOptionDecreasePatch
{
    public static bool Prefix(StringOption __instance)
    {
        var option = TOHPlugin.OptionManager.AllHolders.FirstOrDefault(opt => opt.Behaviour == __instance);
        if (option == null) return true;

        option?.Decrement();
        __instance.ValueText.text = option?.GetAsString() ?? "N/A";
        SendOptionsDelayed.SendOptions();

        return false;
    }
}

// It's really bad to constantly send options to clients whenever they get modified, this gives clients 3 seconds to get the new options
public static class SendOptionsDelayed
{
    private static bool _blocked;
    public static void SendOptions()
    {
        if (_blocked) return;
        _blocked = true;
        DTask.Schedule(_Send, 3f);
    }

    private static void _Send()
    {
        HostRpc.RpcSendOptions(TOHPlugin.OptionManager.Options());
        _blocked = false;
    }
}