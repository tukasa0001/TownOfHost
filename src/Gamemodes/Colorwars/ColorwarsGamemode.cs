using System.Collections.Generic;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Options;
using TownOfHost.ReduxOptions;
using TownOfHost.Roles;
using TownOfHost.Victory;

namespace TownOfHost.Gamemodes.Colorwars;

// TODO add option to convert killed to same color, last color standing = win AND/OR traditional mode
public class ColorwarsGamemode: Gamemode
{
    public static GameOptionTab ColorwarsTab = new("Colorwars", "TownOfHost.assets.Tabs.TabIcon_ColorWars.png");
    public static int TeamSize = 2;
    public static bool ConvertColorMode;

    public override string GetName() => "Color Wars";
    public override IEnumerable<GameOptionTab> EnabledTabs() => new[] { ColorwarsTab };
    public override GameAction IgnoredActions() => GameAction.CallSabotage | GameAction.ReportBody;

    public ColorwarsGamemode()
    {
        TOHPlugin.OptionManager.Add(new SmartOptionBuilder()
            .Name("Team Size")
            .IsHeader(true)
            .Tab(ColorwarsTab)
            .BindInt(v => TeamSize = v)
            .AddIntRangeValues(1, 8, 1, 2)
            .Build());

        TOHPlugin.OptionManager.Add(new SmartOptionBuilder()
            .Name("Convert Color Mode")
            .Tab(ColorwarsTab)
            .IsHeader(true)
            .BindBool(v => ConvertColorMode = v)
            .AddOnOffValues(false)
            .Build());

        OptionHolder skOptions = CustomRoleManager.Static.SerialKiller.GetOptionBuilder().Tab(ColorwarsTab).Build();
        TOHPlugin.OptionManager.Add(skOptions);
    }

    public override void Activate()
    {
        var original = typeof(Impostor).GetMethod(nameof(Impostor.TryKill));
        var prefix = typeof(ColorwarsGamemode).GetMethod(nameof(Prefix));
        TOHPlugin.Instance.Harmony.Patch(original, prefix: new HarmonyMethod(prefix));
    }

    public override void Deactivate()
    {
        var original = typeof(SerialKiller).GetMethod(nameof(SerialKiller.TryKill));
        TOHPlugin.Instance.Harmony.Unpatch(original, HarmonyPatchType.Prefix);
    }

    public override void AssignRoles(List<PlayerControl> players)
    {
        ColorwarsAssignRoles.AssignRoles(players);
    }

    public override void SetupWinConditions(WinDelegate winDelegate)
    {
        winDelegate.AddWinCondition(new ColorWarsWinCondition());
    }

    // This is a patch for Impostor that's only active while Colorwars is running it for the turf-war style of Colorwars
    [HarmonyPrefix]
    public static bool Prefix(Impostor __instance, PlayerControl target)
    {
        if (!AmongUsClient.Instance.AmHost || !ConvertColorMode) return true;
        int killerColor = __instance.MyPlayer.cosmetics.bodyMatProperties.ColorId;
        if (killerColor == target.cosmetics.bodyMatProperties.ColorId) return false;

        target.RpcSetColor((byte)killerColor);
        GameOptionOverride[] killOverride = { new(Override.KillCooldown, __instance.KillCooldown * 2) };
        __instance.MyPlayer.RpcGuardAndKill(target);
        if (__instance is SerialKiller sk) sk.DeathTimer.Start();
            __instance.SyncOptions(killOverride);
        return false;
    }
}