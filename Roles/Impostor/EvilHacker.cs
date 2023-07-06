using AmongUs.GameOptions;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;

public sealed class EvilHacker : RoleBase, IImpostor, IKillFlashSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(EvilHacker),
            player => new EvilHacker(player),
            CustomRoles.EvilHacker,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            3100,
            SetupOptionItems,
            "eh"
        );
    public EvilHacker(PlayerControl player) : base(RoleInfo, player)
    {
        canSeeDeadMark = OptionCanSeeDeadMark.GetBool();
        canSeeImpostorMark = OptionCanSeeImpostorMark.GetBool();
        canSeeKillFlash = OptionCanSeeKillFlash.GetBool();
        canSeeMurderRoom = OptionCanSeeMurderRoom.GetBool();
    }

    private static OptionItem OptionCanSeeDeadMark;
    private static OptionItem OptionCanSeeImpostorMark;
    private static OptionItem OptionCanSeeKillFlash;
    private static OptionItem OptionCanSeeMurderRoom;
    private enum OptionName
    {
        EvilHackerCanSeeDeadMark,
        EvilHackerCanSeeImpostorMark,
        EvilHackerCanSeeKillFlash,
        EvilHackerCanSeeMurderRoom,
    }
    private static bool canSeeDeadMark;
    private static bool canSeeImpostorMark;
    private static bool canSeeKillFlash;
    private static bool canSeeMurderRoom;

    private static void SetupOptionItems()
    {
        OptionCanSeeDeadMark = BooleanOptionItem.Create(RoleInfo, 10, OptionName.EvilHackerCanSeeDeadMark, true, false);
        OptionCanSeeImpostorMark = BooleanOptionItem.Create(RoleInfo, 11, OptionName.EvilHackerCanSeeImpostorMark, true, false);
        OptionCanSeeKillFlash = BooleanOptionItem.Create(RoleInfo, 12, OptionName.EvilHackerCanSeeKillFlash, true, false);
        OptionCanSeeMurderRoom = BooleanOptionItem.Create(RoleInfo, 13, OptionName.EvilHackerCanSeeMurderRoom, true, false, OptionCanSeeKillFlash);
    }
}
