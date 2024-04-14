using System.Collections.Generic;
using AmongUs.GameOptions;
using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;

using static TownOfHostForE.Options;
namespace TownOfHostForE.Roles.Impostor
{
   public sealed class EvilGuesser : RoleBase, IImpostor
    {
        /// <summary>
        ///  20000:TOH4E役職
        ///   1000:陣営 1:crew 2:imp 3:Third 4:Animals
        ///    100:役職ID
        /// </summary>
        public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(EvilGuesser),
            player => new EvilGuesser(player),
            CustomRoles.EvilGuesser,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            22300,
            SetupOptionItem,
            "イビルゲッサー"
        );
        public EvilGuesser(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
        {

        }

        public static OptionItem EGCanGuessTime;
        public static OptionItem EGCanGuessImp;
        public static OptionItem EGCanGuessVanilla;
        public static OptionItem EGCanGuessTaskDoneSnitch;
        public static OptionItem EGTryHideMsg;
        public static OptionItem EGCantWhiteCrew;

        enum OptionName
        {
            GuesserCanGuessTimes,
            EGCanGuessImp,
            EGCanGuessVanilla,
            EGCanGuessTaskDoneSnitch,
            EGGuesserTryHideMsg,
            ChangeGuessDeathReason,
            GuessCantWhiteCrew,
        }

        public static OptionItem ChangeGuessDeathReason;

        private static void SetupOptionItem()
        {
            EGCanGuessTime = IntegerOptionItem.Create(RoleInfo, 10, OptionName.GuesserCanGuessTimes, new(1, 15, 1), 3, false)
                .SetValueFormat(OptionFormat.Players);
            EGCanGuessImp = BooleanOptionItem.Create(RoleInfo, 11, OptionName.EGCanGuessImp, false, false);
            EGCanGuessVanilla = BooleanOptionItem.Create(RoleInfo, 12, OptionName.EGCanGuessVanilla, false, false);
            EGCanGuessTaskDoneSnitch = BooleanOptionItem.Create(RoleInfo, 13, OptionName.EGCanGuessTaskDoneSnitch, false, false);
            EGTryHideMsg = BooleanOptionItem.Create(RoleInfo, 14, OptionName.EGGuesserTryHideMsg, false, false);
            ChangeGuessDeathReason = BooleanOptionItem.Create(RoleInfo, 15, OptionName.ChangeGuessDeathReason, false, false);
            EGCantWhiteCrew = BooleanOptionItem.Create(RoleInfo, 16, OptionName.GuessCantWhiteCrew, false, false);
        }
    }
}
