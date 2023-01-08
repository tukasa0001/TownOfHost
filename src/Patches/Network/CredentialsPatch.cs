using System.Globalization;
using HarmonyLib;
using TownOfHost.Addons;
using TownOfHost.Extensions;
using TownOfHost.Managers;
using TownOfHost.Patches.Actions;
using TownOfHost.Roles;
using UnityEngine;

namespace TownOfHost.Patches.Network;

[HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
class VersionShowerStartPatch
{
    static TMPro.TextMeshPro SpecialEventText;
    static void Postfix(VersionShower __instance)
    {
        TOHPlugin.CredentialsText = $"\r\n<color={TOHPlugin.ModColor}>{TOHPlugin.ModName}</color> v{TOHPlugin.PluginVersion}" + (TOHPlugin.DevVersion ? " " + TOHPlugin.DevVersionStr : "");
#if DEBUG
        TOHPlugin.CredentialsText += $"\r\n<color={TOHPlugin.ModColor}>{ThisAssembly.Git.Branch}({ThisAssembly.Git.Commit})</color>";
#endif

        int addonCount = AddonManager.Addons.Count;
        string plural = addonCount == 1 ? "" : "s";
        if (addonCount > 0)
            TOHPlugin.CredentialsText += $"\r\n{new Color(1f, 0.67f, 0.37f).Colorize($"{addonCount} Addon{plural} Loaded")}";


        var credentials = Object.Instantiate(__instance.text);
        credentials.text = TOHPlugin.CredentialsText;
        credentials.alignment = TMPro.TextAlignmentOptions.TopRight;
        credentials.transform.position = new Vector3(4.6f, 3.2f, 0);

        ErrorText.Create(__instance.text);
        if (TOHPlugin.HasArgumentException && ErrorText.Instance != null)
        {
            ErrorText.Instance.AddError(ErrorCode.Main_DictionaryError);
        }

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
        if (SpecialDates.IsInitialRelease)
        {
            SpecialEventText.text = $"Happy Birthday to {TOHPlugin.ModName}!";
            ColorUtility.TryParseHtmlString(TOHPlugin.ModColor, out var col);
            SpecialEventText.color = col;
        }

        if (!SpecialDates.IsChristmas || CultureInfo.CurrentCulture.Name != "ja-JP") return;
        //このソースコ―ドを見た人へ。口外しないでもらえると嬉しいです...
        //To anyone who has seen this source code. I would appreciate it if you would keep your mouth shut...
        SpecialEventText.text = "何とは言いませんが、特別な日ですね。\n<size=15%>\n\n末永く爆発しろ</size>";
        SpecialEventText.color = Utils.GetRoleColor(Lovers.Ref<Lovers>());
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

        DTask.Update(Time.deltaTime);
        MurderPatches.CheckMurderPatch.Update();
    }
    public static void Postfix(ModManager __instance)
    {
        var offsetY = HudManager.InstanceExists ? 1.6f : 0.9f;
        __instance.ModStamp.transform.position = AspectPosition.ComputeWorldPosition(
            __instance.localCamera, AspectPosition.EdgeAlignments.RightTop,
            new Vector3(0.4f, offsetY, __instance.localCamera.nearClipPlane + 0.1f));
    }
}