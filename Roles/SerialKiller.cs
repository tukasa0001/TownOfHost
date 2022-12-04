using System.Collections.Generic;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public static class SerialKiller
    {
        private static readonly int Id = 1100;
        public static List<byte> playerIdList = new();

        private static OptionItem KillCooldown;
        private static OptionItem TimeLimit;

        private static Dictionary<byte, float> SuicideTimer = new();

        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.SerialKiller);
            KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(2.5f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.SerialKiller])
                .SetValueFormat(OptionFormat.Seconds);
            TimeLimit = FloatOptionItem.Create(Id + 11, "SerialKillerLimit", new(5f, 900f, 5f), 60f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.SerialKiller])
                .SetValueFormat(OptionFormat.Seconds);
        }
        public static void Init()
        {
            playerIdList = new();
            SuicideTimer = new();
        }
        public static void Add(byte serial)
        {
            playerIdList.Add(serial);
        }
        public static bool IsEnable() => playerIdList.Count > 0;
        public static void ApplyKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
        public static void ApplyGameOptions(GameOptionsData opt, PlayerControl pc)
        {
            opt.RoleOptions.ShapeshifterCooldown = HasKilled(pc) ? TimeLimit.GetFloat() : 255f;
            opt.RoleOptions.ShapeshifterDuration = 1f;
        }
        ///<summary>
        ///シリアルキラー＋生存＋一人以上キルしている
        ///</summary>
        public static bool HasKilled(PlayerControl pc)
            => pc != null && pc.Is(CustomRoles.SerialKiller) && pc.IsAlive() && Main.PlayerStates[pc.PlayerId].GetKillCount(true) > 0;
        public static void OnCheckMurder(PlayerControl killer, bool CanMurder = true)
        {
            if (!killer.Is(CustomRoles.SerialKiller)) return;
            SuicideTimer.Remove(killer.PlayerId);
            if (CanMurder)
                killer.CustomSyncSettings();
        }
        public static void OnReportDeadBody()
        {
            SuicideTimer.Clear();
        }
        public static void FixedUpdate(PlayerControl player)
        {
            if (!HasKilled(player))
            {
                SuicideTimer.Remove(player.PlayerId);
                return;
            }
            if (!SuicideTimer.ContainsKey(player.PlayerId)) //タイマーがない
            {
                SuicideTimer[player.PlayerId] = 0f;
                player.RpcResetAbilityCooldown();
            }
            else if (SuicideTimer[player.PlayerId] >= TimeLimit.GetFloat())
            {
                //自爆時間が来たとき
                Main.PlayerStates[player.PlayerId].deathReason = PlayerState.DeathReason.Suicide;//死因：自殺
                player.RpcMurderPlayerV2(player);//自殺させる
                SuicideTimer.Remove(player.PlayerId);
            }
            else
                SuicideTimer[player.PlayerId] += Time.fixedDeltaTime;//時間をカウント
        }
        public static void GetAbilityButtonText(HudManager __instance, PlayerControl pc)
        {
            __instance.AbilityButton.ToggleVisible(pc.IsAlive() && HasKilled(pc));
            __instance.AbilityButton.OverrideText($"{GetString("SerialKillerSuicideButtonText")}");
        }
        public static void AfterMeetingTasks()
        {
            foreach (var id in playerIdList)
            {
                if (!Main.PlayerStates[id].IsDead)
                {
                    var pc = Utils.GetPlayerById(id);
                    pc?.RpcResetAbilityCooldown();
                    if (HasKilled(pc))
                        SuicideTimer[id] = 0f;
                }
            }
        }
    }
}