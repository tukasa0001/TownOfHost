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
        private static Dictionary<byte, bool> isDeadCache;

        public static void SetIsDead()
        {
            foreach (var info in GameData.Instance.AllPlayers)
            {
                if (info == null) continue;
                isDeadCache[info.PlayerId] = info.IsDead;
                info.IsDead = false;
            }
        }
        public static void RestoreIsDead()
        {
            foreach (var info in GameData.Instance.AllPlayers)
            {
                if (info == null) continue;
                if (isDeadCache.TryGetValue(info.PlayerId, out bool val)) info.IsDead = val;
            }
        }

        [HarmonyPatch(typeof(GameData), nameof(GameData.Serialize))]
        public static class GameDataSerializePatch
        {
            public static void Postfix(GameData __instance, ref bool __result)
            {
                if (__result) Logger.Info("GameDataが送信されました。", "GameDataSerializePatch");
            }
        }
    }
}