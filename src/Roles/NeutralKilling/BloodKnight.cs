using TownOfHost.Extensions;
using TownOfHost.Factions;
using TownOfHost.GUI;
using TownOfHost.Options;
using UnityEngine;
using VentLib.Utilities;

namespace TownOfHost.Roles;

public class BloodKnight : NeutralKillingBase
{
    private float protectionAmt;
    private bool canVent;
    private bool isProtected;

    public override bool CanSabotage() => false;
    public override bool CanBeKilled() => !isProtected;

    // Usually I use Misc but because the Blood Knight's color is hard to see I'm displaying this next to the player's name which requires a bit more hacky code
    [DynElement(UI.Name)]
    private string ProtectedIndicator() => Color.white.Colorize(isProtected ? MyPlayer.GetRawName() + RoleColor.Colorize("•") : MyPlayer.GetRawName());

    [RoleAction(RoleActionType.RoundStart)]
    public void Reset()
    {
        isProtected = false;
    }

    [RoleAction(RoleActionType.AttemptKill)]
    public new bool TryKill(PlayerControl target)
    {
        // Call to Impostor.TryKill()
        bool killed = base.TryKill(target);
        // Possibly died due to veteran
        if (MyPlayer.Data.IsDead) return killed;

        isProtected = true;
        Async.Schedule(() => isProtected = false, protectionAmt);
        return killed;
    }

    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream) =>
         base.RegisterOptions(optionStream)
             .Tab(DefaultTabs.NeutralTab)
             .AddSubOption(opt =>
                opt.Name("Kill Cooldown")
                .BindFloat(v => KillCooldown = v)
                .AddFloatRangeValues(2.5f, 180f, 2.5f, 11, "s")
                .Build())
            .AddSubOption(opt =>
                opt.Name("Protection Duration")
                .BindFloat(v => protectionAmt = v)
                .AddFloatRangeValues(2.5f, 180, 2.5f, 5, "s")
                .Build())
            .AddSubOption(opt =>
                opt.Name("Can Vent")
                .BindBool(v => canVent = v)
                .AddOnOffValues()
                .Build());



    protected override RoleModifier Modify(RoleModifier roleModifier)
    {
        return base.Modify(roleModifier) // call base because we're utilizing some settings setup by NeutralKillingBase
            .RoleName("Blood Knight")
            .Factions(Faction.Solo)
            .RoleColor(new Color(0.47f, 0f, 0f)) // Using Color() because it's easier to edit and get an idea for actual color
            .CanVent(canVent);
    }

}