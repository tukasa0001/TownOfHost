using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;

namespace TownOfHost.Roles;

public static class CustomRoleManager
{
    public static List<SimpleRoleInfo> AllRolesInfo = new(Enum.GetValues(typeof(CustomRoles)).Length);
    public static List<RoleBase> AllActiveRoles = new(Enum.GetValues(typeof(CustomRoles)).Length);

    public static SimpleRoleInfo GetRoleInfo(this CustomRoles role) => AllRolesInfo.ToArray().Where(info => info.RoleName == role).FirstOrDefault();
    public static RoleBase GetRoleClass(this PlayerControl player) => GetByPlayerId(player.PlayerId);
    public static RoleBase GetByPlayerId(byte playerId) => AllActiveRoles.ToArray().Where(roleClass => roleClass.Player.PlayerId == playerId).FirstOrDefault();
    public static void Do<T>(this List<T> roleBase, Action<T> action) => roleBase.ToArray().Do(action);
    // == CheckMurder関連処理 ==
    // ==/CheckMurder関連処理 ==
    public static void Initialize()
    {
        AllRolesInfo.Do(role => role.IsEnable = role.RoleName.IsEnable());
        AllActiveRoles.Clear();
    }
    public static void CreateInstance()
    {
        foreach (var pc in Main.AllPlayerControls.ToArray())
        {
            RoleBase _ = pc.GetCustomRole() switch
            {
                //インポスター役職
                CustomRoles.BountyHunter => new BountyHunter(pc),

                //マッドメイト役職
                //CustomRoles.Madmate => new Madmate(pc),

                //クルー役職
                CustomRoles.Sheriff => new Sheriff(pc),

                //第三陣営
                //CustomRoles.Arsonist => new Arsonist(pc),
                _ => null
            };
        }
    }
}