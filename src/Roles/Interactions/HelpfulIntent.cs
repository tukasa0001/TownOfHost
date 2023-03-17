using TOHTOR.Extensions;
using TOHTOR.Roles.Interactions.Interfaces;

namespace TOHTOR.Roles.Interactions;

public class HelpfulIntent : IHelpfulIntent
{
    public void Action(PlayerControl actor, PlayerControl target)
    {
    }

    public void Halted(PlayerControl actor, PlayerControl target)
    {
        actor.RpcGuardAndKill(actor);
    }
}