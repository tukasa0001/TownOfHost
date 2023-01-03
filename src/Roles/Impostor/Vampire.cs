using TownOfHost.Extensions;
using TownOfHost.ReduxOptions;

namespace TownOfHost.Roles;

public class Vampire : Impostor
{
    private float killDelay;

    [RoleAction(RoleActionType.AttemptKill)]
    public new bool TryKill(PlayerControl target)
    {
        InteractionResult result = CheckInteractions(target.GetCustomRole(), target);
        if (result is InteractionResult.Halt) return false;

        bool canKillTarget = target.GetCustomRole().CanBeKilled();

        if (canKillTarget)
        {
            MyPlayer.RpcGuardAndKill(MyPlayer);
            DTask.Schedule(() => target.RpcMurderPlayer(target), killDelay);
        }
        return canKillTarget;
    }

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
            .OptionOverride(Override.KillCooldown, () => DesyncOptions.OriginalHostOptions.AsNormalOptions()!.KillCooldown * 2);

    /*case Vampire:
                    __instance.KillButton.OverrideText($"{GetString("VampireBiteButtonText")}");
                    break;*/
}