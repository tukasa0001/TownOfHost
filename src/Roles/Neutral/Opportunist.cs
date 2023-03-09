using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.Options;
using TOHTOR.Roles.Internals;
using TOHTOR.Victory;
using TOHTOR.Victory.Conditions;
using VentLib.Options.Game;

namespace TOHTOR.Roles;

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

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
         base.RegisterOptions(optionStream).Tab(DefaultTabs.NeutralTab);
}