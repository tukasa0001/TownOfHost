using System;
using System.Collections.Generic;
using static TOHE.Options;

namespace TOHE;

public static class Sans
{
    private static readonly int Id = 902055;
    public static List<byte> playerIdList = new();

    private static OptionItem DefaultKillCooldown;
    private static OptionItem ReduceKillCooldown;
    private static OptionItem MinKillCooldown;

    private static Dictionary<byte, float> NowCooldown;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Sans);
        DefaultKillCooldown = FloatOptionItem.Create(Id + 10, "SansDefaultKillCooldown", new(0f, 180f, 2.5f), 65f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sans])
            .SetValueFormat(OptionFormat.Seconds);
        ReduceKillCooldown = FloatOptionItem.Create(Id + 11, "SansReduceKillCooldown", new(0f, 180f, 2.5f), 15f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sans])
            .SetValueFormat(OptionFormat.Seconds);
        MinKillCooldown = FloatOptionItem.Create(Id + 12, "SansMinKillCooldown", new(0f, 180f, 2.5f), 2.5f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Sans])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public static void Init()
    {
        playerIdList = new();
        NowCooldown = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        NowCooldown.TryAdd(playerId, DefaultKillCooldown.GetFloat());
    }
    public static bool IsEnable() => playerIdList.Count > 0;
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = NowCooldown[id];
    public static void OnCheckMurder(PlayerControl killer)
    {
        NowCooldown[killer.PlayerId] = Math.Clamp(NowCooldown[killer.PlayerId] - ReduceKillCooldown.GetFloat(), MinKillCooldown.GetFloat(), DefaultKillCooldown.GetFloat());
        killer.ResetKillCooldown();
        killer.SyncSettings();
    }
}