using AmongUs.GameOptions;
using TOHTOR.Options;

namespace TOHTOR.Roles;

public class CustomSyncOptions
{
    public static void SyncOptions(PlayerControl myPlayer)
    {
        IGameOptions hnsOptions = GameOptionsManager.Instance.hideNSeekGameHostOptions.Cast<IGameOptions>();
        hnsOptions.SetBool(BoolOptionNames.UseFlashlight, true);

        DesyncOptions.SyncToPlayer(hnsOptions, myPlayer);
    }
}