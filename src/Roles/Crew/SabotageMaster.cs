using TownOfHost.Patches.Systems;

namespace TownOfHost.Roles;

public class SabotageMaster: Crewmate
{
    [RoleAction(RoleActionType.SabotagePartialFix)]
    private void SaboMasterFixes(SabotageType type, PlayerControl fixer)
    {
        if (fixer.PlayerId != MyPlayer.PlayerId) return;
    }

}