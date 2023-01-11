using HarmonyLib;
using TownOfHost.Roles;
using TownOfHost;
using UnityEngine;
using static TownOfHost.Translator;

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
        if (TOHPlugin.NoGameEnd) __instance.text.text += $"\r\n" + Utils.ColorString(Color.red, GetString("NoGameEnd"));
        if (OldOptions.IsStandardHAS) __instance.text.text += $"\r\n" + Utils.ColorString(Color.yellow, GetString("StandardHAS"));
        if (OldOptions.CurrentGameMode == CustomGameMode.HideAndSeek) __instance.text.text += $"\r\n" + Utils.ColorString(Color.red, GetString("HideAndSeek"));
        if (DebugModeManager.IsDebugMode) __instance.text.text += "\r\n" + Utils.ColorString(Color.green, "デバッグモード");

        var offsetX = 1.2f; //右端からのオフセット
        if (HudManager.InstanceExists && HudManager._instance.Chat.ChatButton.active) offsetX += 0.8f; //チャットボタンがある場合の追加オフセット
        if (FriendsListManager.InstanceExists && FriendsListManager._instance.FriendsListButton.Button.active) offsetX += 0.8f; //フレンドリストボタンがある場合の追加オフセット
        __instance.GetComponent<AspectPosition>().DistanceFromEdge = new Vector3(offsetX, 0f, 0f);

        if (!GameStates.IsLobby) return;
        if (OldOptions.IsStandardHAS && !CustomRoleManager.Static.Sheriff.IsEnable() && !CustomRoleManager.Static.SerialKiller.IsEnable() && CustomRoleManager.Static.Egoist.IsEnable()) // Egoist
            __instance.text.text += $"\r\n" + Utils.ColorString(Color.red, GetString("Warning.EgoistCannotWin"));
        if (!ControllerManagerUpdatePatch.showPing)
            __instance.text.text = "";
    }
}