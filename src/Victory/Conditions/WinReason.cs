namespace TownOfHost.Victory.Conditions;

public enum WinReason
{
    NoWinCondition,
    HostForceEnd,
    FactionLastStanding,
    TasksComplete,
    GamemodeSpecificWin,
    Sabotage,
    RoleSpecificWin,
    /// <summary>
    /// This reason should be used when only the players marked by the initial condition should win (prevents things like Survivor from winning)
    /// </summary>
    SoloWinner
}