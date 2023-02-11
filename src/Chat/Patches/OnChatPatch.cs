using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using TownOfHost.Extensions;
using VentLib.Utilities;

namespace TownOfHost.Chat.Patches;

[HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
internal static class OnChatPatch
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal static void Prefix(ChatController __instance, PlayerControl sourcePlayer, string chatText)
    {
        if (TOHPlugin.PluginDataManager.ChatManager.HasBannedWord(chatText) && !sourcePlayer.IsHost())
            AmongUsClient.Instance.KickPlayer(sourcePlayer.GetClientId(), false);
    }
}