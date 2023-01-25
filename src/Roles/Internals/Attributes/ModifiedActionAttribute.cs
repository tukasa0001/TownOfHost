namespace TownOfHost.Roles.Internals.Attributes;

public class ModifiedActionAttribute: RoleActionAttribute
{
    public ModifiedBehaviour Behaviour = ModifiedBehaviour.Replace;

    public ModifiedActionAttribute(RoleActionType actionType, Priority priority = Priority.NoPriority) : base(actionType, priority) { }

    public ModifiedActionAttribute(RoleActionType actionType, ModifiedBehaviour behaviour, Priority priority = Priority.NoPriority)
        : base(actionType, priority)
    {
        Behaviour = behaviour;
    }
}

public enum ModifiedBehaviour
{

    /// /// <summary>
    /// Replaces any Role Actions of the same type declared within the class
    /// </summary>
    Replace,

    /// <summary>
    ///
    /// </summary>
    PatchBefore,
    PatchAfter,
    Addition
}