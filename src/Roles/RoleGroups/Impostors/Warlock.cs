using System.Collections.Generic;
using System.Linq;
using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.Roles.Events;
using TOHTOR.Roles.Interactions;
using TOHTOR.Roles.Internals;
using TOHTOR.Roles.Internals.Attributes;
using TOHTOR.Roles.RoleGroups.Vanilla;
using VentLib.Utilities.Extensions;

namespace TOHTOR.Roles.RoleGroups.Impostors;

public class Warlock : Morphling
{
    private List<PlayerControl> cursedPlayers;
    public bool Shapeshifted;

    protected override void Setup(PlayerControl player) => cursedPlayers = new List<PlayerControl>();

    [RoleAction(RoleActionType.Attack)]
    public override bool TryKill(PlayerControl target)
    {
        SyncOptions();
        if (Shapeshifted) return base.TryKill(target);
        InteractionResult result = CheckInteractions(target.GetCustomRole(), target);
        if (result is InteractionResult.Halt) return false;

        cursedPlayers.Add(target);
        MyPlayer.RpcGuardAndKill(MyPlayer);
        return true;
    }

    [RoleAction(RoleActionType.Shapeshift)]
    private void WarlockKillCheck()
    {
        Shapeshifted = true;
        foreach (PlayerControl player in new List<PlayerControl>(cursedPlayers))
        {

            if (player.Data.IsDead)
            {
                cursedPlayers.Remove(player);
                continue;
            }
            List<PlayerControl> inRangePlayers = player.GetPlayersInAbilityRangeSorted().Where(p => !p.GetCustomRole().IsAllied(MyPlayer) && p.GetCustomRole().CanBeKilled()).ToList();
            if (inRangePlayers.Count == 0) continue;
            PlayerControl target = inRangePlayers.GetRandom();
            ManipulatedPlayerDeathEvent playerDeathEvent = new(target, player);
            FatalIntent fatalIntent = new(false, () => playerDeathEvent);
            bool isDead = player.InteractWith(target, new ManipulatedInteraction(fatalIntent, player.GetCustomRole(), MyPlayer)) is InteractionResult.Proceed;
            Game.GameHistory.AddEvent(new ManipulatedPlayerKillEvent(player, target, MyPlayer, isDead));
            cursedPlayers.Remove(player);
        }

        cursedPlayers.RemoveAll(p => p.Data.IsDead);
    }

    [RoleAction(RoleActionType.Unshapeshift)]
    private void WarlockUnshapeshift() => Shapeshifted = false;

    [RoleAction(RoleActionType.RoundEnd)]
    private void WarlockClearCursed() => cursedPlayers.Clear();

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier)
            .OptionOverride(Override.KillCooldown, KillCooldown * 2, () => !Shapeshifted);
}