using TownOfHost.Extensions;
using TownOfHost.ReduxOptions;
using System.Collections.Generic;
using TownOfHost.Options;

namespace TownOfHost.Roles;

public class Vampire : Impostor
{
    private float killDelay;
    private List<byte> bitten;

    [RoleAction(RoleActionType.AttemptKill)]
    public new bool TryKill(PlayerControl target)
    {
        InteractionResult result = CheckInteractions(target.GetCustomRole(), target);
        if (result is InteractionResult.Halt) return false;

        bool canKillTarget = target.GetCustomRole().CanBeKilled();

        if (!canKillTarget) return canKillTarget;

        MyPlayer.RpcGuardAndKill(MyPlayer);
        bitten.Add(target.PlayerId);
        DTask.Schedule(() => { if (bitten.Contains(target.PlayerId)) target.RpcMurderPlayer(target); }, killDelay);
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

    [RoleAction(RoleActionType.RoundStart)]
    public void ResetBitten()
    {
        bitten = new List<byte>();
    }

    [RoleAction(RoleActionType.RoundEnd)]
    public void KillBitten()
    {
        foreach (var playerid in bitten)
        {
            var pc = Utils.GetPlayerById(playerid);
            bool canKillTarget = pc.GetCustomRole().CanBeKilled();
            if (!canKillTarget) return;
            pc.RpcMurderPlayer(pc);
        }
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .OptionOverride(Override.KillCooldown, KillCooldown * 2);

    /*case Vampire:
                    __instance.KillButton.OverrideText($"{GetString("VampireBiteButtonText")}");
                    break;*/
}