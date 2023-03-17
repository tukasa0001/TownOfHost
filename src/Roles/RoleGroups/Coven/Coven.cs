
using TOHTOR.Roles.Internals;

namespace TOHTOR.Roles.RoleGroups.Coven;

public class Coven: Vanilla.Impostor
{
    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier
            .SpecialType(SpecialType.Coven);
}