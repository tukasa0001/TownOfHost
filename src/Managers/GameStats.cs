using TownOfHost.Patches.Network;

namespace TownOfHost.Managers;

public static class GameStats
{

    public static int CountAliveImpostors() => Game.GetAliveImpostors().Count;

    public static float DeriveDelay() => (PingTrackerPatch.LastPing * ModConstants.DeriveDelayMultiplier) + ModConstants.DeriveDelayFlatValue;

}