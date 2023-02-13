using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;

namespace TownOfHost.Roles;

public static class CustomRoleManager
{
    public static List<RoleInfoBase> AllRoleBasicInfo = new(Enum.GetValues(typeof(CustomRoles)).Length);

    public static void Initialize()
    {
        AllRoleBasicInfo.Do(role => role.IsEnable = role.RoleName.IsEnable());
    }
    public static T Get<T>(this List<T> roleBase, CustomRoles role) where T : RoleInfoBase => roleBase.ToArray().Where(roleClass => roleClass.RoleName == role).FirstOrDefault();
    public static void Do<T>(this List<T> roleBase, Action<T> action) where T : RoleInfoBase => roleBase.ToArray().Do(action);
    // == CheckMurder関連処理 ==
    // ==/CheckMurder関連処理 ==
}