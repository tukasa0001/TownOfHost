using System.Collections.Generic;
using UnityEngine;
using TownOfHostForE.Roles.Core;
using static TownOfHostForE.Options;
using AmongUs.GameOptions;
using TownOfHostForE.Attributes;

namespace TownOfHostForE.Roles.AddOns.Common;

public static class Sunglasses
{
    private static readonly int Id = 85000;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Sunglasses);
    public static string SubRoleMark = Utils.ColorString(RoleColor, "Su");
    private static List<byte> playerIdList = new();

    private static OptionItem OptionSubCrewmateVision;
    private static OptionItem OptionSubImpostorVision;

    public static float SubCrewmateVision;
    public static float SubImpostorVision;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Sunglasses);
        OptionSubCrewmateVision = FloatOptionItem.Create(Id + 10, "SunglassesSubCrewmateVision", new(0f, 5f, 0.05f), 0.2f, TabGroup.Addons, false)
            .SetValueFormat(OptionFormat.Multiplier).SetGameMode(CustomGameMode.Standard);
        OptionSubImpostorVision = FloatOptionItem.Create(Id + 11, "SunglassesSubImpostorVision", new(0f, 5f, 0.1f), 0.5f, TabGroup.Addons, false)
            .SetValueFormat(OptionFormat.Multiplier).SetGameMode(CustomGameMode.Standard);
    }
    [GameModuleInitializer]
    public static void Init()
    {
        playerIdList = new();

        SubCrewmateVision = OptionSubCrewmateVision.GetFloat();
        SubImpostorVision = OptionSubImpostorVision.GetFloat();
    }
    public static void Add(byte playerId)
    {
        if (!playerIdList.Contains(playerId))
            playerIdList.Add(playerId);
    }
    public static void ApplyGameOptions(IGameOptions opt)
    {
        var crewLightMod = FloatOptionNames.CrewLightMod;
        var impostorLightMod = FloatOptionNames.ImpostorLightMod;

        opt.SetFloat(crewLightMod, opt.GetFloat(crewLightMod) - SubCrewmateVision);
        opt.SetFloat(impostorLightMod, opt.GetFloat(impostorLightMod) - SubImpostorVision);
    }

    public static bool IsEnable => playerIdList.Count > 0;
    public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);
}