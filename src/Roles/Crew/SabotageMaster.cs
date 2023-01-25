using TownOfHost.Patches.Systems;
using TownOfHost.Roles.Internals.Attributes;

namespace TownOfHost.Roles;

public class SabotageMaster: Crewmate
{
    [RoleAction(RoleActionType.SabotagePartialFix)]
    private void SaboMasterFixes(SabotageType type, PlayerControl fixer)
    {
        if (fixer.PlayerId != MyPlayer.PlayerId) return;
    }

}