namespace TOHTOR.Roles.Subrole;

public class Subrole: CustomRole
{

    protected override RoleModifier Modify(RoleModifier roleModifier) => roleModifier.Subrole(true);
}