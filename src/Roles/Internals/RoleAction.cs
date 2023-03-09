using System.Reflection;
using TOHTOR.Roles.Internals.Attributes;

namespace TOHTOR.Roles.Internals;

public class RoleAction
{
    public RoleActionType ActionType { get; }
    public Priority Priority { get; }
    public bool Blockable { get; }

    internal RoleActionAttribute Attribute;
    internal MethodInfo method;

    public RoleAction(RoleActionAttribute attribute, MethodInfo method)
    {
        this.method = method;
        this.ActionType = attribute.ActionType;
        this.Priority = attribute.Priority;
        this.Blockable = attribute.Blockable;
        this.Attribute = attribute;
    }

    public virtual void Execute(AbstractBaseRole role, object[] args)
    {
        method.InvokeAligned(role, args);
    }

    public virtual void ExecuteFixed(AbstractBaseRole role)
    {
        method.Invoke(role, null);
    }

    public override string ToString() => $"RoleAction(type={ActionType}, Priority={Priority}, Blockable={Blockable})";
}