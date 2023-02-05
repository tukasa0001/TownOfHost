using System.Collections.Generic;
using System.Linq;
using TownOfHost.Extensions;
using TownOfHost.GUI;
using TownOfHost.Managers;
using TownOfHost.Roles.Internals.Attributes;
using UnityEngine;
using VentLib.Utilities;
using VentLib.Utilities.Extensions;

namespace TownOfHost.Roles;

public class Lovers: Subrole
{
    public PlayerControl Partner;
    private bool originalLovers = true;

    [RoleAction(RoleActionType.MyDeath)]
    private void LoversDies()
    {
        if (Partner != null && !Partner.Data.IsDead)
            Partner.RpcMurderPlayer(Partner);
    }

    protected override void Setup(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        player.GetDynamicName().SetComponentValue(UI.Subrole, new DynamicString(RoleColor.Colorize("♡")));
        if (Partner != null)
            Partner.GetDynamicName().AddRule(GameState.Roaming, UI.Subrole, new DynamicString(RoleColor.Colorize("♡")), MyPlayer.PlayerId);

        if (!originalLovers) return;

        List<PlayerControl> matchCandidates = Game.GetAllPlayers().Where(p => p.PlayerId != player.PlayerId && p.GetSubrole<Lovers>() == null).ToList();
        if (!matchCandidates.Any()) return;
        Partner = matchCandidates.GetRandom();
        Partner.GetDynamicName().AddRule(GameState.Roaming, UI.Subrole, new DynamicString(RoleColor.Colorize("♡")), MyPlayer.PlayerId);
        Lovers otherLovers = (Lovers)this.Clone();
        otherLovers.Partner = player;
        otherLovers.originalLovers = false;

        Game.AssignSubrole(Partner, otherLovers);
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier).RoleColor(new Color(1f, 0.4f, 0.8f));
}