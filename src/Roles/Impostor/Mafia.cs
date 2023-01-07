using TownOfHost.Managers;

namespace TownOfHost.Roles;

public class Mafia: Impostor
{
    public static bool CanKill() => GameStats.CountAliveImpostors() <= 1;

    [RoleAction(RoleActionType.AttemptKill)]
    public override bool TryKill(PlayerControl target) => CanKill() && base.TryKill(target);
}