using Hazel;
using System.Collections.Generic;
using UnityEngine;

namespace TOHE.Roles.Crewmate;

public static class SwordsMan
{
    private static readonly int Id = 8021075;
    public static List<byte> playerIdList = new();
    //public static bool isKilled = false;
    public static List<byte> killed = new();

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.SwordsMan);
    }
    public static void Init()
    {
        killed = new();
        playerIdList = new();
    }
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = IsKilled(id) ? 300f : 1f;
    public static string GetKillLimit(byte id) => Utils.ColorString(!IsKilled(id) ? Color.yellow : Color.gray, !IsKilled(id) ? "(1)" : "(0)");
    public static bool CanUseKillButton(byte playerId)
        => !Main.PlayerStates[playerId].IsDead
        && !IsKilled(playerId);
    public static bool IsKilled(byte playerId) => killed.Contains(playerId);
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);

        if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SwordsManKill, SendOption.Reliable, -1);
        writer.Write(playerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte SwordsManId = reader.ReadByte();
        if (!killed.Contains(SwordsManId))
            killed.Add(SwordsManId);
    }
    public static bool OnCheckMurder(PlayerControl killer) => CanUseKillButton(killer.PlayerId);
    public static void OnMurder(PlayerControl killer)
    {
        SendRPC(killer.PlayerId);
        killed.Add(killer.PlayerId);
        Logger.Info($"{killer.GetNameWithRole()} : " + (IsKilled(killer.PlayerId) ? "已使用击杀机会" : "未使用击杀机会"), "SwordsMan");
        SetKillCooldown(killer.PlayerId);
        Utils.NotifyRoles(SpecifySeer: killer);
    }
}