using TOHTOR.Patches.Systems;
using TOHTOR.Roles.Internals.Attributes;
using TOHTOR.Roles.RoleGroups.Vanilla;

namespace TOHTOR.Roles.RoleGroups.Crew;

public class SabotageMaster: Crewmate
{
    [RoleAction(RoleActionType.SabotagePartialFix)]
    private void SaboMasterFixes(SabotageType type, PlayerControl fixer)
    {
        if (fixer.PlayerId != MyPlayer.PlayerId) return;
    }

}