using System.Collections.Generic;
using UnityEngine;
using TownOfHostForE.Roles.Core;
using static TownOfHostForE.Options;

using TownOfHostForE.Attributes;
namespace TownOfHostForE.Roles.AddOns.Common;

public static class Refusing
{
    private static readonly int Id = 81100;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Refusing);
    public static string SubRoleMark = Utils.ColorString(RoleColor, "Ｒ");
    private static List<byte> playerIdList = new();
    private static List<byte> IgnoreExiled = new();

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Refusing);
    }
    [GameModuleInitializer]
    public static void Init()
    {
        playerIdList = new();
        IgnoreExiled = new();
    }
    public static void Add(byte playerId)
    {
        if (!playerIdList.Contains(playerId))
            playerIdList.Add(playerId);
        if (!IgnoreExiled.Contains(playerId))
            IgnoreExiled.Add(playerId);
    }
    public static GameData.PlayerInfo VoteChange(GameData.PlayerInfo Exiled)
    {
        if (Exiled == null || !IgnoreExiled.Contains(Exiled.PlayerId)) return Exiled;

        IgnoreExiled.Remove(Exiled.PlayerId);
        return null;
    }

    public static bool IsEnable => playerIdList.Count > 0;
    public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);
}