using TOHTOR.Roles.Internals.Attributes;
using TOHTOR.Roles.RoleGroups.Vanilla;
using TOHTOR.Victory.Conditions;

namespace TOHTOR.Roles.RoleGroups.Crew;

public class Child: Crewmate
{
    [RoleAction(RoleActionType.AnyDeath)]
    private void CheckChildDeath(PlayerControl target, PlayerControl killer)
    {
        if (target.PlayerId == killer.PlayerId) ManualWin.Activate(MyPlayer, WinReason.RoleSpecificWin, 999);
    }
}