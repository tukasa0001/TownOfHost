using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using TOHTOR.Extensions;
using VentLib.Utilities;

namespace TOHTOR.Chat.Patches;

[HarmonyPatch(typeof(ChatController), nameof(ChatController.AddChat))]
internal static class OnChatPatch
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal static void Prefix(ChatController __instance, PlayerControl sourcePlayer, string chatText)
    {
        if (!TOHPlugin.PluginDataManager.ChatManager.HasBannedWord(chatText) || sourcePlayer.IsHost()) return;
        AmongUsClient.Instance.KickPlayer(sourcePlayer.GetClientId(), false);
        Utils.SendMessage($"{sourcePlayer.GetRawName()} was kicked by AutoKick.");
    }
}