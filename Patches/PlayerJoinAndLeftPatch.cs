using System.Diagnostics;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using System;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnhollowerBaseLib;
using TownOfHost;
using Hazel;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using InnerNet;

namespace TownOfHost {
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
    class OnGameJoinedPatch {
        public static void Postfix(AmongUsClient __instance) {
            Logger.info("RealNamesをリセット");
            main.RealNames = new Dictionary<byte, string>();
        }
    }
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerLeft))]
    class OnPlayerJoinedPatch {
        public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData data, [HarmonyArgument(1)] DisconnectReasons reason) {
            Logger.info($"RealNames[{data.Character.PlayerId}]を削除");
            main.RealNames.Remove(data.Character.PlayerId);
            Logger.info("切断理由:" + reason.ToString());
        }
    }
}