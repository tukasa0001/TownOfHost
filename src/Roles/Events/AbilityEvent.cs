using TOHTOR.Extensions;
using TOHTOR.Managers.History;
using TOHTOR.Managers.History.Events;
using VentLib.Utilities.Optionals;

namespace TOHTOR.Roles.Events;

public abstract class AbilityEvent : IRoleEvent
{
    private PlayerControl user;
    private Optional<CustomRole> role;

    private Timestamp timestamp = new();
    private bool completion;

    public AbilityEvent(PlayerControl user, bool completed = true)
    {
        this.user = user;
        role = Optional<CustomRole>.Of(user.GetCustomRole());
        completion = completed;
    }

    public PlayerControl Player() => user;

    public Optional<CustomRole> RelatedRole() => role;

    public Timestamp Timestamp() => timestamp;

    public bool IsCompletion() => completion;

    public abstract string Message();
}