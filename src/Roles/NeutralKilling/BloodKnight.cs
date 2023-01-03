using AmongUs.GameOptions;
using TownOfHost.Extensions;
using TownOfHost.Factions;
using TownOfHost.Options;
using TownOfHost.ReduxOptions;
using TownOfHost.RPC;
using UnityEngine;


namespace TownOfHost.Roles;

public class BloodKnight : NeutralKillingBase
{
    private float protectionAmt;
    private float KillCooldown;
    private bool canVent;
    public bool isProtected;
    protected override RoleModifier Modify(RoleModifier roleModifier)
    {
        return roleModifier
        .RoleName("Blood Knight")
        .RoleColor("#630000")
        .SpecialType(SpecialType.Neutral)
        .OptionOverride(Override.KillCooldown, () => KillCooldown);
    }

    [RoleAction(RoleActionType.RoundStart)]
    public void Reset()
    {
        isProtected = false;
    }

    [RoleAction(RoleActionType.AttemptKill)]
    public void AttemptKill()
    {
        if (!isProtected)
        {
            isProtected = true;
            DTask.Schedule(() => isProtected = false, protectionAmt);
        }
    }

    [RoleAction(RoleActionType.VentEnter)]
    public void VentEnter(Vent vent)
    {
        if (!canVent)
            MyPlayer.MyPhysics.RpcBootFromVent(vent.Id);
    }

    public override bool CanVent() => canVent;
    public override bool CanBeKilled() => !isProtected;

    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream) =>
         base.RegisterOptions(optionStream)
             .Tab(DefaultTabs.NeutralTab)
             .AddSubOption(opt =>
                opt.Name("Kill Cooldown")
                .BindFloat(v => KillCooldown = v)
                .AddFloatRangeValues(2.5f, 180f, 2.5f, 11)
                .Build())
            .AddSubOption(opt =>
                opt.Name("Protectioon Duration")
                .BindFloat(v => protectionAmt = v)
                .AddFloatRangeValues(2.5f, 180, 2.5f, 5)
                .Build())
            .AddSubOption(opt =>
                opt.Name("Can Vent")
                .BindBool(v => canVent = v)
                .AddOnOffValues()
                .Build());
}