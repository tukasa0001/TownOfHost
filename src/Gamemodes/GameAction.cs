using System;

namespace TownOfHost.Gamemodes;

[Flags]
public enum GameAction
{
    KillPlayers,
    ReportBody,
    CallSabotage,
}