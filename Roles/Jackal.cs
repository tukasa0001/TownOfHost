using System.Collections.Generic;
using Hazel;
using UnityEngine;
using static TownOfHost.Options;

namespace TownOfHost
{
    public static class Jackal
    {
        private static readonly int Id = 50900;
        public static List<byte> playerIdList = new();

        private static OptionItem KillCooldown;
        public static OptionItem CanVent;
        public static OptionItem CanUseSabotage;
        private static OptionItem HasImpostorVision;

        public static void SetupCustomOption()
        {
            //Jackalは1人固定
            SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Jackal, 1);
            KillCooldown = OptionItem.Create(Id + 10, TabGroup.NeutralRoles, Color.white, "KillCooldown", 30, 2.5f, 180, 2.5f, CustomRoleSpawnChances[CustomRoles.Jackal], format: OptionFormat.Seconds);
            CanVent = OptionItem.Create(Id + 11, TabGroup.NeutralRoles, Color.white, "CanVent", true, CustomRoleSpawnChances[CustomRoles.Jackal]);
            CanUseSabotage = OptionItem.Create(Id + 12, TabGroup.NeutralRoles, Color.white, "CanUseSabotage", false, CustomRoleSpawnChances[CustomRoles.Jackal]);
            HasImpostorVision = OptionItem.Create(Id + 13, TabGroup.NeutralRoles, Color.white, "ImpostorVision", true, CustomRoleSpawnChances[CustomRoles.Jackal]);
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