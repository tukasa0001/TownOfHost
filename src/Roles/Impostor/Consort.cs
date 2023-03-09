using System.Collections.Generic;
using HarmonyLib;
using TOHTOR.Extensions;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using TOHTOR.RPC;
using VentLib.Options.Game;
using VentLib.Utilities;

namespace TOHTOR.Roles;

public class Consort: Morphling
{
    private float roleblockDuration;
    private bool blocking = true;
    private List<byte> locallyBlockedPlayers;

    protected override void Setup(PlayerControl player) => locallyBlockedPlayers = new List<byte>();

    [RoleAction(RoleActionType.AttemptKill)]
    public override bool TryKill(PlayerControl target)
    {
        SyncOptions();
        if (!blocking) return base.TryKill(target);

        InteractionResult result = CheckInteractions(target.GetCustomRole(), target);
        if (result is InteractionResult.Halt) return false;

        CustomRoleManager.RoleBlockedPlayers.Add(target.PlayerId);
        locallyBlockedPlayers.Add(target.PlayerId);

        if (roleblockDuration > 0)
            Async.Schedule(() => {
                locallyBlockedPlayers.Remove(target.PlayerId);
                CustomRoleManager.RoleBlockedPlayers.Remove(target.PlayerId);
            }, roleblockDuration);

        MyPlayer.RpcGuardAndKill(MyPlayer);
        return true;
    }

    [RoleAction(RoleActionType.Shapeshift)]
    private void ConsortModeSwitch(ActionHandle handle)
    {
        if (!blocking) return;
        blocking = false;
        MyPlayer.CRpcShapeshift(MyPlayer, false);
    }

    [RoleAction(RoleActionType.Unshapeshift)]
    private void ConsortUnshift()
    {
        blocking = true;
    }

    [RoleAction(RoleActionType.RoundStart)]
    private void UnblockPlayers()
    {
        locallyBlockedPlayers.Do(b => CustomRoleManager.RoleBlockedPlayers.Remove(b));
        locallyBlockedPlayers.Clear();
    }

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .SubOption(sub => sub
                .Name("Roleblock Duration")
                .Bind(v => roleblockDuration = (float)v)
                .Value(v => v.Text("Until Meeting").Value(-1f).Build())
                .AddFloatRange(5, 120, 5, suffix: "s")
                .Build());


    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .OptionOverride(Override.KillCooldown, KillCooldown * 2, () => blocking);
}