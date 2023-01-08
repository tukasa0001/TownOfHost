using System.Collections.Generic;
using TownOfHost.Gamemodes.FFA;
using TownOfHost.Interface.Menus;
using TownOfHost.ReduxOptions;
using TownOfHost.Victory;

namespace TownOfHost.Gamemodes.Colorwars;

// TODO add option to convert killed to same color, last color standing = win AND/OR traditional mode
public class ColorwarsGamemode: Gamemode
{
    public static int TeamSize = 2;

    public ColorwarsGamemode()
    {
        TOHPlugin.OptionManager.Add(new SmartOptionBuilder()
            .Name("Team Size")
            .IsHeader(true)
            .Tab(FreeForAllGamemode.FFATab)
            .BindInt(v => TeamSize = v)
            .AddIntRangeValues(1, 8, 1, 2)
            .Build());
    }


    public override string GetName() => "Color Wars";

    public override void AssignRoles(List<PlayerControl> players)
    {
        ColorwarsAssignRoles.AssignRoles(players);
    }

    public override IEnumerable<GameOptionTab> EnabledTabs() => new[] { FreeForAllGamemode.FFATab };

    public override void SetupWinConditions(WinDelegate winDelegate)
    {
        winDelegate.AddWinCondition(new ColorWarsWinCondition());
    }
}