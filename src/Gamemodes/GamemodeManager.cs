using System;
using System.Collections.Generic;
using System.Linq;
using TOHTOR.Gamemodes.CaptureTheFlag;
using TOHTOR.Gamemodes.Colorwars;
using TOHTOR.Gamemodes.Debug;
using TOHTOR.Gamemodes.Standard;
using VentLib.Options;
using VentLib.Logging;
using VentLib.Options.Game;
using VentLib.Options.Game.Events;
using VentLib.Options.Game.Tabs;
using VentLib.Utilities.Extensions;

namespace TOHTOR.Gamemodes;

// As we move to the future we're going to try to use instances for managers rather than making everything static
public class GamemodeManager
{
    public List<IGamemode> Gamemodes = new();

    public IGamemode CurrentGamemode
    {
        get => _currentGamemode;
        set
        {
            _currentGamemode?.InternalDeactivate();
            _currentGamemode = value;
            _currentGamemode?.InternalActivate();
        }
    }

    private IGamemode _currentGamemode;
    private Option gamemodeOption = null!;
    internal readonly List<Type> GamemodeTypes = new() { typeof(StandardGamemode), typeof(TestHnsGamemode), typeof(ColorwarsGamemode), typeof(DebugGamemode), typeof(CTFGamemode)};

    public void SetGamemode(int id)
    {
        CurrentGamemode = Gamemodes[id];
        VentLogger.High($"Setting Gamemode {CurrentGamemode.GetName()}", "Gamemode");
    }

    public void Setup()
    {
        Gamemodes = GamemodeTypes.Select(g => (IGamemode)g.GetConstructor(Array.Empty<Type>())!.Invoke(null)).ToList();

        GameOptionBuilder builder = new GameOptionBuilder()
            .Name("Gamemode")
            .IsHeader(true)
            .Tab(VanillaMainTab.Instance)
            .BindInt(SetGamemode);

        for (int i = 0; i < Gamemodes.Count; i++)
        {
            IGamemode gamemode = Gamemodes[i];
            var index = i;
            builder.Value(v => v.Text(gamemode.GetName()).Value(index).Build());
        }

        gamemodeOption = builder.BuildAndRegister();
        GameOptionController.RegisterEventHandler(ce =>
        {
            if (ce is not OptionOpenEvent) return;
            GameOptionController.ClearTabs();
            _currentGamemode.EnabledTabs().ForEach(GameOptionController.AddTab);
        });
    }
}