using System;

namespace TownOfHost;

[Flags]
public enum SabotageType
{
    Lights = 1,
    Communications = 2,
    Oxygen = 4,
    Reactor = 8,
    Door = 16,
    Helicopter = 32
}