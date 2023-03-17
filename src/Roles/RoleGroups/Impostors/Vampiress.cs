using System.Collections.Generic;
using TOHTOR.Extensions;
using TOHTOR.GUI;
using TOHTOR.Roles.Events;
using TOHTOR.Roles.Interactions;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using TOHTOR.Roles.RoleGroups.Vanilla;
using VentLib.Logging;
using VentLib.Options.Game;
using VentLib.Utilities;

namespace TOHTOR.Roles.RoleGroups.Impostors;

public class Vampiress : Impostor
{
    private float killDelay;
    private VampireMode mode = VampireMode.Biting;
    private List<byte> bitten = null!;

    protected override void Setup(PlayerControl player) => bitten = new List<byte>();

    [DynElement(UI.Misc)]
    private string CurrentMode() => mode is VampireMode.Biting ? RoleColor.Colorize("(Bite)") : RoleColor.Colorize("(Kill)");

    [RoleAction(RoleActionType.Attack)]
    public override bool TryKill(PlayerControl target)
    {
        SyncOptions();
        if (mode is VampireMode.Killing) return base.TryKill(target);
        InteractionResult result = MyPlayer.InteractWith(target, SimpleInteraction.HostileInteraction.Create(this));
        if (result is InteractionResult.Halt) return false;

        MyPlayer.RpcGuardAndKill(target);
        bitten.Add(target.PlayerId);
        Async.Schedule(() =>
        {
            FatalIntent intent = new(true, () => new BittenDeathEvent(target, MyPlayer));
            DelayedInteraction interaction = new(intent, killDelay, this);
            MyPlayer.InteractWith(target, interaction);
        }, killDelay);

        return false;
    }

    [RoleAction(RoleActionType.RoundStart)]
    private void ResetKillState()
    {
        mode = VampireMode.Killing;
        bitten = new List<byte>();
    }

    [RoleAction(RoleActionType.OnPet)]
    public void SwitchMode()
    {
        VampireMode currentMode = mode;
        mode = mode is VampireMode.Killing ? VampireMode.Biting : VampireMode.Killing;
        VentLogger.Trace($"Swapping Vampire Mode: {currentMode} => {mode}");
    }

    [RoleAction(RoleActionType.RoundEnd)]
    public void KillBitten()
    {
        bitten.ForEach(id => Utils.PlayerById(id).IfPresent(p =>
        {
            FatalIntent intent = new(true, () => new BittenDeathEvent(p, MyPlayer));
            DelayedInteraction interaction = new(intent, killDelay, this);
            MyPlayer.InteractWith(p, interaction);
        }));
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub
                .Name("Kill Delay")
                .BindFloat(v => killDelay = v)
                .AddFloatRange(2.5f, 60f, 2.5f, 2, "s")
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .OptionOverride(Override.KillCooldown, KillCooldown * 2, () => mode is VampireMode.Biting);

    public enum VampireMode
    {
        Killing,
        Biting
    }
}