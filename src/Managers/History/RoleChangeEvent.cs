using TOHTOR.Extensions;
using TOHTOR.Options;
using TOHTOR.Roles;
using VentLib.Localization.Attributes;

namespace TOHTOR.Managers.History;

[Localized(Group = "HistoryEvent")]
public class RoleChangeEvent: HistoryEvent
{
    private PlayerControl player;
    private CustomRole role;

    [Localized("PlayerChangedRole")]
    private static string roleChangedString;

    public RoleChangeEvent(PlayerControl player, CustomRole role)
    {
        this.player = player;
        this.role = role;
    }

    public override string CreateReport()
    {
        string timestamp = StaticOptions.ShowHistoryTimestamp ? RelativeTimestamp() + " " : "";
        return $"{timestamp}{player.GetRawName()} {roleChangedString} {role.RoleName}";
    }
}