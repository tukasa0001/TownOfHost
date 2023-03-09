
using TOHTOR.Roles.Internals;

namespace TOHTOR.Roles;

public class Coven: Impostor
{
    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier
            .SpecialType(SpecialType.Coven);
}