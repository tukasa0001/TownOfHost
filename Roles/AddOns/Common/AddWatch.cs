using System.Collections.Generic;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHostForE.Roles.Core;
using static TownOfHostForE.Options;
using TownOfHostForE.Attributes;

namespace TownOfHostForE.Roles.AddOns.Common;

public static class AddWatch
{
    private static readonly int Id = 75000;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.AddWatch);
    public static string SubRoleMark = Utils.ColorString(RoleColor, "Ｗ");
    private static List<byte> playerIdList = new();

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.AddWatch);
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
    public static void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetBool(BoolOptionNames.AnonymousVotes, false);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);
}