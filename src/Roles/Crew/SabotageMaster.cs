using TOHTOR.Patches.Systems;
using TOHTOR.Roles.Internals.Attributes;

namespace TOHTOR.Roles;

public class SabotageMaster: Crewmate
{
    [RoleAction(RoleActionType.SabotagePartialFix)]
    private void SaboMasterFixes(SabotageType type, PlayerControl fixer)
    {
        if (fixer.PlayerId != MyPlayer.PlayerId) return;
    }

}