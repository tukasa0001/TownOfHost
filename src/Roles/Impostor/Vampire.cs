using TownOfHost.Extensions;
using System.Collections.Generic;
using HarmonyLib;
using TownOfHost.Options;
using TownOfHost.Roles.Internals;
using TownOfHost.Roles.Internals.Attributes;
using VentLib.Utilities;

namespace TownOfHost.Roles;

public class Vampire : Impostor
{
    private float killDelay;
    private List<PlayerControl> bitten = null!;

    protected override void Setup(PlayerControl player) => bitten = new List<PlayerControl>();

    [RoleAction(RoleActionType.AttemptKill)]
    public new bool TryKill(PlayerControl target)
    {
        InteractionResult result = CheckInteractions(target.GetCustomRole(), target);
        if (result is InteractionResult.Halt) return false;

        bool canKillTarget = target.GetCustomRole().CanBeKilled();

        if (!canKillTarget) return canKillTarget;

        MyPlayer.RpcGuardAndKill(target);
        bitten.Add(target);
        Async.Schedule(() => RoleUtils.RoleCheckedMurder(target, target), killDelay);
        return canKillTarget;
    }

    [RoleAction(RoleActionType.RoundStart)]
    public void ResetBitten() => bitten.Clear();

    [RoleAction(RoleActionType.RoundEnd)]
    public void KillBitten() => bitten.Do(p => RoleUtils.RoleCheckedMurder(p, p));

    [RoleInteraction(typeof(Veteran))]
    private InteractionResult VeteranBite(PlayerControl veteran) => veteran.GetCustomRole<Veteran>().TryKill(MyPlayer)
        ? InteractionResult.Halt
        : InteractionResult.Proceed;

    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .AddSubOption(sub => sub
                .Name("Kill Delay")
                .Bind(v => killDelay = (float)v)
                .AddFloatRangeValues(2.5f, 60f, 2.5f, 2, "s")
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .OptionOverride(Override.KillCooldown, KillCooldown * 2);

    /*case Vampire:
                    __instance.KillButton.OverrideText($"{GetString("VampireBiteButtonText")}");
                    break;*/
}