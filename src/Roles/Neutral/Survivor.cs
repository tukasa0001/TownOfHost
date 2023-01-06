using TownOfHost.Extensions;
using TownOfHost.Interface;
using TownOfHost.Interface.Menus.CustomNameMenu;
using TownOfHost.Managers;
using TownOfHost.ReduxOptions;
using UnityEngine;
using TownOfHost.Options;

namespace TownOfHost.Roles;

public class Survivor : CustomRole
{
    private Cooldown vestTimer;
    private float vestCD;
    private float vestDur;

    [DynElement(UI.Counter)]
    private string CustomCooldown() => vestTimer.ToString() == "0" ? "" : Color.white.Colorize(vestTimer + "s");

    [RoleAction(RoleActionType.RoundStart)]
    public void Restart(bool gameStart)
    {
        if (gameStart)
        {
            vestTimer.Duration = 10f;
            vestTimer.Start();
        }
        else
        {
            vestTimer.Duration = vestCD;
            vestTimer.Start();
        }
    }

    [RoleAction(RoleActionType.OnPet)]
    public void OnPet()
    {
        if (vestTimer.NotReady()) return;
        vestTimer.Duration = vestCD + vestDur;
        vestTimer.Start();
    }

    public override bool CanBeKilled() => vestTimer.TimeRemaining() <= (vestCD + vestDur) - vestDur || vestTimer.IsReady();
    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .Tab(DefaultTabs.NeutralTab)
            .AddSubOption(sub => sub
                .Name("Vest Duration")
                .Bind(v => vestDur = (float)v)
                .AddFloatRangeValues(2.5f, 180f, 2.5f, 11, "s")
                .Build())
            .AddSubOption(sub => sub
                .Name("Vest Cooldown")
                .Bind(v => vestCD = (float)v)
                .AddFloatRangeValues(2.5f, 180f, 2.5f, 5, "s")
                .Build());
    protected override RoleModifier Modify(RoleModifier roleModifier)
    {
        return roleModifier
            .SpecialType(SpecialType.Neutral)
            .RoleColor(Utils.ConvertHexToColor("#FFE64D"));
    }
}