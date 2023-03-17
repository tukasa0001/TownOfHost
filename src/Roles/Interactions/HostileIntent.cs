using TOHTOR.Extensions;

namespace TOHTOR.Roles.Interactions;

public class HostileIntent : IHostileIntent
{
    public void Action(PlayerControl actor, PlayerControl target)
    {
    }

    public void Halted(PlayerControl actor, PlayerControl target)
    {
        actor.RpcGuardAndKill(actor);
    }
}