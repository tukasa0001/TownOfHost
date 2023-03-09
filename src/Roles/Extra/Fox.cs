using TOHTOR.Gamemodes;
using VentLib.Options.Game;

namespace TOHTOR.Roles;

public class Fox : NotImplemented
{

    protected override GameOptionBuilder RegisterOptions(GameOptionBuilder optionStream) =>
        base.RegisterOptions(optionStream)
            .Tab(TestHnsGamemode.HnsTab);
}