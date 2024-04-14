using System.Collections.Generic;
using UnityEngine;
using TownOfHostForE.Roles.Core;
using TownOfHostForE.Attributes;
using static TownOfHostForE.Options;

namespace TownOfHostForE.Roles.AddOns.Common;

public static class AddSeer
{
    private static readonly int Id = 80100;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.AddSeer);
    public static string SubRoleMark = Utils.ColorString(RoleColor, "Se");
    private static List<byte> playerIdList = new();

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.AddSeer);
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