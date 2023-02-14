using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;

namespace TownOfHost.Roles;

public static class CustomRoleManager
{
    public static List<SimpleRoleInfo> AllRolesInfo = new(Enum.GetValues(typeof(CustomRoles)).Length);
    public static List<RoleBase> AllActiveRoles = new(Enum.GetValues(typeof(CustomRoles)).Length);

    public static void Initialize()
    {
        AllRolesInfo.Do(role => role.IsEnable = role.RoleName.IsEnable());
        AllActiveRoles.Clear();
    }
    public static SimpleRoleInfo GetRoleInfo(this CustomRoles role) => AllRolesInfo.ToArray().Where(info => info.RoleName == role).FirstOrDefault();
    public static RoleBase GetRoleClass(this PlayerControl player) => GetByPlayerId(player.PlayerId);
    public static RoleBase GetByPlayerId(byte playerId) => AllActiveRoles.ToArray().Where(roleClass => roleClass.Player.PlayerId == playerId).FirstOrDefault();
    public static void Do<T>(this List<T> roleBase, Action<T> action) => roleBase.ToArray().Do(action);
    // == CheckMurder関連処理 ==
    // ==/CheckMurder関連処理 ==
}