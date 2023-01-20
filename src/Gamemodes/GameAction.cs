using System;

namespace TownOfHost.Gamemodes;

[Flags]
public enum GameAction
{
    ReportBody = 1,
    KillPlayers = 2,
    CallSabotage = 4,
    EnterVent = 8,

    // These flags cannot be blocked so it doesn't matter if we set them to the following
    GameJoin = 16,
    GameLeave = 17,
    GameStart = 18,
}