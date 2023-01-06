using System.Collections.Generic;
using TownOfHost.Interface.Menus;

namespace TownOfHost.Options;

public class DefaultTabs
{
    public static GameOptionTab GeneralTab = new("General Settings", "TownOfHost.assets.TabIcon_MainSettings.png");

    public static GameOptionTab ImpostorsTab = new("Impostor Settings", "TownOfHost.assets.TabIcon_ImpostorRoles.png");

    public static GameOptionTab CrewmateTab = new("Crewmate Settings", "TownOfHost.assets.TabIcon_CrewmateRoles.png");

    public static GameOptionTab NeutralTab = new("Neutral Settings", "TownOfHost.assets.TabIcon_NeutralRoles.png");

    //public static GameOptionTab SubrolesTab = new("Subrole Settings", "TownOfHost.assets.TabIcon_Addons.png");

    public static GameOptionTab MiscTab = new("Misc Settings", "TownOfHost.assets.TabIcon_MiscRoles.png", TabOrder.Last);

    public static List<GameOptionTab> All = new() { GeneralTab, ImpostorsTab, CrewmateTab, NeutralTab, MiscTab };

    static DefaultTabs()
    {
        GeneralTab.Register();
        ImpostorsTab.Register();
        CrewmateTab.Register();
        NeutralTab.Register();
        MiscTab.Register();
    }
}