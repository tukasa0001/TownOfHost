using AmongUs.GameOptions;
using TownOfHost.Extensions;
using TownOfHost.GUI;
using TownOfHost.Options;
using TownOfHost.Roles.Internals;
using TownOfHost.Roles.Internals.Attributes;
using UnityEngine;
using VentLib.Utilities;

namespace TownOfHost.Roles;

public class Bewilder: Subrole
{
    [DynElement(UI.Subrole)]
    private string SubroleIndicator() => RoleColor.Colorize("★");

    [RoleAction(RoleActionType.MyDeath)]
    private void BaitDies(PlayerControl killer)
    {
        CustomRole role = killer.GetCustomRole();
        role.AddOverride(new GameOptionOverride(Override.ImpostorLightMod, DesyncOptions.OriginalHostOptions.GetFloat(FloatOptionNames.CrewLightMod)));
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier).RoleColor(new Color(0.42f, 0.28f, 0.2f));
}