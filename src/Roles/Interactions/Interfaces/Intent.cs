namespace TOHTOR.Roles.Interactions.Interfaces;

// ReSharper disable once IdentifierTypo
// ReSharper disable once InconsistentNaming
public interface Intent
{
    public void Action(PlayerControl actor, PlayerControl target);

    public void Halted(PlayerControl actor, PlayerControl target);
}