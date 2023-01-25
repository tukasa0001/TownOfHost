using System.Reflection;
using TownOfHost.Roles.Internals.Attributes;

namespace TownOfHost.Roles.Internals;

public class ModifiedAction: RoleAction
{
    public ModifiedBehaviour Behaviour { get; }

    public ModifiedAction(ModifiedActionAttribute attribute, MethodInfo method) : base(attribute, method)
    {
        Behaviour = attribute.Behaviour;
    }

    public override void Execute(AbstractBaseRole role, object[] args)
    {
        method.InvokeAligned(role.Editor!, args);
    }

    public override void ExecuteFixed(AbstractBaseRole role)
    {
        method.Invoke(role.Editor!, null);
    }
}