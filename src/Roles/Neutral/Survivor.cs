using TownOfHost.Extensions;
using TownOfHost.GUI;
using TownOfHost.Managers;
using TownOfHost.Options;
using TownOfHost.Victory;
using TownOfHost.Victory.Conditions;

namespace TownOfHost.Roles;

public class Survivor : CustomRole
{
    [DynElement(UI.Cooldown)]
    private Cooldown vestCooldown;
    private Cooldown vestDuration;

    [DynElement(UI.Misc)]
    private string GetVestString() => vestDuration.IsReady() ? "" : RoleColor.Colorize("♣");

    public override bool CanBeKilled() => vestDuration.IsReady();

    protected override void Setup(PlayerControl player)
    {
        base.Setup(player);
        vestDuration.Start(10f);
        Game.GetWinDelegate().AddSubscriber(GameEnd);
    }

    [RoleAction(RoleActionType.RoundStart)]
    public void Restart() => vestCooldown.Start();

    [RoleAction(RoleActionType.OnPet)]
    public void OnPet()
    {
        if (vestCooldown.NotReady()) return;
        vestCooldown.Start();
        vestDuration.Start();
    }

    private void GameEnd(WinDelegate winDelegate)
    {
        if (!MyPlayer.IsAlive() || winDelegate.GetWinReason() is WinReason.SoloWinner) return;
        winDelegate.GetWinners().Add(MyPlayer);
    }

    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .Tab(DefaultTabs.NeutralTab)
            .AddSubOption(sub => sub
                .Name("Vest Duration")
                .BindFloat(v => vestDuration.Duration = v)
                .AddFloatRangeValues(2.5f, 180f, 2.5f, 11, "s")
                .Build())
            .AddSubOption(sub => sub
                .Name("Vest Cooldown")
                .BindFloat(v => vestCooldown.Duration = v)
                .AddFloatRangeValues(2.5f, 180f, 2.5f, 5, "s")
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier
            .SpecialType(SpecialType.Neutral)
            .RoleColor("#FFE64D");
}