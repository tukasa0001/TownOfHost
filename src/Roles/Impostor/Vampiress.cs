using TownOfHost.Extensions;
using System.Collections.Generic;
using TownOfHost.GUI;
using TownOfHost.Options;
using TownOfHost.Player;

namespace TownOfHost.Roles;

public class Vampiress : Impostor
{
    private float killDelay;
    public VampireMode Mode = VampireMode.Biting;
    private List<byte> bitten;

    protected override void Setup(PlayerControl player) => Pet.Guarantee(player);

    [DynElement(UI.Misc)]
    private string CurrentMode() => Mode == VampireMode.Biting ? RoleColor.Colorize("(Bite)") : RoleColor.Colorize("(Kill)");

    [RoleAction(RoleActionType.AttemptKill)]
    public new bool TryKill(PlayerControl target)
    {
        SyncOptions();
        InteractionResult result = CheckInteractions(target.GetCustomRole(), target);
        if (result is InteractionResult.Halt) return false;
        if (Mode is VampireMode.Killing)
            return RoleUtils.RoleCheckedMurder(MyPlayer, target);

        MyPlayer.RpcGuardAndKill(MyPlayer);
        bitten.Add(target.PlayerId);
        DTask.Schedule(() => { if (bitten.Contains(target.PlayerId)) RoleUtils.RoleCheckedMurder(target, target); }, killDelay);

        return true;
    }

    [RoleAction(RoleActionType.RoundStart)]
    private void EnterKillMode()
    {
        Logger.Blue("Round Started", "Round Start");
        Mode = VampireMode.Killing;
        bitten = new List<byte>();
    }

    [RoleAction(RoleActionType.OnPet)]
    public void SwitchMode()
    {
        "Swapping Vampire Mode".DebugLog();
        MyPlayer.name.DebugLog("My player: s");
        Mode = Mode is VampireMode.Killing ? VampireMode.Biting : VampireMode.Killing;
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

    /*[RoleInteraction(typeof(Veteran))]
    private InteractionResult VeteranBite(PlayerControl veteran) => veteran.GetCustomRole<Veteran>().TryKill(MyPlayer)
        ? InteractionResult.Halt
        : InteractionResult.Proceed;*/

    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .AddSubOption(sub => sub
                .Name("Kill Delay")
                .Bind(v => killDelay = (float)v)
                .AddFloatRangeValues(2.5f, 60f, 2.5f, 2, "s")
                .Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .OptionOverride(Override.KillCooldown, KillCooldown * 2, () => Mode is VampireMode.Biting);

    public enum VampireMode
    {
        Killing,
        Biting
    }
}