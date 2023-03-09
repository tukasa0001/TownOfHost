using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TOHTOR.API;
using TOHTOR.Extensions;
using TOHTOR.Options;
using VentLib.Options;
using TOHTOR.Victory;
using UnityEngine;
using VentLib.Localization.Attributes;
using VentLib.Logging;
using VentLib.Options.Game;
using VentLib.Options.Game.Tabs;
using VentLib.Ranges;
using VentLib.Utilities;
using OptionValue = VentLib.Options.OptionValue;

namespace TOHTOR.Gamemodes.Colorwars;

// TODO add option to convert killed to same color, last color standing = win AND/OR traditional mode
[Localized(Group = "Gamemodes", Subgroup = "Colorwars")]
public class ColorwarsGamemode: Gamemode
{
    public static GameOptionTab ColorwarsTab = new("Colorwars", () => Utils.LoadSprite("TOHTOR.assets.Tabs.TabIcon_ColorWars.png"));
    public static int TeamSize = 2;
    public static bool ConvertColorMode;
    public static bool ManualTeams;
    public static float GracePeriod;

    public override string GetName() => "Color Wars";
    public override IEnumerable<GameOptionTab> EnabledTabs() => new[] { ColorwarsTab };
    public override GameAction IgnoredActions() => GameAction.CallSabotage | GameAction.ReportBody | GameAction.CallMeeting;

    public static List<TeamInfo> Teams = new();
    private bool updated;
    private bool randomSpawnLocations;

    private List<Option> playerOptions = new();

    private static readonly Color[] _colors =
    {
        Color.red, new(0.23f, 0.45f, 1f), new(0f, 0.43f, 0f), Color.magenta, new(1f, 0.54f, 0f),
        Color.yellow, new(0.17f, 0.17f, 0.17f), Color.white, new(0.33f, 0.24f, 0.5f), new(0.35f, 0.24f, 0.15f),
        Color.cyan, Color.green, new(0.4f, 0.15f, 0.14f), new(0.93f, 0.75f, 0.83f),
        new(1f, 1f, 0.74f), Color.gray, new(0.57f, 0.53f, 0.46f), new(0.93f, 0.46f, 0.47f)
    };

    public ColorwarsGamemode()
    {
        OptionManager colorwarsManager = OptionManager.GetManager(file: "colorwars_options.txt");
        new GameOptionBuilder()
            .Name("Team Size")
            .Description("Number of players per team in color wars.")
            .IsHeader(true)
            .Tab(ColorwarsTab)
            .BindInt(v => TeamSize = v)
            .AddIntRange(1, 8, 1, 2)
            .BuildAndRegister(colorwarsManager);


        new GameOptionBuilder()
            .Name("Convert Color Mode")
            .Tab(ColorwarsTab)
            .IsHeader(true)
            .BindBool(v => ConvertColorMode = v)
            .AddOnOffValues(false)
            .BuildAndRegister(colorwarsManager);

        new GameOptionBuilder()
            .LocaleName("Gamemodes.Colorwars.Options.GracePeriod")
            .Tab(ColorwarsTab)
            .IsHeader(true)
            .AddFloatRange(0, 30, 1f, 5, "s")
            .BindFloat(v => GracePeriod = v)
            .BuildAndRegister(colorwarsManager);

        new GameOptionBuilder()
            .LocaleName("StaticOptions.RandomSpawn.Enable")
            .Key("Colorwars Random Spawn")
            .Tab(ColorwarsTab)
            .BindBool(v => randomSpawnLocations = v)
            .AddOnOffValues()
            .BuildAndRegister(colorwarsManager);

        BindAction(GameAction.GameJoin, () => Async.Schedule(RefreshOptions, 1f));
        BindAction(GameAction.GameLeave, () => Async.Schedule(RefreshOptions, 1f));
    }

    private void RefreshOptions()
    {
        List<PlayerControl> allPlayers = PlayerControl.AllPlayerControls.ToArray().ToList();
        /*playerOptions.Do(opt => opt.Delete());*/ // TODO
        playerOptions.Clear();

        int playerCount = allPlayers.Count;
        int teams = Mathf.CeilToInt((float)playerCount / TeamSize);

        List<OptionValue> teamOptions = new IntRangeGen(1, teams)
            .AsEnumerable()
            .Select(i => new OptionValue.OptionValueBuilder().Text($"Team {i}").Value(i-1).Build())
            .ToList();

        allPlayers.Do(p =>
        {
            var newOption = new GameOptionBuilder()
                .Name(p.GetRawName())
                .Tab(ColorwarsTab)
                .Color(_colors[p.cosmetics.bodyMatProperties.ColorId])
                .Values(teamOptions)
                .Build();
            //TODO: Save on change
            playerOptions.Add(newOption);
        });
    }

    public override void AssignRoles(List<PlayerControl> players)
    {
        int teamNumber = -1;
        /*for (int i = 0; i < manualTeamsOption.SubOptions.Count && ManualTeams; i++)
        {
            Option option = manualTeamsOption.SubOptions[i];
            if (option.Name.StartsWith("Team")) {
                teamNumber++;
                continue;
            }
            byte playerId = Convert.ToByte(option.GetValue()!);
            VentLogger.Trace($"Adding player {playerId} to team: {teamNumber}");
            Teams[teamNumber].Players.Add(playerId);
        }*/

        ColorwarsAssignRoles.AssignRoles(new List<PlayerControl>(players));
        Async.Schedule(() => players.Do(p => Game.RandomSpawn.Spawn(p)), NetUtils.DeriveDelay(1f));
    }

    public override void SetupWinConditions(WinDelegate winDelegate) => winDelegate.AddWinCondition(new ColorWarsWinCondition());

    private void CreateTeamOptions()
    {
        updated = false;
        //manualTeamsOption.SubOptions.RemoveAll(o => _tempOption.GetTempOptions().Contains(o));
        //_tempOption.DeleteAll();

        int totalPlayers = PlayerControl.AllPlayerControls.Count;
        int remainder = totalPlayers % TeamSize;

        int totalTeams = Mathf.CeilToInt((float)totalPlayers / TeamSize);
        Teams.Clear();
        for (int ii = 0; ii < totalTeams; ii++) Teams.Add(new TeamInfo());

        for (int i = 0; i < totalTeams; i++)
        {
            var i1 = i;
            Option option = new GameOptionBuilder()
                .Name($"Team {i + 1} Color")
                .IsHeader(true)
                .Value(v => v.Text("Random").Color(new Color(0.61f, 0.67f, 1f)).Value(-1).Build())
                .Value(v => v.Text("Red").Color(_colors[0]).Value(0).Build())
                .Value(v => v.Text("Blue").Color(_colors[1]).Value(1).Build())
                .Value(v => v.Text("Green").Color(_colors[2]).Value(2).Build())
                .Value(v => v.Text("Pink").Color(_colors[3]).Value(3).Build())
                .Value(v => v.Text("Orange").Color(_colors[4]).Value(4).Build())
                .Value(v => v.Text("Yellow").Color(_colors[5]).Value(5).Build())
                .Value(v => v.Text("Black").Color(_colors[6]).Value(6).Build())
                .Value(v => v.Text("White").Color(_colors[7]).Value(7).Build())
                .Value(v => v.Text("Purple").Color(_colors[8]).Value(8).Build())
                .Value(v => v.Text("Brown").Color(_colors[9]).Value(9).Build())
                .Value(v => v.Text("Cyan").Color(_colors[10]).Value(10).Build())
                .Value(v => v.Text("Lime").Color(_colors[11]).Value(11).Build())
                .Value(v => v.Text("Maroon").Color(_colors[12]).Value(12).Build())
                .Value(v => v.Text("Rose").Color(_colors[13]).Value(13).Build())
                .Value(v => v.Text("Banana").Color(_colors[14]).Value(14).Build())
                .Value(v => v.Text("Gray").Color(_colors[15]).Value(15).Build())
                .Value(v => v.Text("Tan").Color(_colors[16]).Value(16).Build())
                .Value(v => v.Text("Coral").Color(_colors[17]).Value(17).Build())
                .Bind(v => Teams[i1].Color = v == null ? -1 : (int)v).Build();
            //manualTeamsOption.SubOptions.Add(option);
            //_tempOption.Add(option);

            int teamSize = i < totalTeams - 1 || remainder == 0 ? TeamSize : remainder;

            for (int j = 0; j < teamSize; j++)
            {
                GameOptionBuilder builder = new GameOptionBuilder().Name($"Player {j + 1}");
                foreach (PlayerControl player in PlayerControl.AllPlayerControls)
                    builder.Value(v => v.Text(player.GetRawName()).Value((int)player.PlayerId).Build());
                option = builder.Build();
                //manualTeamsOption.SubOptions.Add(option);
                //_tempOption.Add(option);
            }
        }
    }

    public record TeamInfo
    {
        public int Color { get; set; } = -1;
        public List<byte> Players { get; } = new();
    }
}