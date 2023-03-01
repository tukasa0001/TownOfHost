using System.Collections.Generic;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE.Roles.Impostor
{
    public static class Gangster
    {
        static readonly int Id = 5054525;
        static List<byte> playerIdList = new();
        private static OptionItem RecruitLimitOpt;
        public static OptionItem KillCooldown;
        public static Dictionary<byte, float> RecruitLimit = new();
        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Gangster);
            KillCooldown = FloatOptionItem.Create(Id + 10, "GangsterRecruitCooldown", new(2.5f, 180f, 2.5f), 20f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gangster])
                .SetValueFormat(OptionFormat.Seconds);
            RecruitLimitOpt = IntegerOptionItem.Create(Id + 12, "GangsterRecruitLimit", new(1, 15, 1), 2, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Gangster])
                .SetValueFormat(OptionFormat.Times);
        }
        public static void Init()
        {
            playerIdList = new();
            RecruitLimit = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
            RecruitLimit.TryAdd(playerId, RecruitLimitOpt.GetInt());
        }
        public static bool IsEnable => playerIdList.Count > 0;
        public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CanUseKillButton(id) ? KillCooldown.GetFloat() : 0f;
        public static bool CanUseKillButton(byte playerId)
            => !Main.PlayerStates[playerId].IsDead
            && RecruitLimit[playerId] > 0;
        public static void SetKillButtonText(byte plaeryId)
        {
            if (CanUseKillButton(plaeryId))
                HudManager.Instance.KillButton.OverrideText($"{GetString("GangsterButtonText")}");
            else
                HudManager.Instance.KillButton.OverrideText($"{GetString("KillButtonText")}");
        }
        public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
        {
            SetKillCooldown(killer.PlayerId);
            if (RecruitLimit[killer.PlayerId] < 1) return false;
            if (Utils.CanBeMadmate(target))
            {
                RecruitLimit[killer.PlayerId]--;
                Main.PlayerStates[target.PlayerId].SetSubRole(CustomRoles.Madmate);
                Utils.NotifyRoles(target);
                Utils.NotifyRoles(killer);
                killer.RpcGuardAndKill(killer);
                killer.RpcGuardAndKill(target);
                target.RpcGuardAndKill(killer);
                target.RpcGuardAndKill(target);
                Logger.Info("役職設定:" + target?.Data?.PlayerName + " = " + target.GetCustomRole().ToString() + " + " + CustomRoles.Madmate.ToString(), "Assign " + CustomRoles.Madmate.ToString());
                if (RecruitLimit[killer.PlayerId] < 0)
                    HudManager.Instance.KillButton.OverrideText($"{GetString("KillButtonText")}");
                Logger.Info($"{killer.GetNameWithRole()} : 剩余{RecruitLimit[killer.PlayerId]}次招募机会", "Gangster");
                return true;
            }
            if (RecruitLimit[killer.PlayerId] < 0)
                HudManager.Instance.KillButton.OverrideText($"{GetString("KillButtonText")}");
            Logger.Info($"{killer.GetNameWithRole()} : 剩余{RecruitLimit[killer.PlayerId]}次招募机会", "Gangster");
            return false;
        }
        public static string GetRecruitLimit(byte playerId) => Utils.ColorString(CanUseKillButton(playerId) ? Color.red : Color.gray, RecruitLimit.TryGetValue(playerId, out var recruitLimit) ? $"({recruitLimit})" : "Invalid");
    }
}