using TOHTOR.Roles.Interactions.Interfaces;

namespace TOHTOR.Roles.Interactions;

public class UnblockedInteraction : SimpleInteraction, IUnblockedInteraction
{
    public UnblockedInteraction(Intent intent, CustomRole? customRole = null) : base(intent, customRole)
    {
    }
}