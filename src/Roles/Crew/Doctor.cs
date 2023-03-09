using TOHTOR.Roles.Internals.Attributes;

namespace TOHTOR.Roles;

public class Doctor : Scientist
{
    [RoleAction(RoleActionType.AnyDeath)]
    private void DoctorAnyDeath()
    {

    }
}