using TOHTOR.API;
using TOHTOR.Roles.Internals.Attributes;

namespace TOHTOR.Roles.RoleGroups.Impostors;

public class Mafia: Vanilla.Impostor
{
    public static bool CanKill() => GameStates.CountAliveImpostors() <= 1;

    [RoleAction(RoleActionType.Attack)]
    public override bool TryKill(PlayerControl target) => CanKill() && base.TryKill(target);
}