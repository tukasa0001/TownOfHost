namespace TownOfHost.Managers;

public static class GameStats
{

    public static int CountAliveImpostors() => Game.GetAliveImpostors().Count;


}