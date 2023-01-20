using System.Text.RegularExpressions;
using HarmonyLib;
using TownOfHost.Managers;
using TownOfHost.Options;
using TownOfHost.Patches.Client;
using UnityEngine;
using VentLib.Localization;


namespace TownOfHost.Patches.Network;

[HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
class PingTrackerPatch
{
    static void Postfix(PingTracker __instance)
    {
        __instance.text.alignment = TMPro.TextAlignmentOptions.TopRight;
        if (ControllerManagerUpdatePatch.showPing)
            __instance.text.text += TOHPlugin.CredentialsText;
        if (StaticOptions.NoGameEnd) __instance.text.text += $"\r\n" + Utils.ColorString(Color.red, Localizer.Get("StaticOptions.NoGameEnd"));
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