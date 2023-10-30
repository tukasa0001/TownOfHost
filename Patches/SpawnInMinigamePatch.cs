using HarmonyLib;

namespace TownOfHost.Patches;

[HarmonyPatch(typeof(SpawnInMinigame), nameof(SpawnInMinigame.SpawnAt))]
public static class SpawnInMinigameSpawnAtPatch
{
    public static void Postfix()
    {
        if (AmongUsClient.Instance.AmHost)
        {
            RandomSpawn.AirshipSpawn(PlayerControl.LocalPlayer);
        }
    }
}
