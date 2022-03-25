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
            Logger.info("RealNamesをリセット");
            main.RealNames = new Dictionary<byte, string>();

            NameColorManager.Begin();
        }
    }
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerLeft))]
    class OnPlayerJoinedPatch
    {
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData data, [HarmonyArgument(1)] DisconnectReasons reason)
        {
            Logger.info($"RealNames[{data.Character.PlayerId}]を削除");
            main.RealNames.Remove(data.Character.PlayerId);
            PlayerState.setDeathReason(data.Character.PlayerId, PlayerState.DeathReason.Disconnected);
            PlayerState.isDead[data.Character.PlayerId] = true;
            Logger.info("切断理由:" + reason.ToString());
        }
    }
}