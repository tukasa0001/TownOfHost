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

namespace TownOfHost.Roles;

public class Vampiress : Impostor
{
    private float killDelay;
    public bool InKillMode;
    public VampireMode Mode = VampireMode.Biting;
    public VampireMode previousMode = VampireMode.Biting;

    [RoleAction(RoleActionType.AttemptKill)]
    public new bool TryKill(PlayerControl target)
    {
        InteractionResult result = CheckInteractions(target.GetCustomRole(), target);
        if (result is InteractionResult.Halt) return false;

        bool canKillTarget = target.GetCustomRole().CanBeKilled();

        if (canKillTarget)
        {
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
        }
        return canKillTarget;
    }

    [RoleAction(RoleActionType.RoundStart)]
    public void EnterKillModeOnRoundStart(bool gameStart) => EnterKillMode(gameStart);

    [DynElement(UI.Misc)]
    private string CurrentMode() => Mode == VampireMode.Biting ? RoleColor.Colorize("(Bite)") : RoleColor.Colorize("(Kill)");

    [RoleAction(RoleActionType.OnPet)]
    public void SwitchMode()
    {
        "Swapping Vampire Mode".DebugLog();
        MyPlayer.name.DebugLog("My player: s");
        switch (Mode)
        {
            case VampireMode.Killing:
                EnterBiteMode();
                break;
            case VampireMode.Biting:
                EnterKillMode();
                break;
        }
    }

    private void EnterKillMode(bool FirstTime = false)
    {
        InKillMode = true;
        if (FirstTime)
            RpcV2.Immediate((byte)MyPlayer.NetId, (byte)RpcCalls.SetPetStr).Write("pet_clank").Send(MyPlayer.GetClientId());
        Mode = VampireMode.Killing;
        previousMode = VampireMode.Biting;
    }
    private void EnterBiteMode()
    {
        InKillMode = false;
        Mode = VampireMode.Biting;
        previousMode = VampireMode.Killing;
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
        base.Modify(roleModifier);
    // .OptionOverride(Override.KillCooldown, () => DesyncOptions.OriginalHostOptions.AsNormalOptions()!.KillCooldown * 2);

    public enum VampireMode
    {
        Killing,
        Biting
    }
}