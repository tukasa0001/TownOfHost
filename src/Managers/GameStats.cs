using TownOfHost.Patches.Network;

namespace TownOfHost.Managers;

public static class GameStats
{

    public static int CountAliveImpostors() => Game.GetAliveImpostors().Count;

    // ReSharper disable once CompareOfFloatsByEqualityOperator
    public static float DeriveDelay(float flatDelay = float.MinValue) =>
        (PingTrackerPatch.LastPing * ModConstants.DeriveDelayMultiplier)
        + (flatDelay == float.MinValue ? ModConstants.DeriveDelayFlatValue : flatDelay);

}