using HarmonyLib;
using InnerNet;
using TownOfHost.Managers;
using UnityEngine;
using VentLib.Localization;
using VentLib.Logging;
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable InconsistentNaming


namespace TownOfHost.Patches.Client;

[Localized(Group = "ModUpdater")]
[HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.MakePublic))]
class MakePublicPatch
{
    [Localized("ModBrokenMessage")]
    public static string ModBrokenMessage = null!;
    [Localized("ModUpdateMessage")]
    public static string ModUpdateMessage = null!;

    public static bool Prefix(GameStartManager __instance)
    {
        if ((!(ModUpdater.isBroken | ModUpdater.hasUpdate)) || ModUpdater.ForceAccept) return true;
        var message = "";
        if (ModUpdater.isBroken) message = ModBrokenMessage;
        if (ModUpdater.hasUpdate) message = ModUpdateMessage;
        VentLogger.Old(message, "MakePublicPatch");
        VentLogger.SendInGame(message);
        return false;
    }
}

[HarmonyPatch(typeof(MMOnlineManager), nameof(MMOnlineManager.Start))]
class MMOnlineManagerStartPatch
{
    public static void Postfix(MMOnlineManager __instance)
    {
        if (!(ModUpdater.hasUpdate || ModUpdater.isBroken)) return;
        if (ModUpdater.ForceAccept) return;
        var obj = GameObject.Find("FindGameButton");
        if (!obj) return;

        obj?.SetActive(false);
        var parentObj = obj.transform.parent.gameObject;
        var textObj = Object.Instantiate<TMPro.TextMeshPro>(obj.transform.FindChild("Text_TMP").GetComponent<TMPro.TextMeshPro>());
        textObj.transform.position = new Vector3(1f, -0.3f, 0);
        textObj.name = "CanNotJoinPublic";
        var message = ModUpdater.isBroken ? $"<size=2>{Utils.ColorString(Color.red, MakePublicPatch.ModBrokenMessage)}</size>"
            : $"<size=2>{Utils.ColorString(Color.red, MakePublicPatch.ModUpdateMessage)}</size>";
        DTask.Schedule(() => { textObj.text = message; }, 0.01f);
    }
}

[HarmonyPatch(typeof(BanMenu), nameof(BanMenu.SetVisible))]
class BanMenuSetVisiblePatch
{
    public static bool Prefix(BanMenu __instance, bool show)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        show &= PlayerControl.LocalPlayer && PlayerControl.LocalPlayer.Data != null;
        __instance.BanButton.gameObject.SetActive(AmongUsClient.Instance.CanBan());
        __instance.KickButton.gameObject.SetActive(AmongUsClient.Instance.CanKick());
        __instance.MenuButton.gameObject.SetActive(show);
        __instance.hotkeyGlyph.SetActive(show);
        return false;
    }
}
[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.CanBan))]
class InnerNetClientCanBanPatch
{
    public static bool Prefix(InnerNet.InnerNetClient __instance, ref bool __result)
    {
        __result = __instance.AmHost;
        return false;
    }
}
[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.KickPlayer))]
class KickPlayerPatch
{
    public static void Prefix(InnerNetClient __instance, int clientId, bool ban)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (ban) BanManager.AddBanPlayer(AmongUsClient.Instance.GetClient(clientId));
    }
}
[HarmonyPatch(typeof(ResolutionManager), nameof(ResolutionManager.SetResolution))]
class SetResolutionManager
{
    public static void Postfix()
    {
        if (MainMenuManagerPatch.discordButton != null)
            MainMenuManagerPatch.discordButton.transform.position = Vector3.Reflect(MainMenuManagerPatch.template.transform.position, Vector3.left);
        if (MainMenuManagerPatch.updateButton != null)
            MainMenuManagerPatch.updateButton.transform.position = MainMenuManagerPatch.template.transform.position + new Vector3(0.25f, 0.75f);
    }
}