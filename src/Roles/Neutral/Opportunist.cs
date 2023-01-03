using TownOfHost.Extensions;
using TownOfHost.Options;
using TownOfHost.ReduxOptions;

namespace TownOfHost.Roles;

public class Opportunist : CustomRole
{
    [RoleAction(RoleActionType.RoundEnd)]
    public void OpportunistWin()
    {
        // apparently we are going to redo game ending so im just going to putt his code here
        if (MyPlayer.IsAlive())
        {
            "Opportunist Win".DebugLog();
        }
    }
    protected override RoleModifier Modify(RoleModifier roleModifier)
    {
        return roleModifier
        .RoleColor("#00ff00")
        .SpecialType(SpecialType.Neutral);
    }

    protected override SmartOptionBuilder RegisterOptions(SmartOptionBuilder optionStream) =>
         base.RegisterOptions(optionStream)
             .Tab(DefaultTabs.NeutralTab);
}