using TownOfHost.API;
using TownOfHost.Extensions;
using TownOfHost.Options;
using VentLib.Options;
using TownOfHost.Roles.Internals;
using TownOfHost.Victory;
using TownOfHost.Victory.Conditions;

namespace TownOfHost.Roles;

public class Opportunist : CustomRole
{
    protected override void Setup(PlayerControl player) => Game.GetWinDelegate().AddSubscriber(WinSubscriber);

    private void WinSubscriber(WinDelegate winDelegate)
    {
        if (!MyPlayer.IsAlive() || winDelegate.GetWinReason() is WinReason.SoloWinner) return;
        winDelegate.GetWinners().Add(MyPlayer);
    }

    protected override RoleModifier Modify(RoleModifier roleModifier)
    {
        return roleModifier
        .RoleColor("#00ff00")
        .SpecialType(SpecialType.Neutral);
    }

    protected override OptionBuilder RegisterOptions(OptionBuilder optionStream) =>
         base.RegisterOptions(optionStream).Tab(DefaultTabs.NeutralTab);
}