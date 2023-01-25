using System;
using HarmonyLib;

namespace TownOfHost.GUI.Patches;

[HarmonyPatch(typeof(KillOverlay), nameof(KillOverlay.ShowKillAnimation))]
public class KillOverlayPatch
{
    private static DateTime _lastOverlay = DateTime.Now;

    public static bool Prefix(KillOverlay __instance)
    {
        bool show = ((DateTime.Now - _lastOverlay).TotalSeconds > 0.5f);
        _lastOverlay = DateTime.Now;
        return show;
    }
}