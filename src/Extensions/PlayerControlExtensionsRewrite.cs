using System.Collections.Generic;
using TownOfHost.Roles;

namespace TownOfHost.Extensions;

public static class PlayerControlExtensionsRewrite
{
    public static void Trigger(this PlayerControl player, RoleActionType action, ref ActionHandle handle, params object[] parameters)
    {
        CustomRole role = player.GetCustomRole();
        List<Subrole> subroles = player.GetSubroles();
        role.Trigger(action, ref handle, parameters);
        if (handle is { IsCanceled: true }) return;
        foreach (Subrole subrole in subroles)
        {
            subrole.Trigger(action, ref handle, parameters);
            if (handle is { IsCanceled: true }) return;
        }
    }

    public static Subrole? GetSubrole(this PlayerControl player)
    {
        List<Subrole>? role = CustomRoleManager.PlayerSubroles.GetValueOrDefault(player.PlayerId);
        if (role == null || role.Count == 0) return null;
        return role[0];
    }

    public static T GetSubrole<T>(this PlayerControl player) where T : Subrole
    {
        return (T)player.GetSubrole()!;
    }

    public static List<Subrole> GetSubroles(this PlayerControl player)
    {
        return CustomRoleManager.PlayerSubroles.GetValueOrDefault(player.PlayerId, new List<Subrole>());
    }

    public static string GetRoleName(this PlayerControl player) => player.GetCustomRole().RoleName;

    public static string? GetRawName(this PlayerControl? player, bool isMeeting = false)
    {
        return player.GetDynamicName()?.RawName ?? player.Data.PlayerName;
    }
}