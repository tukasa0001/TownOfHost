using System.Text;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using UnityEngine;

namespace TownOfHost.Roles.Impostor
{
    public sealed class Insider : RoleBase
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Insider),
                player => new Insider(player),
                CustomRoles.Insider,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Impostor,
                2800,
                SetupOptionItem,
                "ins"
            );
        public Insider(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
        {
            canSeeImpostorAbilities = optionCanSeeImpostorAbilities.GetBool();
            canSeeAllGhostsRoles = optionCanSeeAllGhostsRoles.GetBool();
            canSeeMadmates = optionCanSeeMadmates.GetBool();
            killCountToSeeMadmates = optionKillCountToSeeMadmates.GetInt();
        }
        private static OptionItem optionCanSeeImpostorAbilities;
        private static OptionItem optionCanSeeAllGhostsRoles;
        private static OptionItem optionCanSeeMadmates;
        private static OptionItem optionKillCountToSeeMadmates;
        private enum OptionName
        {
            InsiderCanSeeImpostorAbilities,
            InsiderCanSeeAllGhostsRoles,
            InsiderCanSeeMadmates,
            InsiderKillCountToSeeMadmates,
        }
        private static bool canSeeImpostorAbilities;
        private static bool canSeeAllGhostsRoles;
        private static bool canSeeMadmates;
        private static int killCountToSeeMadmates;

        private static void SetupOptionItem()
        {
            optionCanSeeImpostorAbilities = BooleanOptionItem.Create(RoleInfo, 10, OptionName.InsiderCanSeeImpostorAbilities, true, false);
            optionCanSeeAllGhostsRoles = BooleanOptionItem.Create(RoleInfo, 11, OptionName.InsiderCanSeeAllGhostsRoles, false, false);
            optionCanSeeMadmates = BooleanOptionItem.Create(RoleInfo, 12, OptionName.InsiderCanSeeMadmates, false, false);
            optionKillCountToSeeMadmates = IntegerOptionItem.Create(RoleInfo, 13, OptionName.InsiderKillCountToSeeMadmates, new(0, 15, 1), 2, false)
                .SetParent(optionCanSeeMadmates)
                .SetValueFormat(OptionFormat.Times);
        }
    }
}