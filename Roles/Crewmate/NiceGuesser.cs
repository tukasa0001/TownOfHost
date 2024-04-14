using System.Collections.Generic;
using AmongUs.GameOptions;
using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;

using static TownOfHostForE.Options;

namespace TownOfHostForE.Roles.Impostor
{
    public sealed class NiceGuesser : RoleBase
    {
        /// <summary>
        ///  20000:TOH4E役職
        ///   1000:陣営 1:crew 2:imp 3:Third 4:Animals
        ///    100:役職ID
        /// </summary>
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(NiceGuesser),
                player => new NiceGuesser(player),
                CustomRoles.NiceGuesser,
                () => RoleTypes.Crewmate,
                CustomRoleTypes.Crewmate,
                21400,
                SetupOptionItem,
                "ナイスゲッサー"
            );

        public NiceGuesser(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
        {

        }

        public static OptionItem GGCanGuessCrew;
        public static OptionItem GGCanGuessAdt;
        public static OptionItem GGCanGuessVanilla;
        public static OptionItem GGCanGuessTime;
        public static OptionItem GGTryHideMsg;

        enum OptionName
        {
            GuesserCanGuessTimes,
            GGCanGuessCrew,
            EGCanGuessVanilla,
            EGCanGuessTaskDoneSnitch,
            EGGuesserTryHideMsg,
            ChangeGuessDeathReason,
        }

        public static OptionItem ChangeGuessDeathReason;

        private static void SetupOptionItem()
        {
            GGCanGuessTime = IntegerOptionItem.Create(RoleInfo, 10, OptionName.GuesserCanGuessTimes, new(1, 15, 1), 3, false)
                .SetValueFormat(OptionFormat.Players);
            GGCanGuessCrew = BooleanOptionItem.Create(RoleInfo, 11, OptionName.GGCanGuessCrew, false, false);
            GGCanGuessVanilla = BooleanOptionItem.Create(RoleInfo, 12, OptionName.EGCanGuessVanilla, false, false);
            GGTryHideMsg = BooleanOptionItem.Create(RoleInfo, 13, OptionName.EGGuesserTryHideMsg, false, false);
            ChangeGuessDeathReason = BooleanOptionItem.Create(RoleInfo, 14, OptionName.ChangeGuessDeathReason, false, false);
        }
    }
}
