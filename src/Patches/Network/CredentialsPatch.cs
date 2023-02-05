using HarmonyLib;
using TMPro;
using TownOfHost.Addons;
using TownOfHost.Managers.Date;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Utilities;

namespace TownOfHost.Patches.Network;

[Localized("PingDisplay")]
[HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
public class VersionShowerStartPatch
{
    [Localized("AddonsLoaded")]
    private static string addonsLoaded;

    private static bool _init = false;

    public static TextMeshPro? SpecialEventText;
    static void Postfix(VersionShower __instance)
    {
        TOHPlugin.CredentialsText = $"\r\n<color={TOHPlugin.ModColor}>{TOHPlugin.ModName}</color> v{TOHPlugin.PluginVersion}" + (TOHPlugin.DevVersion ? " " + TOHPlugin.DevVersionStr : "");
#if DEBUG
        TOHPlugin.CredentialsText += $"\r\n<color={TOHPlugin.ModColor}>{TOHPlugin.Instance.Version().Branch}({TOHPlugin.Instance.Version().CommitNumber})</color>";
#endif

        int addonCount = AddonManager.Addons.Count;
        if (addonCount > 0)
            TOHPlugin.CredentialsText += $"\r\n{new Color(1f, 0.67f, 0.37f).Colorize($"{addonCount} {addonsLoaded}")}";


        var credentials = Object.Instantiate(__instance.text);
        credentials.text = TOHPlugin.CredentialsText;
        credentials.alignment = TMPro.TextAlignmentOptions.TopRight;
        credentials.transform.position = new Vector3(4.6f, 3.2f, 0);


        if (SpecialEventText == null)
        {
            SpecialEventText = Object.Instantiate(__instance.text);
            SpecialEventText.text = "";
            SpecialEventText.color = Color.white;
            SpecialEventText.fontSize += 2.5f;
            SpecialEventText.alignment = TMPro.TextAlignmentOptions.Top;
            SpecialEventText.transform.position = new Vector3(0, 0.5f, 0);
        }

        SpecialEventText.enabled = TitleLogoPatch.AmongUsLogo != null;
        if (!_init)
            ISpecialDate.CheckDates();
        _init = true;
    }
}

[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
class TitleLogoPatch
{
    public static GameObject AmongUsLogo;
    static void Postfix(MainMenuManager __instance)
    {
        if ((AmongUsLogo = GameObject.Find("bannerLogo_AmongUs")) != null)
        {
            AmongUsLogo.transform.localScale *= 0.4f;
            AmongUsLogo.transform.position += Vector3.up * 0.25f;
        }

        var tohLogo = new GameObject("titleLogo_TOH");
        tohLogo.transform.position = Vector3.up;
        tohLogo.transform.localScale *= 1.2f;
        var renderer = tohLogo.AddComponent<SpriteRenderer>();
        renderer.sprite = Utils.LoadSprite("TownOfHost.assets.tohtor-logo-rold.png", 300f);
    }
}
[HarmonyPatch(typeof(ModManager), nameof(ModManager.LateUpdate))]
class ModManagerLateUpdatePatch
{
    public static void Prefix(ModManager __instance)
    {
        __instance.ShowModStamp();
    }

    public static void Postfix(ModManager __instance)
    {
        var offsetY = HudManager.InstanceExists ? 1.6f : 0.9f;
        __instance.ModStamp.transform.position = AspectPosition.ComputeWorldPosition(
            __instance.localCamera, AspectPosition.EdgeAlignments.RightTop,
            new Vector3(0.4f, offsetY, __instance.localCamera.nearClipPlane + 0.1f));
    }
}