using System.Collections.Generic;
using UnityEngine;
using TownOfHostForE.Roles.Core;
using static TownOfHostForE.Options;
using TownOfHostForE.Attributes;

namespace TownOfHostForE.Roles.AddOns.Common;

public static class Management
{
    private static readonly int Id = 80500;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Management);
    public static string SubRoleMark = Utils.ColorString(RoleColor, "Ｍ");
    private static List<byte> playerIdList = new();

    private static OptionItem OptionSeeNowtask;
    public static bool SeeNowtask;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Management);
        OptionSeeNowtask = BooleanOptionItem.Create(Id + 10, "ManagementSeeNowtask", true, TabGroup.Addons, false);
    }
    [GameModuleInitializer]
    public static void Init()
    {
        playerIdList = new();

        SeeNowtask = OptionSeeNowtask.GetBool();
    }
    public static void Add(byte playerId)
    {
        if (!playerIdList.Contains(playerId))
            playerIdList.Add(playerId);
    }
    public static string GetProgressText(PlayerState State, bool comms)
    {
        var nowtask = "?";
        int completetask;
        int alltask;
        (completetask, alltask) = Utils.GetTasksState();

        if ((GameStates.IsMeeting || State.IsDead || SeeNowtask)
            && !comms)
            nowtask = $"{completetask}";

        return Utils.ColorString(Color.cyan, $"({nowtask}/{alltask})");
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);
}