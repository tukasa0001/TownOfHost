using System.Collections.Generic;
using System.Linq;
using TownOfHost.Extensions;
using TownOfHost.Interface.Menus.CustomNameMenu;
using UnityEngine;

namespace TownOfHost.Roles;

public class Lovers: Subrole
{
    private bool originalLovers = true;
    private PlayerControl partner;

    [RoleAction(RoleActionType.MyDeath)]
    private void LoversDies()
    {
        if (partner != null && !partner.Data.IsDead)
            partner.RpcMurderPlayer(partner);
    }

    protected override void Setup(PlayerControl player)
    {
        player.GetDynamicName().SetComponentValue(UI.Subrole, new DynamicString(RoleColor.Colorize("♡")));
        if (partner != null)
            partner.GetDynamicName().AddRule(GameState.Roaming, UI.Subrole, new DynamicString(RoleColor.Colorize("♡")), MyPlayer.PlayerId);

        if (!originalLovers) return;
        originalLovers = false;

        List<PlayerControl> matchCandidates = Game.GetAllPlayers().Where(p => p.PlayerId != player.PlayerId).ToList();
        if (!matchCandidates.Any()) return;
        partner = matchCandidates.GetRandom();
        partner.GetDynamicName().AddRule(GameState.Roaming, UI.Subrole, new DynamicString(RoleColor.Colorize("♡")), MyPlayer.PlayerId);
        Lovers otherLovers = (Lovers)this.Instantiate(partner);
        otherLovers.partner = player;

        CustomRoleManager.AddPlayerSubrole(partner.PlayerId, otherLovers);

        originalLovers = true;
    }

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier).RoleColor(new Color(1f, 0.4f, 0.8f));
}