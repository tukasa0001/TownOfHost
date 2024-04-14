using System.Collections.Generic;
using AmongUs.GameOptions;
using TownOfHostForE.Roles.Core;

using static TownOfHostForE.Translator;
using static TownOfHostForE.Utils;

namespace TownOfHostForE.Roles.Crewmate;
public sealed class SeeingOff : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(SeeingOff),
            player => new SeeingOff(player),
            CustomRoles.SeeingOff,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            40500,
            SetupOptionItem,
            "見送り人",
            "#883fd1"
        );
    public SeeingOff(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        optionSeeingCrew = OptionSeeingCrew.GetBool();
    }
    enum OptionName
    {
        AddOptionSeeingCrew
    }
    private static OptionItem OptionSeeingCrew;
    public static bool optionSeeingCrew = false;

    private static void SetupOptionItem()
    {
        OptionSeeingCrew = BooleanOptionItem.Create(RoleInfo, 10, OptionName.AddOptionSeeingCrew, false, false);
    }
}