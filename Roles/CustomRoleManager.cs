using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;

namespace TownOfHost.Roles;

public static class CustomRoleManager
{
    public static List<RoleBase> AllRoles = new(Enum.GetValues(typeof(CustomRoles)).Length);

    public static void Initialize()
    {
        AllRoles.Do(role => role.IsEnable = role.RoleName.IsEnable());
    }
    public static RoleBase Get(this List<RoleBase> roleBase, CustomRoles role) => roleBase.ToArray().Where(roleClass => roleClass.RoleName == role).FirstOrDefault();
    public static void Do(this List<RoleBase> roleBase, Action<RoleBase> action) => roleBase.ToArray().Do(action);
    // == CheckMurder関連処理 ==
    // ==/CheckMurder関連処理 ==
}