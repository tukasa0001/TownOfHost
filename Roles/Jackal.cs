/*
* Jackal.cs created on Wed Aug 31 2022
* This software is released under the GNU General Public License v3.0.
* Copyright (c) 2022 空き瓶/EmptyBottle
*/

using System.Collections.Generic;
using Hazel;
using UnityEngine;
using static TownOfHost.Translator;
using static TownOfHost.Options;

namespace TownOfHost
{
    public static class Jackal
    {
        private static readonly int Id = 50900;
        public static List<byte> playerIdList = new();

        private static CustomOption KillCooldown;
        public static CustomOption CanVent;
        public static CustomOption CanUseSabotage;
        private static CustomOption HasImpostorVision;

        public static void SetupCustomOption()
        {
            //Jackalは1人固定
            SetupSingleRoleOptions(Id, CustomRoles.Jackal, 1);
            KillCooldown = CustomOption.Create(Id + 10, Color.white, "JackalKillCooldown", 30, 2.5f, 180, 2.5f, CustomRoleSpawnChances[CustomRoles.Jackal]);
            CanVent = CustomOption.Create(Id + 11, Color.white, "JackalCanVent", true, CustomRoleSpawnChances[CustomRoles.Jackal]);
            CanUseSabotage = CustomOption.Create(Id + 12, Color.white, "JackalCanUseSabotage", false, CustomRoleSpawnChances[CustomRoles.Jackal]);
            HasImpostorVision = CustomOption.Create(Id + 13, Color.white, "JackalHasImpostorVision", true, CustomRoleSpawnChances[CustomRoles.Jackal]);
        }
        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);

            if (!AmongUsClient.Instance.AmHost) return;

            if (!Main.ResetCamPlayerList.Contains(playerId))
                Main.ResetCamPlayerList.Add(playerId);
        }
        public static bool IsEnable => playerIdList.Count > 0;
        private static void SendRPC(byte playerId)
        {
        }
        public static void ReceiveRPC(MessageReader reader)
        {
        }
        public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
        public static void ApplyGameOptions(GameOptionsData opt, PlayerControl player) => opt.SetVision(player, HasImpostorVision.GetBool());
        public static void CanUseVent(PlayerControl player)
        {
            bool jackal_canUse = CanVent.GetBool();
            DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(jackal_canUse && !player.Data.IsDead);
            player.Data.Role.CanVent = jackal_canUse;
        }
        public static void SetHudActive(HudManager __instance, bool isActive, PlayerControl player)
        {
            if (player.Data.Role.Role != RoleTypes.GuardianAngel)
                __instance.KillButton.ToggleVisible(isActive && !player.Data.IsDead);
            __instance.SabotageButton.ToggleVisible(isActive && CanUseSabotage.GetBool());
            __instance.ImpostorVentButton.ToggleVisible(isActive && CanVent.GetBool());
            __instance.AbilityButton.ToggleVisible(false);
        }
    }
}