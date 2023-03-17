using TOHTOR.Roles.Internals.Attributes;
using TOHTOR.Roles.RoleGroups.Vanilla;
using UnityEngine;

namespace TOHTOR.Roles.RoleGroups.Crew;

public class Baiter: Crewmate
{
    [RoleAction(RoleActionType.MyDeath)]
    private void BaiterDies(PlayerControl killer) => killer.ReportDeadBody(MyPlayer.Data);

    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        base.Modify(roleModifier).RoleColor(new Color(0f, 0.7f, 0.7f));
}