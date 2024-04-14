using System.Collections.Generic;
using UnityEngine;
using TownOfHostForE.Roles.Core;
using static TownOfHostForE.Options;
using TownOfHostForE.Attributes;

namespace TownOfHostForE.Roles.AddOns.Common;

public static class Autopsy
{
    private static readonly int Id = 80200;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Autopsy);
    public static string SubRoleMark = Utils.ColorString(RoleColor, "Ａ");
    private static List<byte> playerIdList = new();

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Autopsy);
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