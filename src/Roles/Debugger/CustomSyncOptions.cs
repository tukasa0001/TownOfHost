using AmongUs.GameOptions;
using TownOfHost.Options;

namespace TownOfHost.Roles;

public class CustomSyncOptions
{
    public static void SyncOptions(PlayerControl myPlayer)
    {
        IGameOptions hnsOptions = GameOptionsManager.Instance.hideNSeekGameHostOptions.Cast<IGameOptions>();
        hnsOptions.SetBool(BoolOptionNames.UseFlashlight, true);

        DesyncOptions.SyncToPlayer(hnsOptions, myPlayer);
    }
}