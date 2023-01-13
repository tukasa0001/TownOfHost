using System.Linq;
using TownOfHost.Extensions;
using TownOfHost.Patches.Network;
using TownOfHost.Roles;

namespace TownOfHost.Managers;

public static class GameStats
{

    public static int CountAliveImpostors() => Game.GetAliveImpostors().Count;
    public static int CountAliveRealImpostors() => Game.GetAlivePlayers().Count(p => p.GetCustomRole().RealRole.IsImpostor());
    public static int CountRealImpostors() => Game.GetAllPlayers().Count(p => p.GetCustomRole().RealRole.IsImpostor());
    public static int CountAliveRealCrew() => Game.GetAlivePlayers().Count(p => p.GetCustomRole().RealRole.IsCrewmate());
    public static int CountRealCrew() => Game.GetAllPlayers().Count(p => p.GetCustomRole().RealRole.IsCrewmate());

    // ReSharper disable once CompareOfFloatsByEqualityOperator
    public static float DeriveDelay(float flatDelay = float.MinValue) =>
        (PingTrackerPatch.LastPing * ModConstants.DeriveDelayMultiplier)
        + (flatDelay == float.MinValue ? ModConstants.DeriveDelayFlatValue : flatDelay);

}