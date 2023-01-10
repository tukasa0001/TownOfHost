using System;

namespace TownOfHost.Gamemodes;

[Flags]
public enum GameAction
{
    ReportBody = 1,
    KillPlayers = 2,
    CallSabotage = 4,
    EnterVent = 8,
}