using TownOfHost.Roles.Internals.Attributes;

namespace TownOfHost.Roles;

public class Doctor : Scientist
{
    [RoleAction(RoleActionType.AnyDeath)]
    private void DoctorAnyDeath()
    {

    }
}