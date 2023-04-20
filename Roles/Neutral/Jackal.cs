using System.Collections.Generic;
using AmongUs.GameOptions;
using Hazel;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Neutral
{
    public sealed class Jackal : RoleBase
    {
        public static readonly SimpleRoleInfo RoleInfo =
            new(
                typeof(Jackal),
                player => new Jackal(player),
                CustomRoles.Jackal,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Neutral,
                50900,
                SetupOptionItem,
                "#00b4eb"
            );
        public Jackal(PlayerControl player)
        : base(
            RoleInfo,
            player,
            () => HasTask.False,
            CountTypes.Jackal
        )
        {
            KillCooldown = OptionKillCooldown.GetFloat();
            CanVent = OptionCanVent.GetBool();
            CanUseSabotage = OptionCanUseSabotage.GetBool();
            HasImpostorVision = OptionHasImpostorVision.GetBool();
        }

        private static OptionItem OptionKillCooldown;
        public static OptionItem OptionCanVent;
        public static OptionItem OptionCanUseSabotage;
        private static OptionItem OptionHasImpostorVision;
        enum OptionName
        {
            KillCooldown,
            CanVent,
            CanUseSabotage,
            ImpostorVision
        }
        private static float KillCooldown;
        public static bool CanVent;
        public static bool CanUseSabotage;
        private static bool HasImpostorVision;
        private static void SetupOptionItem()
        {
            OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.KillCooldown, new(2.5f, 180f, 2.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionCanVent = BooleanOptionItem.Create(RoleInfo, 11, OptionName.CanVent, true, false);
            OptionCanUseSabotage = BooleanOptionItem.Create(RoleInfo, 12, OptionName.CanUseSabotage, false, false);
            OptionHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 13, OptionName.ImpostorVision, true, false);
        }
        public override float SetKillCooldown() => KillCooldown;
        public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision);
        public static void SetHudActive(HudManager __instance, bool isActive)
        {
            __instance.SabotageButton.ToggleVisible(isActive && CanUseSabotage);
        }
        public override bool CanSabotage(SystemTypes systemType) => CanUseSabotage;
    }
}