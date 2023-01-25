using TownOfHost.Extensions;
using TownOfHost.Roles.Internals.Attributes;
using TownOfHost.RPC;
using UnityEngine;

namespace TownOfHost.Roles;

public class Doctor : Scientist
{
    [RoleAction(RoleActionType.AnyDeath)]
    private void DoctorAnyDeath()
    {

    }
}