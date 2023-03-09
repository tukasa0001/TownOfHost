using System;

namespace TOHTOR.Gamemodes;

[Flags]
public enum GameAction
{
    ReportBody = 1,
    CallMeeting = 2,
    KillPlayers = 4,
    CallSabotage = 8,
    EnterVent = 16,

    // These flags cannot be blocked so it doesn't matter if we set them to the following
    GameJoin = 16,
    GameLeave = 17,
    GameStart = 18,
}