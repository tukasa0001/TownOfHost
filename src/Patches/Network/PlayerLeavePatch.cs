using HarmonyLib;
using InnerNet;
using TownOfHost.Managers;

namespace TownOfHost.Patches.Network;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnDisconnected))]
class OnDisconnectedPatch
{
    public static void Postfix(AmongUsClient __instance)
    {
        TOHPlugin.VisibleTasksCount = false;
    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerLeft))]
class OnPlayerLeftPatch
{
    public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData data, [HarmonyArgument(1)] DisconnectReasons reason)
    {
        Logger.Info($"{data.PlayerName}(ClientID:{data.Id})が切断(理由:{reason}, ping:{AmongUsClient.Instance.Ping})", "Session");
        if (Game.State is GameState.InLobby) return;
        Game.players.Remove(data.Character.PlayerId);
        AntiBlackout.OnDisconnect(data.Character.Data);
    }
}