using HarmonyLib;
using System.Collections.Generic;
using InnerNet;

namespace TownOfHost
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
    class OnGameJoinedPatch
    {
        public static void Postfix(AmongUsClient __instance)
        {
            Logger.info("RealNamesをリセット", "OnGameJoined");
            Logger.info($"{__instance.GameId}に参加", "OnGameJoined");
            main.RealNames = new Dictionary<byte, string>();
            main.playerVersion = new Dictionary<byte, PlayerVersion>();
            RPC.RpcVersionCheck();

            NameColorManager.Begin();
            Options.Load();
        }
    }
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
    class OnPlayerJoinedPatch
    {
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData client)
        {

            Logger.info($"{client.PlayerName}(ClientID:{client.Id})が参加", "Session");
            main.playerVersion = new Dictionary<byte, PlayerVersion>();
            RPC.RpcVersionCheck();
        }
    }
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerLeft))]
    class OnPlayerLeftPatch
    {
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData data, [HarmonyArgument(1)] DisconnectReasons reason)
        {
            //            Logger.info($"RealNames[{data.Character.PlayerId}]を削除");
            //            main.RealNames.Remove(data.Character.PlayerId);
            if (data.Character.Is(CustomRoles.TimeThief))
                data.Character.ReturnVotingTime();
            if (!main.isDeadDoused[data.Character.PlayerId])
                data.Character.RemoveDousePlayer();
            PlayerState.setDeathReason(data.Character.PlayerId, PlayerState.DeathReason.Disconnected);
            PlayerState.setDead(data.Character.PlayerId);
            Logger.info($"{data.PlayerName}(ClientID:{data.Id})が切断(理由:{reason})", "Session");
        }
    }
}