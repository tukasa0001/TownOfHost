using System;
using VentLib.Localization;

namespace TownOfHost.Managers.History;

//[Localization(Group = "Hello")]
public abstract class HistoryEvent
{
    private DateTime timestamp = DateTime.Now;

    public abstract string CreateReport();

    public string RelativeTimestamp() => $"[{(timestamp - Game.StartTime):mm\\:ss\\.ff]}";
}