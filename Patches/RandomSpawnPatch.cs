using System;
using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Hazel;

namespace TownOfHost
{
    class RandomSpawn
    {
        [HarmonyPatch(typeof(CustomNetworkTransform), nameof(CustomNetworkTransform.SnapTo), typeof(Vector2), typeof(ushort))]
        public class CustomNetworkTransformPatch
        {
            public static Dictionary<byte, int> NumOfTP = new();
            public static void Postfix(CustomNetworkTransform __instance, [HarmonyArgument(0)] Vector2 position)
            {
            }
        }
    }
}