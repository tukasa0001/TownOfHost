using System.Collections.Generic;
using System.Linq;
using TownOfHost.Extensions;
using AmongUs.GameOptions;
using TownOfHost.GUI;
using TownOfHost.Options;
using TownOfHost.Player;
using VentLib.Utilities;

namespace TownOfHost.Roles;

public class Ninja : Impostor
{
    private List<PlayerControl> playerList;
    private bool playerTeleportsToNinja;
    public NinjaMode Mode = NinjaMode.Killing;
    private ActivationType activationType;

    [DynElement(UI.Misc)]
    private string CurrentMode() => RoleColor.Colorize(Mode == NinjaMode.Hunting ? "(Hunting)" : "(Killing)");

    protected override void Setup(PlayerControl player) => playerList = new List<PlayerControl>();

    [RoleAction(RoleActionType.AttemptKill)]
    public override bool TryKill(PlayerControl target)
    {
        SyncOptions();
        if (Mode is NinjaMode.Killing) return base.TryKill(target);
        InteractionResult result = CheckInteractions(target.GetCustomRole(), target);
        if (result is InteractionResult.Halt) return false;

        playerList.Add(target);
        MyPlayer.RpcGuardAndKill(MyPlayer);
        return true;
    }

    [RoleAction(RoleActionType.Shapeshift)]
    private void NinjaTargetCheck()
    {
        if (activationType is not ActivationType.Shapeshift) return;
        Mode = NinjaMode.Hunting;
    }

    [RoleAction(RoleActionType.Unshapeshift)]
    private void NinjaUnShapeShift()
    {
        if (activationType is not ActivationType.Shapeshift) return;
        NinjaHuntAbility();
    }

    [RoleAction(RoleActionType.RoundStart)]
    private void EnterKillMode() => Mode = NinjaMode.Killing;

    [RoleAction(RoleActionType.RoundEnd)]
    private void NinjaClearTarget() => playerList.Clear();

    [RoleAction(RoleActionType.OnPet)]
    public void SwitchMode()
    {
        if (activationType is not ActivationType.PetButton) return;

        if (Mode is NinjaMode.Hunting) NinjaHuntAbility();

        Mode = Mode is NinjaMode.Killing ? NinjaMode.Hunting : NinjaMode.Killing;
    }

    // Heavily simplified logic - I think you were looking at Puppeteer but that role is a bit special since
    // it's not solely the Puppeteer doing the killing so there's more checks needed, here because the Ninja kills all
    // in their Ninja kill list we can just iterate through it then clear it at the end of the action
    private void NinjaHuntAbility()
    {
        if (playerList.Count == 0) return;
        foreach (var target in playerList.Where(target => target.IsAlive()))
        {
            if (!playerTeleportsToNinja)
                MyPlayer.RpcMurderPlayer(target);
            else
            {
                Utils.Teleport(target.NetTransform, MyPlayer.transform.position);
                Async.ScheduleInStep(() => MyPlayer.RpcMurderPlayer(target), 0.25f);
            }
        }

        playerList.Clear();
    }

    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
        .AddSubOption(sub => sub
            .Name("Players Teleport to Ninja")
            .BindBool(v => playerTeleportsToNinja = v)
            .AddOnOffValues(false)
            .Build())
        .AddSubOption(sub => sub
            .Name("Ninja Ability Activation")
            .BindInt(v => activationType = (ActivationType)v)
            .AddValue(v => v.Text("Pet Button").Value(0).Build())
            .AddValue(v => v.Text("Shapeshift Button").Value(1).Build())
            .Build());


    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .VanillaRole(activationType is ActivationType.Shapeshift ? RoleTypes.Shapeshifter : RoleTypes.Impostor)
            .OptionOverride(Override.KillCooldown, KillCooldown * 2, () => Mode is NinjaMode.Hunting);

    public enum NinjaMode
    {
        Killing,
        Hunting
    }
}