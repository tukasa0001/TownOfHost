using System;
using System.Collections.Generic;
using HarmonyLib;
using TownOfHost.Extensions;
using TownOfHost.Managers;
using TownOfHost.Managers.Date;
using TownOfHost.Options;
using TownOfHost.Roles;
using TownOfHost.Roles.Internals;
using TownOfHost.Victory;
using UnityEngine;
using VentLib.Localization;
using VentLib.Localization.Attributes;
using VentLib.Logging;
using VentLib.Utilities;

namespace TownOfHost.Gamemodes.Colorwars;

// TODO add option to convert killed to same color, last color standing = win AND/OR traditional mode
[Localized(Group = "Gamemodes", Subgroup = "Colorwars")]
public class ColorwarsGamemode: Gamemode
{
    public static GameOptionTab ColorwarsTab = new("Colorwars", "TownOfHost.assets.Tabs.TabIcon_ColorWars.png");
    public static int TeamSize = 2;
    public static bool ConvertColorMode;
    public static bool ManualTeams;
    public static float GracePeriod;
    private static TempOptionHolder _tempOptionHolder = new();

    public override string GetName() => "Color Wars";
    public override IEnumerable<GameOptionTab> EnabledTabs() => new[] { ColorwarsTab };
    public override GameAction IgnoredActions() => GameAction.CallSabotage | GameAction.ReportBody;

    public static List<TeamInfo> Teams = new();
    private OptionHolder manualTeamsOption;
    private bool updated;
    private bool randomSpawnLocations;

    private static Color[] colors =
    {
        Color.red, new(0.23f, 0.45f, 1f), new(0f, 0.43f, 0f), Color.magenta, new(1f, 0.54f, 0f),
        Color.yellow, new(0.17f, 0.17f, 0.17f), Color.white, new(0.33f, 0.24f, 0.5f), new(0.35f, 0.24f, 0.15f),
        Color.cyan, Color.green, new(0.4f, 0.15f, 0.14f), new(0.93f, 0.75f, 0.83f),
        new(1f, 1f, 0.74f), Color.gray, new(0.57f, 0.53f, 0.46f), new(0.93f, 0.46f, 0.47f)
    };

    public ColorwarsGamemode()
    {
        TOHPlugin.OptionManager.Add(new SmartOptionBuilder()
            .Name("Team Size")
            .IsHeader(true)
            .Tab(ColorwarsTab)
            .BindInt(v =>
            {
                updated = true;
                TeamSize = v;
            })
            .AddIntRangeValues(1, 8, 1, 2)
            .Build());

        TOHPlugin.OptionManager.Add(new SmartOptionBuilder()
            .Name("Convert Color Mode")
            .Tab(ColorwarsTab)
            .IsHeader(true)
            .BindBool(v => ConvertColorMode = v)
            .AddOnOffValues(false)
            .Build());

        CustomRoleManager.LinkEditor(typeof(CwPainter));

        manualTeamsOption = new SmartOptionBuilder()
            .Name("Manually Assign Teams").Tab(ColorwarsTab).IsHeader(true).AddOnOffValues()
            .ShowSubOptionsWhen(v => (bool)v)
            .BindBool(v => {
                updated = true;
                ManualTeams = v;
            }).Build();

        TOHPlugin.OptionManager.Add(new SmartOptionBuilder()
            .Name(Localizer.Get("Gamemodes.Colorwars.Options.GracePeriod"))
            .Tab(ColorwarsTab)
            .IsHeader(true)
            .AddFloatRangeValues(0, 30, 1f, 5, "s")
            .BindFloat(v => GracePeriod = v)
            .Build());

        TOHPlugin.OptionManager.Add(new SmartOptionBuilder()
            .Name(Localizer.Get("StaticOptions.RandomSpawn.Enable"))
            .Tab(ColorwarsTab)
            .BindBool(v => randomSpawnLocations = v)
            .AddOnOffValues()
            .Build());

        TOHPlugin.OptionManager.Add(manualTeamsOption);
        BindAction(GameAction.GameJoin, () => updated = true);
        BindAction(GameAction.GameLeave, () => updated = true);
    }

    public override void Activate()
    {
        if (!SpecialDate.ShiftyBirthday.IsDate())
        {
            Async.Schedule(() => TOHPlugin.GamemodeManager.SetGamemode(0), 0.05f);
            return;
        }
        if (TOHPlugin.Initialized && updated)
            CreateTeamOptions();
    }

    public override void Deactivate()
    {
        _tempOptionHolder.DeleteAll();
    }

    public override void AssignRoles(List<PlayerControl> players)
    {
        int teamNumber = -1;
        for (int i = 0; i < manualTeamsOption.SubOptions.Count && ManualTeams; i++)
        {
            OptionHolder option = manualTeamsOption.SubOptions[i];
            if (option.Name.StartsWith("Team")) {
                teamNumber++;
                continue;
            }
            byte playerId = Convert.ToByte(option.GetValue()!);
            VentLogger.Trace($"Adding player {playerId} to team: {teamNumber}");
            Teams[teamNumber].Players.Add(playerId);
        }

        ColorwarsAssignRoles.AssignRoles(new List<PlayerControl>(players));
        Async.Schedule(() => players.Do(p => Game.RandomSpawn.Spawn(p)), NetUtils.DeriveDelay(1f));
    }

    public override void SetupWinConditions(WinDelegate winDelegate) => winDelegate.AddWinCondition(new ColorWarsWinCondition());

    private void CreateTeamOptions()
    {
        updated = false;
        manualTeamsOption.SubOptions.RemoveAll(o => _tempOptionHolder.GetTempOptions().Contains(o));
        _tempOptionHolder.DeleteAll();

        int totalPlayers = PlayerControl.AllPlayerControls.Count;
        int remainder = totalPlayers % TeamSize;

        int totalTeams = Mathf.CeilToInt((float)totalPlayers / TeamSize);
        Teams.Clear();
        for (int ii = 0; ii < totalTeams; ii++) Teams.Add(new TeamInfo());

        for (int i = 0; i < totalTeams; i++)
        {
            var i1 = i;
            OptionHolder option = new SmartOptionBuilder()
                .Name($"Team {i + 1} Color")
                .IsHeader(true)
                .AddValue(v => v.Text("Random").Color(new Color(0.61f, 0.67f, 1f)).Value(-1).Build())
                .AddValue(v => v.Text("Red").Color(colors[0]).Value(0).Build())
                .AddValue(v => v.Text("Blue").Color(colors[1]).Value(1).Build())
                .AddValue(v => v.Text("Green").Color(colors[2]).Value(2).Build())
                .AddValue(v => v.Text("Pink").Color(colors[3]).Value(3).Build())
                .AddValue(v => v.Text("Orange").Color(colors[4]).Value(4).Build())
                .AddValue(v => v.Text("Yellow").Color(colors[5]).Value(5).Build())
                .AddValue(v => v.Text("Black").Color(colors[6]).Value(6).Build())
                .AddValue(v => v.Text("White").Color(colors[7]).Value(7).Build())
                .AddValue(v => v.Text("Purple").Color(colors[8]).Value(8).Build())
                .AddValue(v => v.Text("Brown").Color(colors[9]).Value(9).Build())
                .AddValue(v => v.Text("Cyan").Color(colors[10]).Value(10).Build())
                .AddValue(v => v.Text("Lime").Color(colors[11]).Value(11).Build())
                .AddValue(v => v.Text("Maroon").Color(colors[12]).Value(12).Build())
                .AddValue(v => v.Text("Rose").Color(colors[13]).Value(13).Build())
                .AddValue(v => v.Text("Banana").Color(colors[14]).Value(14).Build())
                .AddValue(v => v.Text("Gray").Color(colors[15]).Value(15).Build())
                .AddValue(v => v.Text("Tan").Color(colors[16]).Value(16).Build())
                .AddValue(v => v.Text("Coral").Color(colors[17]).Value(17).Build())
                .Bind(v => Teams[i1].Color = v == null ? -1 : (int)v).Build();
            manualTeamsOption.SubOptions.Add(option);
            _tempOptionHolder.Add(option);

            int teamSize = i < totalTeams - 1 || remainder == 0 ? TeamSize : remainder;

            for (int j = 0; j < teamSize; j++)
            {
                SmartOptionBuilder builder = new SmartOptionBuilder().Name($"Player {j + 1}");
                foreach (PlayerControl player in PlayerControl.AllPlayerControls)
                    builder.AddValue(v => v.Text(player.GetRawName()).Value((int)player.PlayerId).Build());
                option = builder.Build();
                manualTeamsOption.SubOptions.Add(option);
                _tempOptionHolder.Add(option);
            }
        }
    }

    public record TeamInfo
    {
        public int Color { get; set; } = -1;
        public List<byte> Players { get; } = new();
    }
}