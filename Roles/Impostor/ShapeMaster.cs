using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;

public sealed class ShapeMaster : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(ShapeMaster),
            player => new ShapeMaster(player),
            CustomRoles.ShapeMaster,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            1200,
            SetupOptionItem,
            "sha"
        );
    public ShapeMaster(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        shapeshiftDuration = OptionShapeshiftDuration.GetFloat();
    }
    private static OptionItem OptionShapeshiftDuration;
    enum OptionName
    {
        ShapeMasterShapeshiftDuration,
    }
    private static float shapeshiftDuration;

    public static void SetupOptionItem()
    {
        OptionShapeshiftDuration = FloatOptionItem.Create(RoleInfo, 10, OptionName.ShapeMasterShapeshiftDuration, new(1, 1000, 1), 10, false);
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown = 0f;
        AURoleOptions.ShapeshifterLeaveSkin = false;
        AURoleOptions.ShapeshifterDuration = shapeshiftDuration;
    }
}
