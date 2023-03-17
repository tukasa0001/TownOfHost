using TOHTOR.Roles.Interactions.Interfaces;

namespace TOHTOR.Roles.Interactions;

public class RangedInteraction : SimpleInteraction, IRangedInteraction
{
    private readonly float distance;

    public RangedInteraction(Intent intent, float distance, CustomRole? customRole = null) : base(intent, customRole)
    {
        this.distance = distance;
    }

    public float Distance() => distance;
}