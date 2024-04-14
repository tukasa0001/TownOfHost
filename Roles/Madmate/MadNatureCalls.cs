using AmongUs.GameOptions;

using TownOfHostForE.Roles.Core;

namespace TownOfHostForE.Roles.Madmate;
public sealed class MadNatureCalls : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(MadNatureCalls),
            player => new MadNatureCalls(player),
            CustomRoles.MadNatureCalls,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Madmate,
            5400,
            SetupOptionItem,
            "マッドネイチャコール",
            introSound: () => GetIntroSound(RoleTypes.Impostor)
        );
    public MadNatureCalls(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
    }

    public static void SetupOptionItem()
    {
        Options.SetUpAddOnOptions(RoleInfo.ConfigId + 10, RoleInfo.RoleName, RoleInfo.Tab);
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, 79);
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, 80);
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, 81);
        ShipStatus.Instance.RpcUpdateSystem(SystemTypes.Doors, 82);
        return true;
    }
}
