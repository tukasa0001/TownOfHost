using System.Collections.Generic;
using TownOfHost.Options;
using TownOfHost.ReduxOptions;

namespace TownOfHost.Gamemodes;

// As we move to the future we're going to try to use instances for managers rather than making everything static
public class GamemodeManager
{
    public List<IGamemode> Gamemodes = new() { new StandardGamemode() };
    public IGamemode CurrentGamemode;

    public void SetGamemode(int id)
    {
        CurrentGamemode = Gamemodes[id];
    }

    public void Setup()
    {
        SmartOptionBuilder builder = new SmartOptionBuilder()
            .Name("Gamemode")
            .Tab(DefaultTabs.GeneralTab)
            .IsHeader(true)
            .BindInt(SetGamemode);

        for (int i = 0; i < Gamemodes.Count; i++)
        {
            IGamemode gamemode = Gamemodes[i];
            var index = i;
            builder.AddValue(v => v.Text(gamemode.GetName()).Value(index).Build());
        }

        TOHPlugin.OptionManager.Add(builder.Build());
    }
}