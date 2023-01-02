namespace TownOfHost.Roles;

public class Mafia: Impostor
{
    public static bool CanKill() => Game.CountAliveImpostors() <= 1;

    [RoleAction(RoleActionType.AttemptKill)]
    public override bool TryKill(PlayerControl target) => CanKill() && base.TryKill(target);
}