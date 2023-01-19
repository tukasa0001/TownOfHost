using TownOfHost.Extensions;
using TownOfHost.Managers;
using TownOfHost.Options;
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

    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream) =>
         base.RegisterOptions(optionStream)
             .Tab(DefaultTabs.NeutralTab);
}