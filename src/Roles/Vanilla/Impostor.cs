using AmongUs.GameOptions;
using TownOfHost.Extensions;
using TownOfHost.Factions;
using UnityEngine;

namespace TownOfHost.Roles;

public class Impostor: CustomRole
{
    public virtual bool CanSabotage() => canSabotage;
    public virtual bool CanKill() => canKill;
    protected bool canSabotage = true;
    protected bool canKill = true;

    [RoleAction(RoleActionType.AttemptKill, Subclassing = false)]
    public virtual bool TryKill(PlayerControl target)
    {
        InteractionResult result = CheckInteractions(target.GetCustomRoleREWRITE(), target);
        if (result == InteractionResult.Halt) return false;

        MyPlayer.RpcMurderPlayer(target);
        return true;
    }

    /*[RoleInteraction(typeof(Veteran))]
    public InteractionResult VeteranInteraction(PlayerControl vet)
    {
        return vet.GetCustomRole<Veteran>().TryKill(MyPlayer) ? InteractionResult.Halt : InteractionResult.Proceed;
    }*/

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier
            .VanillaRole(RoleTypes.Impostor)
            .Factions(Faction.Impostors)
            .RoleColor(Color.red);
}