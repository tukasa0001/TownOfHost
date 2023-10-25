using HarmonyLib;
using Hazel;

namespace TownOfHost.Patches.ISystemType;

[HarmonyPatch(typeof(SecurityCameraSystemType), nameof(SecurityCameraSystemType.UpdateSystem))]
public static class SecurityCameraSystemTypeUpdateSystemPatch
{
    public static bool Prefix([HarmonyArgument(1)] MessageReader msgReader)
    {
        var newReader = MessageReader.Get(msgReader);
        var amount = newReader.ReadByte();
        // カメラ無効時，バニラプレイヤーはカメラを開けるので点滅させない
        if (amount == SecurityCameraSystemType.IncrementOp)
        {
            var camerasDisabled = (MapNames)Main.NormalOptions.MapId switch
            {
                MapNames.Skeld => Options.DisableSkeldCamera.GetBool(),
                MapNames.Polus => Options.DisablePolusCamera.GetBool(),
                MapNames.Airship => Options.DisableAirshipCamera.GetBool(),
                _ => false,
            };
            return !camerasDisabled;
        }
        return true;
    }
}
