using Hazel;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace TOHE.Roles.Impostor;

internal static class QuickShooter
{
    private static readonly int Id = 902522;
    public static List<byte> playerIdList = new();
    private static OptionItem KillCooldown;
    private static OptionItem MeetingReserved;
    public static OptionItem ShapeshiftCooldown;
    public static Dictionary<byte, int> ShotLimit = new();

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.QuickShooter);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(0f, 990f, 1f), 35f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.QuickShooter])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftCooldown = FloatOptionItem.Create(Id + 12, "QuickShooterShapeshiftCooldown", new(5f, 990f, 1f), 15f, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.QuickShooter])
            .SetValueFormat(OptionFormat.Seconds);
        MeetingReserved = IntegerOptionItem.Create(Id + 14, "MeetingReserved", new(0, 100, 1), 2, TabGroup.ImpostorRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.QuickShooter]);
    }
    public static void Init()
    {
        playerIdList = new();
        ShotLimit = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
        ShotLimit.TryAdd(playerId, 0);
        Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()} : 残り{ShotLimit[playerId]}発", "QuickShooter");
    }
    public static bool IsEnable => playerIdList.Count > 0;
    private static void SendRPC(byte playerId)
    {
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetQuickShooterShotLimit, SendOption.Reliable, -1);
        writer.Write(playerId);
        writer.Write(ShotLimit[playerId]);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void ReceiveRPC(MessageReader reader)
    {
        byte QuickShooterId = reader.ReadByte();
        int Limit = reader.ReadInt32();
        if (ShotLimit.ContainsKey(QuickShooterId))
            ShotLimit[QuickShooterId] = Limit;
        else
            ShotLimit.Add(QuickShooterId, 0);
    }
    public static void OnShapeshift(PlayerControl pc, bool shapeshifting)
    {
        if (pc.killTimer == 0 && shapeshifting)
        {
            ShotLimit[pc.PlayerId]++;
            SendRPC(pc.PlayerId);
            Storaging = true;
            pc.ResetKillCooldown();
            pc.SetKillCooldown();
        }
    }
    private static bool Storaging;
    public static void SetKillCooldown(byte id)
    {
        if (Storaging || ShotLimit[id] < 1)
            Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
        else
            Main.AllPlayerKillCooldown[id] = 0.01f;
        Storaging = false;
    }
    public static void OnMeetingStart()
    {
        foreach (var sl in ShotLimit)
        {
            ShotLimit[sl.Key] = Math.Clamp(sl.Value, 0, MeetingReserved.GetInt());
            SendRPC(sl.Key);
        }
    }
    public static void QuickShooterKill(PlayerControl killer)
    {
        if (ShotLimit.ContainsKey(killer.PlayerId))
            ShotLimit[killer.PlayerId]--;
        else
            ShotLimit.TryAdd(killer.PlayerId, 0);
        SendRPC(killer.PlayerId);
    }
    public static string GetShotLimit(byte playerId) => Utils.ColorString(ShotLimit[playerId] > 0 ? Utils.GetRoleColor(CustomRoles.QuickShooter) : Color.gray, ShotLimit.TryGetValue(playerId, out var shotLimit) ? $"({shotLimit})" : "Invalid");
}
