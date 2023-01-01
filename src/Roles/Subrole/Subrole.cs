namespace TownOfHost.Roles;

public class Subrole: CustomRole
{

    protected override RoleModifier Modify(RoleModifier roleModifier) => roleModifier.Subrole(true);
}