using System.Collections.Generic;
using static TOHE.Options;

namespace TOHE.Roles.Impostor;

public static class Hangman
{
    private static readonly int Id = 905367;
    private static List<byte> playerIdList = new();

    private static OptionItem ShapeshiftCooldown;
    private static OptionItem ShapeshiftDuration;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.ImpostorRoles, CustomRoles.Hangman);
        ShapeshiftCooldown = FloatOptionItem.Create(Id + 2, "ShapeshiftCooldown", new(1f, 999f, 1f), 25f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Hangman])
            .SetValueFormat(OptionFormat.Seconds);
        ShapeshiftDuration = FloatOptionItem.Create(Id + 4, "ShapeshiftDuration", new(1f, 999f, 1f), 10f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Hangman])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public static void Init()
    {
        playerIdList = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static void ApplyGameOptions()
    {
        AURoleOptions.ShapeshifterCooldown = ShapeshiftCooldown.GetFloat();
        AURoleOptions.ShapeshifterDuration = ShapeshiftDuration.GetFloat();
    }
    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (Main.CheckShapeshift.TryGetValue(killer.PlayerId, out var s) && s)
        {
            target.Data.IsDead = true;
            Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.LossOfHead;
            target.RpcExileV2();
            Main.PlayerStates[target.PlayerId].SetDead();
            target.SetRealKiller(killer);
            killer.SetKillCooldown();
            return false;
        }
        return true;
    }
}