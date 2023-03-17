using TOHTOR.API;
using TOHTOR.GUI;
using TOHTOR.Roles.Internals.Attributes;
using TOHTOR.Roles.RoleGroups.Vanilla;

namespace TOHTOR.Roles.RoleGroups.Crew;

public class Doctor : Scientist
{
    [RoleAction(RoleActionType.AnyDeath)]
    private void DoctorAnyDeath(PlayerControl dead)
    {
        string causeOfDeath = Game.GameHistory.GetCauseOfDeath(dead.PlayerId).Map(de => de.SimpleName()).OrElse("Unknown");
        dead.GetDynamicName().AddRule(GameState.InMeeting, UI.Name, new DynamicString("{0} " + causeOfDeath), MyPlayer.PlayerId);
    }
}