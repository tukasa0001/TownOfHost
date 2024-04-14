using System.Collections.Generic;
using AmongUs.GameOptions;
using Hazel;
using TownOfHostForE.Roles.Core.Interfaces;
using TownOfHostForE.Roles.Core;
using TownOfHostForE.Roles.Neutral;
using static TownOfHostForE.Options;

namespace TownOfHostForE.Roles.Animals
{
    public sealed class Coyote : RoleBase, IKiller, ISchrodingerCatOwner
    {

        /// <summary>
        ///  20000:TOH4E役職
        ///   1000:陣営 1:crew 2:imp 3:Third 4:Animals
        ///    100:役職ID
        /// </summary>
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Coyote),
                player => new Coyote(player),
                CustomRoles.Coyote,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Animals,
                24100,
                SetupOptionItem,
                "コヨーテ",
                "#FF8C00",
                true,
                countType: CountTypes.Animals
            );
        public Coyote(PlayerControl player)
        : base(
            RoleInfo,
            player,
            () => HasTask.False
        )
        {
            KillCooldown = OptionKillCooldown.GetFloat();
            CanVent = OptionCanVent.GetBool();
            CanUseSabotage = OptionCanUseSabotage.GetBool();
            HasImpostorVision = OptionHasImpostorVision.GetBool();
        }
        public SchrodingerCat.TeamType SchrodingerCatChangeTo => SchrodingerCat.TeamType.Animals;

        private static OptionItem OptionKillCooldown;
        public static OptionItem OptionCanVent;
        public static OptionItem OptionCanUseSabotage;
        private static OptionItem OptionHasImpostorVision;
        private static float KillCooldown;
        public static bool CanVent;
        public static bool CanUseSabotage;
        private static bool HasImpostorVision;
        private static void SetupOptionItem()
        {
            OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionCanVent = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.CanVent, true, false);
            OptionCanUseSabotage = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.CanUseSabotage, false, false);
            OptionHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 13, GeneralOption.ImpostorVision, true, false);
        }
        public override void Add()
        {
            var playerId = Player.PlayerId;
        }
        public float CalculateKillCooldown() => KillCooldown;
        public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision);
        public bool CanUseImpostorVentButton() => CanVent;
        public bool CanUseSabotageButton() => CanUseSabotage;
        public void ApplySchrodingerCatOptions(IGameOptions option) => ApplyGameOptions(option);
    }
}