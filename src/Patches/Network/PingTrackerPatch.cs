using HarmonyLib;
using TownOfHost.Roles;
using TownOfHost;
using TownOfHost.Managers;
using UnityEngine;
using VentLib.Localization;


namespace TownOfHost.Patches.Network;

[HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
class PingTrackerPatch
{
    public static int LastPing;

    static void Postfix(PingTracker __instance)
    {
        LastPing = int.Parse(__instance.text.text.Replace("Ping: ", "").Replace(" ms", ""));
        __instance.text.alignment = TMPro.TextAlignmentOptions.TopRight;
        if (ControllerManagerUpdatePatch.showPing)
            __instance.text.text += TOHPlugin.CredentialsText;
        if (TOHPlugin.NoGameEnd) __instance.text.text += $"\r\n" + Utils.ColorString(Color.red, Localizer.Get("StaticOptions.NoGameEnd"));
        __instance.text.text += $"\r\n" + Game.CurrentGamemode.GetName();

        var offsetX = 1.2f; //右端からのオフセット
        if (HudManager.InstanceExists && HudManager._instance.Chat.ChatButton.active) offsetX += 0.8f; //チャットボタンがある場合の追加オフセット
        if (FriendsListManager.InstanceExists && FriendsListManager._instance.FriendsListButton.Button.active) offsetX += 0.8f; //フレンドリストボタンがある場合の追加オフセット
        __instance.GetComponent<AspectPosition>().DistanceFromEdge = new Vector3(offsetX, 0f, 0f);

        if (!GameStates.IsLobby) return;
        if (!ControllerManagerUpdatePatch.showPing)
            __instance.text.text = "";
    }
}