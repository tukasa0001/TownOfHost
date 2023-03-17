using System.Collections.Generic;
using HarmonyLib;
using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.Roles.Events;
using TOHTOR.Roles.Interactions;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using TOHTOR.Roles.RoleGroups.Crew;
using VentLib.Options.Game;
using VentLib.Utilities;

namespace TOHTOR.Roles.RoleGroups.Impostors;

public class Vampire : Vanilla.Impostor
{
    private float killDelay;
    private List<PlayerControl> bitten = null!;

    protected override void Setup(PlayerControl player) => bitten = new List<PlayerControl>();

    [RoleAction(RoleActionType.Attack)]
    public override bool TryKill(PlayerControl target)
    {
        InteractionResult result = MyPlayer.InteractWith(target, SimpleInteraction.HostileInteraction.Create(this));
        if (result is InteractionResult.Halt) return false;

        MyPlayer.RpcGuardAndKill(target);
        bitten.Add(target);
        Game.GameHistory.AddEvent(new BittenEvent(MyPlayer, target));
        Async.Schedule(() => {
            FatalIntent intent = new(true, () => new BittenDeathEvent(target, MyPlayer));
            DelayedInteraction interaction = new(intent, killDelay, this);
            MyPlayer.InteractWith(target, interaction);
        }, killDelay);
        return false;
    }

    [RoleAction(RoleActionType.RoundStart)]
    public void ResetBitten() => bitten.Clear();

    [RoleAction(RoleActionType.RoundEnd)]
    public void KillBitten() => bitten.Do(p => p.Attack(p, () => new BittenDeathEvent(p, MyPlayer)));

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub
                .Name("Kill Delay")
                .Bind(v => killDelay = (float)v)
                .AddFloatRange(2.5f, 60f, 2.5f, 2, "s")
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .OptionOverride(Override.KillCooldown, KillCooldown * 2);

    /*case Vampire:
                    __instance.KillButton.OverrideText($"{GetString("VampireBiteButtonText")}");
                    break;*/
}