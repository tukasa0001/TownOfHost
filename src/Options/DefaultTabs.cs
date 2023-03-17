using System.Collections.Generic;
using VentLib.Options.Game;
using VentLib.Options.Game.Tabs;
using VentLib.Utilities.Attributes;

namespace TOHTOR.Options;

[LoadStatic]
public class DefaultTabs
{
    public static GameOptionTab GeneralTab = new("General Settings", () => Utils.LoadSprite("TOHTOR.assets.Tabs.TabIcon_MainSettings.png"));

    public static GameOptionTab ImpostorsTab = new("Impostor Settings", () => Utils.LoadSprite("TOHTOR.assets.Tabs.TabIcon_ImpostorRoles.png"));

    public static GameOptionTab CrewmateTab = new("Crewmate Settings", () => Utils.LoadSprite("TOHTOR.assets.Tabs.TabIcon_CrewmateRoles.png"));

    public static GameOptionTab NeutralTab = new("Neutral Settings", () => Utils.LoadSprite("TOHTOR.assets.Tabs.TabIcon_NeutralRoles.png"));

    //public static GameOptionTab SubrolesTab = new("Subrole Settings", "TOHTOR.assets.TabIcon_Addons.png");

    public static GameOptionTab MiscTab = new("Misc Settings", () => Utils.LoadSprite("TOHTOR.assets.Tabs.TabIcon_MiscRoles.png"));

    public static GameOptionTab HiddenTab = new("Hidden", () => Utils.LoadSprite("TOHTOR.assets.TabIcon_Addons.png"));

    public static List<GameOptionTab> All = new() { GeneralTab, ImpostorsTab, CrewmateTab, NeutralTab, MiscTab };

    static DefaultTabs()
    {
        GameOptionController.AddTab(GeneralTab);
        GameOptionController.AddTab(ImpostorsTab);
        GameOptionController.AddTab(CrewmateTab);
        GameOptionController.AddTab(NeutralTab);
        GameOptionController.AddTab(MiscTab);
    }
}