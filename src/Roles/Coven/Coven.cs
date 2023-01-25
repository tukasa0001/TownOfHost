
using TownOfHost.Roles.Internals;

namespace TownOfHost.Roles;

public class Coven: Impostor
{
    protected override RoleModifier Modify(RoleModifier roleModifier) =>
        roleModifier
            .SpecialType(SpecialType.Coven);
}