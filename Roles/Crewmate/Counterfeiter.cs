using System.Collections.Generic;
using UnityEngine;

namespace TOHE.Roles.Crewmate;

public static class Counterfeiter
{
    static readonly int Id = 8035600;
    static List<byte> playerIdList = new();
    static Dictionary<byte, List<byte>> clientList = new();
    static List<byte> notActiveList = new();
    public static Dictionary<byte, int> seelLimit = new();
    public static OptionItem CounterfeiterSkillCooldown;
    public static OptionItem CounterfeiterSkillLimitTimes;
    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.OtherRoles, CustomRoles.Counterfeiter);
        CounterfeiterSkillCooldown = FloatOptionItem.Create(Id + 10, "CounterfeiterSkillCooldown", new(2.5f, 900f, 2.5f), 20f, TabGroup.OtherRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Counterfeiter])
            .SetValueFormat(OptionFormat.Seconds);
        CounterfeiterSkillLimitTimes = IntegerOptionItem.Create(Id + 11, "CounterfeiterSkillLimitTimes", new(1, 99, 1), 2, TabGroup.OtherRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Counterfeiter])
            .SetValueFormat(OptionFormat.Times);
    }
    public static void Init()
    {
        playerIdList = new();
        clientList = new();
        notActiveList = new();
        seelLimit = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        seelLimit.Add(playerId, CounterfeiterSkillLimitTimes.GetInt());
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static bool CanUseKillButton(byte playerId)
        => !Main.PlayerStates[playerId].IsDead
        && seelLimit[playerId] >= 1;
    public static string GetSeelLimit(byte playerId) => Utils.ColorString(CanUseKillButton(playerId) ? Utils.GetRoleColor(CustomRoles.Counterfeiter) : Color.gray, seelLimit.TryGetValue(playerId, out var x) ? $"({x})" : "Invalid");
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CanUseKillButton(id) ? CounterfeiterSkillCooldown.GetFloat() : 0f;
    public static bool IsClient(byte playerId)
    {
        foreach (var pc in clientList)
            if (pc.Value.Contains(playerId)) return true;
        return false;
    }
    public static bool IsClient(byte pc, byte tar) => clientList.TryGetValue(pc, out var x) && x.Contains(tar);
    public static bool CanBeClient(PlayerControl pc) => pc != null && pc.IsAlive() && !GameStates.IsMeeting && !IsClient(pc.PlayerId);
    public static bool CanSeel(byte playerId) => playerIdList.Contains(playerId) && seelLimit.TryGetValue(playerId, out int x) && x > 0;
    public static void SeelToClient(PlayerControl pc, PlayerControl target)
    {
        if (pc == null || target == null || !pc.Is(CustomRoles.Counterfeiter)) return;
        seelLimit[pc.PlayerId]--;
        if (!clientList.ContainsKey(pc.PlayerId)) clientList.Add(pc.PlayerId, new());
        clientList[pc.PlayerId].Add(target.PlayerId);
        pc.RpcGuardAndKill(pc);
        notActiveList.Add(pc.PlayerId);
        pc.SetKillCooldown();
        Utils.NotifyRoles(pc);
        Logger.Info($"赝品商 {pc.GetRealName()} 将赝品卖给了 {target.GetRealName()}", "Counterfeiter");
    }
    public static bool OnClientMurder(PlayerControl pc)
    {
        if (!IsClient(pc.PlayerId) || notActiveList.Contains(pc.PlayerId)) return false;
        byte cfId = byte.MaxValue;
        foreach (var cf in clientList)
            if (cf.Value.Contains(pc.PlayerId)) cfId = cf.Key;
        if (cfId == byte.MaxValue) return false;
        var killer = Utils.GetPlayerById(cfId);
        var target = pc;
        if (killer == null) return false;
        target.SetRealKiller(killer);
        target.Data.IsDead = true;
        Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Misfire;
        target.RpcMurderPlayer(target);
        Main.PlayerStates[target.PlayerId].SetDead();
        Logger.Info($"赝品商 {pc.GetRealName()} 的客户 {target.GetRealName()} 因使用赝品走火自杀", "Counterfeiter");
        return true;
    }
    public static void OnMeetingDestroy()
    {
        notActiveList.Clear();
        foreach (var cl in clientList)
            foreach (var pc in cl.Value)
            {
                var target = Utils.GetPlayerById(pc);
                if (target == null || !target.IsAlive()) continue;
                var role = target.GetCustomRole();
                if (
                    (role.IsCrewmate() && !role.IsCK()) ||
                    (role.IsNeutral() && !role.IsNK())
                    )
                {
                    var killer = Utils.GetPlayerById(cl.Key);
                    if (killer == null) continue;
                    target.SetRealKiller(killer);
                    target.Data.IsDead = true;
                    Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Misfire;
                    target.MurderPlayer(target);
                    Main.PlayerStates[target.PlayerId].SetDead();
                    Logger.Info($"赝品商 {killer.GetRealName()} 的客户 {target.GetRealName()} 因不带刀自杀", "Counterfeiter");
                }
            }
    }
}