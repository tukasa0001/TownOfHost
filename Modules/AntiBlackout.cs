using System.Text.RegularExpressions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Hazel;
using HarmonyLib;
using UnityEngine;

namespace TownOfHost
{
    public static class AntiBlackout
    {
        private static Dictionary<byte, bool> isDeadCache = new();

        public static void SetIsDead()
        {
            isDeadCache.Clear();
            foreach (var info in GameData.Instance.AllPlayers)
            {
                if (info == null) continue;
                isDeadCache[info.PlayerId] = info.IsDead;
                info.IsDead = false;
            }
            //GameDataSerializePatch.hasUpdate = true;
            SendGameData();
        }
        public static void RestoreIsDead()
        {
            foreach (var info in GameData.Instance.AllPlayers)
            {
                if (info == null) continue;
                if (isDeadCache.TryGetValue(info.PlayerId, out bool val)) info.IsDead = val;
            }
            isDeadCache.Clear();
            //GameDataSerializePatch.hasUpdate = true;
            SendGameData();
        }

        public static void SendGameData()
        {
            MessageWriter writer = AmongUsClient.Instance.Streams[(int)SendOption.Reliable];
            writer.StartMessage(1);
            writer.WritePacked(GameData.Instance.NetId);
            GameData.Instance.Serialize(writer, true);
            writer.EndMessage();
        }

        [HarmonyPatch(typeof(GameData), nameof(GameData.Serialize))]
        public static class GameDataSerializePatch
        {
            public static bool hasUpdate = false;
            public static void Prefix(GameData __instance, [HarmonyArgument(0)] MessageWriter writer, [HarmonyArgument(1)] ref bool initialState)
            {
                if (hasUpdate) initialState = true;
                hasUpdate = false;
            }
            public static void Postfix(GameData __instance, ref bool __result)
            {
                if (__result) Logger.Info("GameDataが送信されました。", "GameDataSerializePatch");
            }
        }
    }
}