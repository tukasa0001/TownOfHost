using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.Roles;
using VentLib.Utilities.Optionals;

namespace TOHTOR.Managers.History.Events;

public class RoleChangeEvent : IRoleChangeEvent
{
    private PlayerControl player;
    private Optional<CustomRole> originalRole;
    private CustomRole newRole;

    private Timestamp timestamp = new();

    public RoleChangeEvent(PlayerControl player, CustomRole newRole, CustomRole? originalRole = null)
    {
        this.player = player;
        this.originalRole = Optional<CustomRole>.Of(originalRole ?? player.GetCustomRole());
        this.newRole = newRole;
    }

    public PlayerControl Player() => player;

    public Optional<CustomRole> RelatedRole() => originalRole;

    public Timestamp Timestamp() => timestamp;

    public bool IsCompletion() => true;

    public string Message()
    {
        return $"{Game.GetName(player)} transformed into {newRole.RoleName}";
    }

    public CustomRole OriginalRole() => originalRole.Get();

    public CustomRole NewRole() => newRole;
}