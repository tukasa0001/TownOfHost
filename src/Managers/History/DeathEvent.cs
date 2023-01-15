using TownOfHost.Options;

namespace TownOfHost.Managers.History;

public class DeathEvent: HistoryEvent
{
    private PlayerControl dead;
    private PlayerControl? killer;

    private static string suicideString;
    private static string murderedString;

    public DeathEvent(PlayerControl dead, PlayerControl? killer)
    {
        this.dead = dead;
        this.killer = killer;
    }

    public override string CreateReport()
    {
        string timestamp = StaticOptions.ShowHistoryTimestamp ? RelativeTimestamp() + " " : "";
        return $"{timestamp}";
    }
}