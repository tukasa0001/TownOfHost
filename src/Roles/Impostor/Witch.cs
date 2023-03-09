using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.GUI;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using UnityEngine;
using VentLib.Options.Game;
using VentLib.Utilities;

namespace TOHTOR.Roles;

public class Witch: Impostor
{
    private bool canSwitchWithButton;

    private List<PlayerControl> cursedPlayers;
    private WitchMode mode = WitchMode.Killing;

    protected override void Setup(PlayerControl player) => cursedPlayers = new List<PlayerControl>();

    [DynElement(UI.Misc)]
    private string WitchModeDisplay() =>
        new Color(0.49f, 0.6f, 0.22f).Colorize("Mode: ") + (mode is WitchMode.Killing
            ? Color.red.Colorize("Kill")
            : new Color(0.63f, 0.45f, 1f).Colorize("Spell"));


    [RoleAction(RoleActionType.AttemptKill)]
    public override bool TryKill(PlayerControl target)
    {
        SyncOptions();
        if (mode is WitchMode.Killing)
        {
            mode = WitchMode.Cursing;
            return base.TryKill(target);
        }

        mode = WitchMode.Killing;
        InteractionResult result = CheckInteractions(target.GetCustomRole(), target);
        if (result is InteractionResult.Halt) return false;

        cursedPlayers.Add(target);
        target.GetDynamicName().AddRule(GameState.InMeeting, UI.Name, new DynamicString("{0}" + Color.red.Colorize("†")));


        MyPlayer.RpcGuardAndKill(MyPlayer);
        return true;
    }

    [RoleAction(RoleActionType.OtherExiled)]
    private void WitchKillCheck()
    {
        cursedPlayers.Where(p => !p.Data.IsDead).Do(p =>
        {
            p.RpcMurderPlayer(p);
            p.GetDynamicName().RemoveRule(GameState.InMeeting, UI.Name);
        });
        cursedPlayers.Clear();
    }

    [RoleAction(RoleActionType.OnPet)]
    private void WitchSwitchModes() => mode = canSwitchWithButton ? mode is WitchMode.Killing ? mode = WitchMode.Cursing : WitchMode.Killing : mode;

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub
                .Name("Can Freely Switch Modes")
                .Bind(v => canSwitchWithButton = (bool)v)
                .AddOnOffValues().Build());

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .OptionOverride(Override.KillCooldown, KillCooldown * 2, () => mode == WitchMode.Cursing);

    private enum WitchMode
    {
        Killing,
        Cursing
    }
}