using TOHTOR.API;
using TOHTOR.Roles.Internals.Attributes;

namespace TOHTOR.Roles;

public class Mafia: Impostor
{
    public static bool CanKill() => GameStates.CountAliveImpostors() <= 1;

    [RoleAction(RoleActionType.AttemptKill)]
    public override bool TryKill(PlayerControl target) => CanKill() && base.TryKill(target);
}