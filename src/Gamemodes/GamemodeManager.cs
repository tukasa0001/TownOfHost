using System.Collections.Generic;
using TownOfHost.ReduxOptions;

namespace TownOfHost.Gamemodes;

// As we move to the future we're going to try to use instances for managers rather than making everything static
public class GamemodeManager
{
    public List<IGamemode> Gamemodes = new() { new StandardGamemode(), new TestHnsGamemode() };
    public IGamemode CurrentGamemode;
    public OptionHolder GamemodeOption;

    public void SetGamemode(int id)
    {
        CurrentGamemode = Gamemodes[id];
        CurrentGamemode.Activate();
    }

    public void Setup()
    {
        SmartOptionBuilder builder = new SmartOptionBuilder()
            .Name("Gamemode")
            .IsHeader(true)
            .BindInt(SetGamemode);

        for (int i = 0; i < Gamemodes.Count; i++)
        {
            IGamemode gamemode = Gamemodes[i];
            var index = i;
            builder.AddValue(v => v.Text(gamemode.GetName()).Value(index).Build());
        }

        GamemodeOption = builder.Build();
        TOHPlugin.OptionManager.AllHolders.Insert(0, GamemodeOption);
    }
}