using Hazel;
using System.Collections.Generic;
using UnityEngine;

namespace TOHE.Roles.Crewmate;

public static class Counterfeiter
{
    private static readonly int Id = 8035600;
    private static List<byte> playerIdList = new();
    private static Dictionary<byte, List<byte>> clientList = new();
    private static List<byte> notActiveList = new();
    public static Dictionary<byte, int> SeelLimit = new();
    public static OptionItem CounterfeiterSkillCooldown;
    public static OptionItem CounterfeiterSkillLimitTimes;
    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Counterfeiter);
        CounterfeiterSkillCooldown = FloatOptionItem.Create(Id + 10, "CounterfeiterSkillCooldown", new(2.5f, 900f, 2.5f), 20f, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Counterfeiter])
            .SetValueFormat(OptionFormat.Seconds);
        CounterfeiterSkillLimitTimes = IntegerOptionItem.Create(Id + 11, "CounterfeiterSkillLimitTimes", new(1, 99, 1), 2, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Counterfeiter])
            .SetValueFormat(OptionFormat.Times);
    }
    public static void Init()
    {
        playerIdList = new();
        clientList = new();
        notActiveList = new();
        SeelLimit = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        SeelLimit.Add(playerId, CounterfeiterSkillLimitTimes.GetInt());

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCounterfeiterSellLimit, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(SeelLimit[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte PlayerId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        if (SeelLimit.ContainsKey(PlayerId))
            SeelLimit[PlayerId] = Limit;
        else
            SeelLimit.Add(PlayerId, CounterfeiterSkillLimitTimes.GetInt());
    }
    public static bool CanUseKillButton(byte playerId)
        => !Main.PlayerStates[playerId].IsDead
        && SeelLimit[playerId] >= 1;
    public static string GetSeelLimit(byte playerId) => Utils.ColorString(CanUseKillButton(playerId) ? Utils.GetRoleColor(CustomRoles.Counterfeiter) : Color.gray, SeelLimit.TryGetValue(playerId, out var x) ? $"({x})" : "Invalid");
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = CanUseKillButton(id) ? CounterfeiterSkillCooldown.GetFloat() : 0f;
    public static bool IsClient(byte playerId)
    {
        foreach (var pc in clientList)
            if (pc.Value.Contains(playerId)) return true;
        return false;
    }
    public static bool IsClient(byte pc, byte tar) => clientList.TryGetValue(pc, out var x) && x.Contains(tar);
    public static bool CanBeClient(PlayerControl pc) => pc != null && pc.IsAlive() && !GameStates.IsMeeting && !IsClient(pc.PlayerId);
    public static bool CanSeel(byte playerId) => playerIdList.Contains(playerId) && SeelLimit.TryGetValue(playerId, out int x) && x > 0;
    public static void SeelToClient(PlayerControl pc, PlayerControl target)
    {
        if (pc == null || target == null || !pc.Is(CustomRoles.Counterfeiter)) return;
        SeelLimit[pc.PlayerId]--;
        SendRPC(pc.PlayerId);
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
        target.RpcMurderPlayerV3(target);
        Main.PlayerStates[target.PlayerId].SetDead();
        Logger.Info($"赝品商 {pc.GetRealName()} 的客户 {target.GetRealName()} 因使用赝品走火自杀", "Counterfeiter");
        return true;
    }
    public static void OnMeetingStart()
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
                    CheckForEndVotingPatch.TryAddAfterMeetingDeathPlayers(PlayerState.DeathReason.Misfire, target.PlayerId);
                    target.SetRealKiller(Utils.GetPlayerById(pc));
                    Logger.Info($"赝品商 {killer.GetRealName()} 的客户 {target.GetRealName()} 因不带刀自杀", "Counterfeiter");
                }
            }
    }
}