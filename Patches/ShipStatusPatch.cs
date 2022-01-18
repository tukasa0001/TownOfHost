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

namespace TownOfHost
{
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.FixedUpdate))]
    class ShipFixedUpdatePatch
    {
        public static void Postfix(ShipStatus __instance)
        {
            //ここより上、全員が実行する
            if (!AmongUsClient.Instance.AmHost) return;
            //ここより下、ホストのみが実行する
            if (main.isFixedCooldown && PlayerControl.GameOptions.KillCooldown == main.BeforeFixCooldown)
            {
                if (main.RefixCooldownDelay <= 0)
                {
                    PlayerControl.GameOptions.KillCooldown = main.BeforeFixCooldown * 2;
                    PlayerControl.LocalPlayer.RpcSyncSettings(PlayerControl.GameOptions); ;
                }
                else
                {
                    main.RefixCooldownDelay -= Time.fixedDeltaTime;
                }
            }
        }
    }
}
