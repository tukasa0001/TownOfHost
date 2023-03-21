using System;
using System.Collections.Generic;

namespace TOHE.Roles.Impostor;

public static class Concealer
{
    private static readonly int Id = 903534;
    public static List<byte> playerIdList = new();
    public static OptionItem SkillCooldown;
    private static OptionItem SkillDuration;
    public static long HiddenTimeStamp;

    public static void SetupCustomOption()
    {
        Options.SetupRoleOptions(Id, TabGroup.OtherRoles, CustomRoles.Concealer);
        SkillCooldown = FloatOptionItem.Create(Id + 10, "ConcealerSkillCooldown", new(1f, 180f, 1f), 35f, TabGroup.OtherRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Concealer])
            .SetValueFormat(OptionFormat.Seconds);
        SkillDuration = FloatOptionItem.Create(Id + 12, "ConcealerSkillDuration", new(5f, 990f, 1f), 15f, TabGroup.OtherRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.Concealer])
            .SetValueFormat(OptionFormat.Seconds);
    }
    public static void Init()
    {
        playerIdList = new();
        HiddenTimeStamp = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static bool IsHidding => HiddenTimeStamp + SkillDuration.GetFloat() > Utils.GetTimeStamp(DateTime.Now) && GameStates.IsInTask;
    public static void OnShapeshift(PlayerControl pc, bool shapeshifting)
    {
        if (shapeshifting)
            HiddenTimeStamp = Utils.GetTimeStamp(DateTime.Now);
        Camouflage.CheckCamouflage();
    }
    public static void OnFixedUpdate()
    {
        if (IsEnable && !IsHidding && HiddenTimeStamp > new long())
        {
            HiddenTimeStamp = new();
            Camouflage.CheckCamouflage();
        }
    }
}