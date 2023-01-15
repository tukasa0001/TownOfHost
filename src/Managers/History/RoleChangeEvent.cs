using TownOfHost.Options;
using TownOfHost.Roles;

namespace TownOfHost.Managers.History;

public class RoleChangeEvent: HistoryEvent
{
    private PlayerControl player;
    private CustomRole role;

    public RoleChangeEvent(PlayerControl player, CustomRole role)
    {
        this.player = player;
        this.role = role;
    }

    public override string CreateReport()
    {
        string timestamp = StaticOptions.ShowHistoryTimestamp ? RelativeTimestamp() + " " : "";
        return "";
    }
}