using TOHTOR.Roles.Interactions.Interfaces;

namespace TOHTOR.Roles.Interactions;

public class DelayedInteraction : SimpleInteraction, IDelayedInteraction
{
    private readonly float delay;

    public DelayedInteraction(Intent intent, float delay, CustomRole? customRole = null) : base(intent, customRole)
    {
        this.delay = delay;
    }

    public float Delay() => delay;
}