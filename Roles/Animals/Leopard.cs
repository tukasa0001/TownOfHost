using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using TownOfHostForE.Roles.Neutral;
using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Core.Interfaces;

using static TownOfHostForE.Options;

namespace TownOfHostForE.Roles.Animals
{
    public sealed class Leopard : RoleBase, IKiller, ISchrodingerCatOwner
    {
        /// <summary>
        ///  20000:TOH4E役職
        ///   1000:陣営 1:crew 2:imp 3:Third 4:Animals
        ///    100:役職ID
        /// </summary>
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Leopard),
                player => new Leopard(player),
                CustomRoles.Leopard,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Animals,
                24500,
                SetupOptionItem,
                "ヒョウ",
                "#FF8C00",
                true,
                countType: CountTypes.Animals,
            introSound: () => ShipStatus.Instance.CommonTasks.Where(task => task.TaskType == TaskTypes.FixWiring).FirstOrDefault().MinigamePrefab.OpenSound
            );
        public Leopard(PlayerControl player)
        : base(
            RoleInfo,
            player,
            () => HasTask.False
        )
        {

        }
        public SchrodingerCat.TeamType SchrodingerCatChangeTo => SchrodingerCat.TeamType.Animals;

        public static OptionItem AGCanGuessTime;
        public static OptionItem AGCanGuessAnim;
        public static OptionItem AGCanGuessVanilla;
        public static OptionItem AGCanGuessTaskDoneSnitch;
        public static OptionItem AGTryHideMsg;
        public static OptionItem AGCantWhiteCrew;

        enum OptionName
        {
            GuesserCanGuessTimes,
            AGCanGuessAnim,
            EGCanGuessVanilla,
            EGCanGuessTaskDoneSnitch,
            EGGuesserTryHideMsg,
            ChangeGuessDeathReason,
            GuessCantWhiteCrew,
        }

        public static OptionItem ChangeGuessDeathReason;

        private static void SetupOptionItem()
        {
            AGCanGuessTime = IntegerOptionItem.Create(RoleInfo, 10, OptionName.GuesserCanGuessTimes, new(1, 15, 1), 3, false)
                .SetValueFormat(OptionFormat.Players);
            AGCanGuessAnim = BooleanOptionItem.Create(RoleInfo, 11, OptionName.AGCanGuessAnim, false, false);
            AGCanGuessVanilla = BooleanOptionItem.Create(RoleInfo, 12, OptionName.EGCanGuessVanilla, false, false);
            AGCanGuessTaskDoneSnitch = BooleanOptionItem.Create(RoleInfo, 13, OptionName.EGCanGuessTaskDoneSnitch, false, false);
            AGTryHideMsg = BooleanOptionItem.Create(RoleInfo, 14, OptionName.EGGuesserTryHideMsg, false, false);
            ChangeGuessDeathReason = BooleanOptionItem.Create(RoleInfo, 15, OptionName.ChangeGuessDeathReason, false, false);
            AGCantWhiteCrew = BooleanOptionItem.Create(RoleInfo, 16, OptionName.GuessCantWhiteCrew, false, false);
        }

        public override void Add()
        {
            var playerId = Player.PlayerId;
        }
        public bool CanUseImpostorVentButton() => true;
        public bool CanUseSabotageButton() => false;
        public void ApplySchrodingerCatOptions(IGameOptions option) => ApplyGameOptions(option);
    }
}
