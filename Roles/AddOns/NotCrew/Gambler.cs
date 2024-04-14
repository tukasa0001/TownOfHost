using System.Collections.Generic;
using UnityEngine;
using TownOfHostForE.Roles.Core;
using static TownOfHostForE.Options;
using TownOfHostForE.Attributes;

namespace TownOfHostForE.Roles.AddOns.NotCrew;

public static class Gambler
{
    private static readonly int Id = 85500;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Gambler);
    public static string SubRoleMark = Utils.ColorString(RoleColor, "$");
    private static List<byte> playerIdList = new();

    public static OptionItem GaCanGuessTime;
    public static OptionItem GaCanGuessVanilla;
    public static OptionItem GaCanGuessTaskDoneSnitch;
    public static OptionItem GaTryHideMsg;
    public static OptionItem ChangeGuessDeathReason;
    public static OptionItem GaCantWhiteCrew;

    enum OptionName
    {
        GuesserCanGuessTimes,
        EGCanGuessImp,
        EGCanGuessVanilla,
        EGCanGuessTaskDoneSnitch,
        EGGuesserTryHideMsg,
        ChangeGuessDeathReason,
        GuessCantWhiteCrew,
    }
    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Gambler);
        GaCanGuessTime = IntegerOptionItem.Create(Id + 10, OptionName.GuesserCanGuessTimes, new(1, 15, 1), 3, TabGroup.Addons, false)
                .SetValueFormat(OptionFormat.Players);
        GaCanGuessVanilla = BooleanOptionItem.Create(Id + 12, OptionName.EGCanGuessVanilla, false, TabGroup.Addons, false);
        GaCanGuessTaskDoneSnitch = BooleanOptionItem.Create(Id + 13, OptionName.EGCanGuessTaskDoneSnitch, false, TabGroup.Addons, false);
        GaTryHideMsg = BooleanOptionItem.Create(Id + 14, OptionName.EGGuesserTryHideMsg, false, TabGroup.Addons, false);
        ChangeGuessDeathReason = BooleanOptionItem.Create(Id + 15, OptionName.ChangeGuessDeathReason, false, TabGroup.Addons, false);
        GaCantWhiteCrew = BooleanOptionItem.Create(Id + 16, OptionName.GuessCantWhiteCrew, false, TabGroup.Addons, false);
    }
    [GameModuleInitializer]
    public static void Init()
    {
        playerIdList = new();
    }
    public static void Add(byte playerId)
    {
        if (!playerIdList.Contains(playerId))
            playerIdList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);

}