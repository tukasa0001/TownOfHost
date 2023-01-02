using System.Collections.Generic;
using System.Linq;
using TownOfHost.Extensions;
using TownOfHost.ReduxOptions;

namespace TownOfHost.Roles;

public class Warlock: Morphling
{
    private List<PlayerControl> cursedPlayers;
    private bool shapeshifted;

    protected override void Setup(PlayerControl player) => cursedPlayers = new List<PlayerControl>();

    [RoleAction(RoleActionType.AttemptKill)]
    public override bool TryKill(PlayerControl target)
    {
        SyncOptions();
        if (shapeshifted) return base.TryKill(target);
        InteractionResult result = CheckInteractions(target.GetCustomRole(), target);
        if (result is InteractionResult.Halt) return false;

        cursedPlayers.Add(target);
        MyPlayer.RpcGuardAndKill(MyPlayer);
        return true;
    }

    [RoleAction(RoleActionType.Shapeshift)]
    private void WarlockKillCheck()
    {
        shapeshifted = true;
        foreach (PlayerControl player in new List<PlayerControl>(cursedPlayers))
        {

            if (player.Data.IsDead) {
                cursedPlayers.Remove(player);
                continue;
            }
            List<PlayerControl> inRangePlayers = player.GetPlayersInAbilityRangeSorted().Where(p => !p.GetCustomRole().IsAllied(MyPlayer)).ToList();
            if (inRangePlayers.Count == 0) continue;
            player.RpcMurderPlayer(inRangePlayers.GetRandom());
            cursedPlayers.Remove(player);
        }

        cursedPlayers.RemoveAll(p => p.Data.IsDead);
    }

    [RoleAction(RoleActionType.Unshapeshift)]
    private void WarlockUnshapeshift() => shapeshifted = false;

    [RoleAction(RoleActionType.RoundEnd)]
    private void WarlockClearCursed() => cursedPlayers.Clear();

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .OptionOverride(Override.KillCooldown, () => DesyncOptions.OriginalHostOptions.AsNormalOptions()!.KillCooldown * 2, () => !shapeshifted);
}