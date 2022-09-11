using System.Collections.Generic;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    public static class SerialKiller
    {
        private static readonly int Id = 1100;
        public static List<byte> playerIdList = new();

        private static CustomOption KillCooldown;
        private static CustomOption TimeLimit;

        private static Dictionary<byte, float> SuicideTimer = new();

        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.SerialKiller);
            KillCooldown = CustomOption.Create(Id + 10, TabGroup.ImpostorRoles, Color.white, "KillCooldown", 20f, 2.5f, 180f, 2.5f, Options.CustomRoleSpawnChances[CustomRoles.SerialKiller]);
            TimeLimit = CustomOption.Create(Id + 11, TabGroup.ImpostorRoles, Color.white, "SerialKillerLimit", 60f, 5f, 900f, 5f, Options.CustomRoleSpawnChances[CustomRoles.SerialKiller]);
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
        public static void ApplyGameOptions(GameOptionsData opt) => opt.RoleOptions.ShapeshifterCooldown = TimeLimit.GetFloat();

        public static void OnCheckMurder(PlayerControl killer, bool isKilledSchrodingerCat = false)
        {
            if (killer.Is(CustomRoles.SerialKiller))
            {
                if (isKilledSchrodingerCat)
                {
                    killer.RpcResetAbilityCooldown();
                    SuicideTimer[killer.PlayerId] = 0f;
                    return;
                }
                else
                {
                    killer.RpcResetAbilityCooldown();
                    SuicideTimer[killer.PlayerId] = 0f;
                    Main.AllPlayerKillCooldown[killer.PlayerId] = KillCooldown.GetFloat();
                    killer.CustomSyncSettings();
                }
            }
        }
        public static void OnReportDeadBody()
        {
            SuicideTimer.Clear();
        }
        public static void FixedUpdate(PlayerControl player)
        {
            if (!player.Is(CustomRoles.SerialKiller)) return; //以下、シリアルキラーのみ実行

            if (GameStates.IsInTask && SuicideTimer.ContainsKey(player.PlayerId))
            {
                if (!player.IsAlive())
                    SuicideTimer.Remove(player.PlayerId);
                else if (SuicideTimer[player.PlayerId] >= TimeLimit.GetFloat())
                {
                    //自爆時間が来たとき
                    PlayerState.SetDeathReason(player.PlayerId, PlayerState.DeathReason.Suicide);//死因：自爆
                    player.RpcMurderPlayerV2(player);//自爆させる
                }
                else
                    SuicideTimer[player.PlayerId] += Time.fixedDeltaTime;//時間をカウント
            }
        }
        public static void GetAbilityButtonText(HudManager __instance) => __instance.AbilityButton.OverrideText($"{GetString("SerialKillerSuicideButtonText")}");
        public static void AfterMeetingTasks()
        {
            foreach (var id in playerIdList)
            {
                if (!PlayerState.isDead[id])
                {
                    Utils.GetPlayerById(id)?.RpcResetAbilityCooldown();
                    SuicideTimer[id] = 0f;
                }
            }
        }
    }
}