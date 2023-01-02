using System.Collections.Generic;
using AmongUs.GameOptions;
using TownOfHost.Roles;

namespace TownOfHost.Extensions;

public static class PlayerControlExtensionsRewrite
{
    public static void Trigger(this PlayerControl player, RoleActionType action, ref ActionHandle handle, params object[] parameters)
    {
        CustomRole role = player.GetCustomRoleREWRITE();
        List<Subrole> subroles = player.GetSubroles();
        role.Trigger(action, ref handle, parameters);
        if (handle is { IsCanceled: true }) return;
        foreach (Subrole subrole in subroles)
        {
            subrole.Trigger(action, ref handle, parameters);
            if (handle is { IsCanceled: true }) return;
        }
    }

    public static CustomRole GetCustomRoleREWRITE(this PlayerControl player)
    {
        CustomRole? role = CustomRoleManager.PlayersCustomRolesRedux.GetValueOrDefault(player.PlayerId);
        return role ?? (player.Data.Role == null ? CustomRoleManager.Default
            : player.Data.Role.Role switch
            {
                RoleTypes.Crewmate => CustomRoleManager.Static.Crewmate,
                RoleTypes.Engineer => CustomRoleManager.Static.Engineer,
                RoleTypes.Scientist => CustomRoleManager.Static.Scientist,
                /*RoleTypes.GuardianAngel => CustomRoleManager.Static.GuardianAngel,*/
                RoleTypes.Impostor => CustomRoleManager.Static.Impostor,
                RoleTypes.Shapeshifter => CustomRoleManager.Static.Morphling,
                _ => CustomRoleManager.Default,
            });
    }

    public static T GetCustomRoleREWRITE<T>(this PlayerControl player) where T : CustomRole
    {
        return (T)player.GetCustomRoleREWRITE();
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

    public static string GetRoleName(this PlayerControl player) => player.GetCustomRoleREWRITE().RoleName;

    public static string? GetRawName(this PlayerControl? player, bool isMeeting = false)
    {
        return player.GetDynamicName()?.RawName ?? player.Data.PlayerName;
    }
}