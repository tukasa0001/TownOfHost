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
    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.ChangeGamePublic))]
    class ChangeGamePublicPatch {
        public static void Prefix(InnerNetClient __instance, [HarmonyArgument(0)] ref bool isPublic) {
            if(main.PluginVersionType == VersionTypes.Beta) {
                if(isPublic) HudManager.Instance.Chat.AddChat(PlayerControl.LocalPlayer, "ベータ版では公開ルームにできません。");
                isPublic = false;
            }
        }
    }
}