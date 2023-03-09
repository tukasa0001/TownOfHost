using TOHTOR.Extensions;
using TOHTOR.Options;
using VentLib.Localization.Attributes;

namespace TOHTOR.Managers.History;

[Localized(Group = "HistoryEvent", Subgroup = "Death")]
public class DeathEvent: HistoryEvent
{
    public PlayerControl Killed;
    public PlayerControl? Killer;


    [Localized("PlayerSuicide")]
    private static string suicideString;
    [Localized("PlayerMurdered")]
    private static string murderedString;

    public DeathEvent(PlayerControl killed, PlayerControl? killer)
    {
        this.Killed = killed;
        this.Killer = killer;
    }

    public override string CreateReport()
    {
        string timestamp = StaticOptions.ShowHistoryTimestamp ? RelativeTimestamp() + " " : "";
        return Killer == null
            ? $"{timestamp}{Killed.GetRawName()} {suicideString}"
            : $"{timestamp}{Killer.GetRawName()} {murderedString} {Killed.GetRawName()}";
    }
}