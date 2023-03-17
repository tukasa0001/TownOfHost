using AmongUs.GameOptions;
using TOHTOR.Extensions;
using TOHTOR.GUI;
using TOHTOR.Options;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using UnityEngine;
using VentLib.Utilities;

namespace TOHTOR.Roles.Subrole;

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