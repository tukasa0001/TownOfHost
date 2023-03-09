using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using TOHTOR.RPC;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;

namespace TOHTOR.Chat.Patches;

[HarmonyPatch(typeof(ChatController), nameof(ChatController.Update))]
public class ChatUpdatePatch
{
    public static bool DoBlockChat = false;
    public static List<(string, byte, string)> MessagesToSend = new();
    public static void Postfix(ChatController __instance)
    {
        /*if (!AmongUsClient.Instance.AmHost || TOHPlugin.MessagesToSend.Count < 1 || (TOHPlugin.MessagesToSend[0].Item2 == byte.MaxValue && TOHPlugin.MessageWait.Value > __instance.TimeSinceLastMessage)) return;*/
        if (MessagesToSend.Count == 0) return;
        if (DoBlockChat) return;
        var player = PlayerControl.AllPlayerControls.ToArray().OrderBy(x => x.PlayerId).FirstOrDefault(x => !x.Data.IsDead);
        if (player == null) return;
        (string msg, byte sendTo, string title) = MessagesToSend.Pop(0);
        int clientId = sendTo == byte.MaxValue ? -1 : Utils.GetPlayerById(sendTo).GetClientId();
        var name = player.Data.PlayerName;
        if (clientId == -1)
        {
            player.SetName(title);
            DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, msg);
            player.SetName(name);
        }
        var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
        writer.StartMessage(clientId);
        writer.StartRpc(player.NetId, (byte)RpcCalls.SetName)
            .Write(title)
            .EndRpc();
        writer.StartRpc(player.NetId, (byte)RpcCalls.SendChat)
            .Write(msg)
            .EndRpc();
        writer.StartRpc(player.NetId, (byte)RpcCalls.SetName)
            .Write(player.Data.PlayerName)
            .EndRpc();
        writer.EndMessage();
        writer.SendMessage();
        __instance.TimeSinceLastMessage = 0f;
    }
}