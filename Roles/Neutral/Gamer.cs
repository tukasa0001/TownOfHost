using AmongUs.GameOptions;
using System.Collections.Generic;
using static TOHE.Options;

namespace TOHE.Roles.Neutral;

public static class Gamer
{
    private static readonly int Id = 5060956;
    public static List<byte> playerIdList = new();

    private static Dictionary<byte, float> PlayerHealth;
    private static Dictionary<byte, float> GamerHealth;

    private static OptionItem KillCooldown;
    public static OptionItem CanVent;
    private static OptionItem HasImpostorVision;
    private static OptionItem HealthMax;
    private static OptionItem Damage;
    private static OptionItem SelfHealthMax;
    private static OptionItem SelfDamage;

    public static void SetupCustomOption()
    {
        //玩家只能有一人
        SetupSingleRoleOptions(Id, TabGroup.NeutralRoles, CustomRoles.Gamer, 1, zeroOne: true);
        KillCooldown = FloatOptionItem.Create(Id + 10, "KillCooldown", new(1f, 180f, 1f), 3f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Gamer])
            .SetValueFormat(OptionFormat.Seconds);
        CanVent = BooleanOptionItem.Create(Id + 11, "CanVent", true, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Gamer]);
        HasImpostorVision = BooleanOptionItem.Create(Id + 13, "ImpostorVision", false, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Gamer]);
        HealthMax = FloatOptionItem.Create(Id + 15, "GamerHealthMax", new(5f, 990f, 5f), 100f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Gamer])
            .SetValueFormat(OptionFormat.Percent);
        Damage = FloatOptionItem.Create(Id + 16, "GamerDamage", new(1f, 100f, 1f), 10f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Gamer])
            .SetValueFormat(OptionFormat.Percent);
        SelfHealthMax = FloatOptionItem.Create(Id + 17, "GamerSelfHealthMax", new(5f, 990f, 5f), 100f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Gamer])
            .SetValueFormat(OptionFormat.Percent);
        SelfDamage = FloatOptionItem.Create(Id + 18, "GamerSelfDamage", new(1f, 100f, 1f), 35f, TabGroup.NeutralRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Gamer])
            .SetValueFormat(OptionFormat.Percent);
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
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = KillCooldown.GetFloat();
    public static void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision.GetBool());
    public static void CanUseVent(PlayerControl player)
    {
        bool gamer_canUse = CanVent.GetBool();
        DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(gamer_canUse && !player.Data.IsDead);
        player.Data.Role.CanVent = gamer_canUse;
    }
}