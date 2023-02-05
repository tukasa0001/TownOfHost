using System.Linq;
using TownOfHost.Extensions;
using TownOfHost.Factions;
using TownOfHost.GUI;
using TownOfHost.Managers;
using TownOfHost.Options;
using UnityEngine;
using VentLib.Options;
using TownOfHost.Roles.Internals;
using TownOfHost.Roles.Internals.Attributes;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;

namespace TownOfHost.Roles;

public class Swapper : CustomRole
{
    private bool canTargetImpostors;
    private bool canTargetNeutrals;

    private PlayerControl? target;

    [DynElement(UI.Misc)]
    private string TargetDisplay() => target == null ? "" : Color.red.Colorize("Target: ") + Color.white.Colorize(target.GetRawName());

    [RoleAction(RoleActionType.RoundStart)]
    public void RoundStart()
    {
        target = Game.GetAllPlayers().Where(p =>
        {
            if (p.PlayerId == MyPlayer.PlayerId) return false;
            Faction[] factions = p.GetCustomRole().Factions;
            if (!canTargetImpostors && factions.IsImpostor()) return false;
            return canTargetNeutrals || !factions.Contains(Faction.Solo);
        }).ToList().GetRandom();
    }


    [RoleAction(RoleActionType.OtherExiled)]
    private void CheckExecutionerWin(PlayerControl exiled)
    {
        if (target == null || target.PlayerId == exiled.PlayerId) return;
        // TODO: Add non-instant win
        //OldRPC.SwapperWin(MyPlayer.PlayerId);
    }

    [RoleAction(RoleActionType.AnyDeath)]
    private void CheckChangeRole(PlayerControl dead)
    {
        if (target == null || target.PlayerId != dead.PlayerId) return;
        target = null;
        MyPlayer.GetDynamicName().Render();
    }

    protected override OptionBuilder RegisterOptions(OptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
        .Tab(DefaultTabs.NeutralTab)
            .SubOption(sub => sub
                .Name("Can Target Impostors")
                .Bind(v => canTargetImpostors = (bool)v)
                .AddOnOffValues(false).Build())
            .SubOption(sub => sub
                .Name("Can Target Neutrals")
                .Bind(v => canTargetNeutrals = (bool)v)
                .AddOnOffValues(false).Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier.RoleColor("#66E666").SpecialType(SpecialType.Neutral);
}