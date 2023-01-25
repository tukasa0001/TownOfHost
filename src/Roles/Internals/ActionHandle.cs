using TownOfHost.Roles.Internals.Attributes;

namespace TownOfHost.Roles.Internals;

public class ActionHandle
{
    public static ActionHandle NoInit() => new();

    public RoleActionType ActionType;
    public bool IsCanceled;

    public ActionHandle(RoleActionType type)
    {
        this.ActionType = type;
    }

    private ActionHandle() { }

    public void Cancel() => this.IsCanceled = true;

    public override string ToString()
    {
        return $"ActionHandle(type={ActionType}, cancelled={IsCanceled})";
    }
}