using TownOfHost.Factions;
using TownOfHost.ReduxOptions;
using UnityEngine;
using TownOfHost.Extensions;
using TownOfHost.RPC;
using TownOfHost.Options;
using System.Collections.Generic;
using System.Linq;
using TownOfHost.Interface;
using TownOfHost.Interface.Menus.CustomNameMenu;
using TownOfHost.Managers;

namespace TownOfHost.Roles;

public class Vampiress : Impostor
{
    private float killDelay;
    public VampireMode Mode = VampireMode.Biting;

    protected override void Setup(PlayerControl player) => Pet.Guarantee(player);

    [RoleAction(RoleActionType.AttemptKill)]
    public new bool TryKill(PlayerControl target)
    {
        InteractionResult result = CheckInteractions(target.GetCustomRole(), target);
        if (result is InteractionResult.Halt) return false;
        bool canKillTarget = target.GetCustomRole().CanBeKilled();

        if (!canKillTarget) return false;
        switch (Mode)
        {
            case VampireMode.Biting:
                MyPlayer.RpcGuardAndKill(MyPlayer);
                DTask.Schedule(() => target.RpcMurderPlayer(target), killDelay);
                break;
            case VampireMode.Killing:
                MyPlayer.RpcMurderPlayer(target);
                break;
        }
        return canKillTarget;
    }

    [DynElement(UI.Misc)]
    private string CurrentMode() => Mode == VampireMode.Biting ? RoleColor.Colorize("(Bite)") : RoleColor.Colorize("(Kill)");

    [RoleAction(RoleActionType.RoundStart)]
    private void EnterKillMode() => Mode = VampireMode.Killing;

    [RoleAction(RoleActionType.OnPet)]
    public void SwitchMode()
    {
        "Swapping Vampire Mode".DebugLog();
        MyPlayer.name.DebugLog("My player: s");
        Mode = Mode is VampireMode.Killing ? VampireMode.Biting : VampireMode.Killing;
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